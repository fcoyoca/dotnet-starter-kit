import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  CheckCircle2,
  ClipboardList,
  FileDown,
  FileText,
  PenLine,
  Pill,
  Plus,
  Save,
  Stethoscope,
  Tablets,
  UserCheck,
} from "lucide-react";
import { toast } from "sonner";
import { getPatientById } from "@/api/patients";
import {
  listCustomDiagnostics,
  listReportFields,
  useClinicOptions,
  useProviderOptions,
  type ReportFieldDto,
} from "@/api/administration";
import {
  addAddendum,
  getReport,
  requestReview,
  reviewSign,
  signReport,
  updateReport,
  type ReportFieldValue,
} from "@/api/reports";
import { getPatientIncident } from "@/api/incidents";
import { searchPatientProblems, setReportProblems } from "@/api/problems";
import { REPORT_PERMISSIONS, SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
import {
  clearReportDraft,
  DRAFT_WRITE_DEBOUNCE_MS,
  readReportDraft,
  writeReportDraft,
  type ReportDraft,
} from "@/state/report-draft-store";
import { useAuth } from "@/auth/use-auth";
import { usePatientTab } from "@/state/patient-workspace-context";
import { useIncidentSwitch } from "@/pages/patient-charts/incident-switch-guard";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { MacroInsert } from "@/components/ui/macro-insert";
import { Combobox, Field } from "@/components/list";
import { describe, formatDate, formatDateTimeMono } from "@/lib/list-helpers";
import { IncidentRef } from "@/pages/patient-charts/incident-ref";
import { ImportAllergiesDialog } from "@/pages/patient-charts/import-allergies-dialog";
import { ImportMedicationsDialog } from "@/pages/patient-charts/import-medications-dialog";
import { ProceduresPerformedDialog } from "@/pages/patient-charts/procedures-performed-dialog";

/** lbs + inches → BMI (rounded to 1 decimal); null when either is missing/0. */
function computeBmi(heightInches: number | null, weightLbs: number | null): number | null {
  if (!heightInches || !weightLbs) return null;
  const bmi = (weightLbs / (heightInches * heightInches)) * 703;
  if (!Number.isFinite(bmi)) return null;
  return Math.round(bmi * 10) / 10;
}

// Fields that expose "Import Dx Codes" (legacy: Clinical Impression = ldfID 15,
// Assessment = ldfID 22). Matched by name since clinic fields are per-type copies.
const DX_IMPORT_FIELDS = new Set(["clinical impression", "assessment"]);
function fieldImportsDx(name: string): boolean {
  return DX_IMPORT_FIELDS.has(name.trim().toLowerCase());
}

// Field that carries the Procedures Performed affordance and receives its macro text
// (legacy: ldfID 24 "Plan"). Matched by name like DX_IMPORT_FIELDS since clinic fields
// are per-type copies.
const PLAN_FIELDS = new Set(["plan", "plan comments", "treatment plan"]);
function fieldIsPlan(name: string): boolean {
  return PLAN_FIELDS.has(name.trim().toLowerCase());
}

// Fields that expose Import Allergies / Import Medications (legacy: ldfID 5 / 6).
const ALLERGY_IMPORT_FIELDS = new Set(["allergies"]);
function fieldImportsAllergies(name: string): boolean {
  return ALLERGY_IMPORT_FIELDS.has(name.trim().toLowerCase());
}
const MEDICATION_IMPORT_FIELDS = new Set(["medications"]);
function fieldImportsMedications(name: string): boolean {
  return MEDICATION_IMPORT_FIELDS.has(name.trim().toLowerCase());
}

// Vitals belong to the legacy "Clinical Exam" report category (rcID 8), which only the
// Initial Evaluation / Progress / Discharge templates carry — Daily Visit and No Show
// reports never capture vitals. Matched by category name (like the field affordances
// above) so tenant-customized templates keep the behavior.
const VITALS_CATEGORY = "clinical exam";
function typeSupportsVitals(fields: ReportFieldDto[]): boolean {
  return fields.some((f) => (f.category ?? "").trim().toLowerCase() === VITALS_CATEGORY);
}

function resolveProviderLabel(
  id: string | null | undefined,
  options: { value: string; label: string }[] | undefined,
): string {
  if (!id) return "—";
  return options?.find((o) => o.value === id)?.label ?? "the reviewer";
}

function toNum(value: string): number | null {
  if (value.trim() === "") return null;
  const n = Number(value);
  return Number.isFinite(n) ? n : null;
}

/** Fields grouped by category, preserving displayOrder and first-seen category order. */
function groupByCategory(fields: ReportFieldDto[]): { category: string; fields: ReportFieldDto[] }[] {
  const sorted = [...fields].sort((a, b) => a.displayOrder - b.displayOrder);
  const groups: { category: string; fields: ReportFieldDto[] }[] = [];
  for (const f of sorted) {
    const category = f.category?.trim() || "General";
    let group = groups.find((g) => g.category === category);
    if (!group) {
      group = { category, fields: [] };
      groups.push(group);
    }
    group.fields.push(f);
  }
  return groups;
}

export function ReportEditorPanel({
  patientId,
  reportId,
}: {
  patientId: string;
  reportId: string;
}) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canUpdate = user?.permissions?.includes(REPORT_PERMISSIONS.update) ?? false;
  const canSign = user?.permissions?.includes(REPORT_PERMISSIONS.sign) ?? false;
  const canReview = user?.permissions?.includes(REPORT_PERMISSIONS.review) ?? false;
  const canViewSuperBills = user?.permissions?.includes(SUPERBILL_PERMISSIONS.view) ?? false;

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId),
  });

  const reportQuery = useQuery({
    queryKey: ["report", reportId],
    queryFn: () => getReport(reportId),
  });

  const report = reportQuery.data;
  const isSigned = report?.isSigned ?? false;

  // The report's OWN incident, resolved from the report rather than inherited
  // from the chart's active incident: several reports from different incidents
  // can sit open at once, and the header has to name the one THIS report is
  // filed under.
  const incidentQuery = useQuery({
    queryKey: ["incident", report?.incidentId],
    queryFn: () => getPatientIncident(report!.incidentId),
    enabled: report != null,
  });

  // A report stays open across incident switches, so the report on screen may
  // belong to an incident OTHER than the one the chart is currently working in.
  // Editing it then would file changes against an incident the user isn't
  // looking at — the exact mix-up the DOIV/DOL labels warn about — so the whole
  // report goes read-only until its incident is made active again.
  //
  // A null activeIncidentId means "not resolved yet" (the chart picks one on
  // load), NOT "mismatch" — locking on that would flash a spurious banner.
  // Switching back goes through the chart's guard, not straight to
  // setActiveIncident: it locks whatever is open on the current incident, so the
  // user confirms that trade first.
  const { requestIncidentSwitch } = useIncidentSwitch();
  const activeIncidentId = usePatientTab(patientId)?.activeIncidentId ?? null;
  const isForeignIncident =
    report != null && activeIncidentId != null && report.incidentId !== activeIncidentId;

  // The dx codes shown on the report are the ones recorded on its OWN incident
  // (what the Diagnostic Codes dialog edits) — not the report's associated
  // problems. The incident stores bare ids, so they're resolved to codes here.
  const incidentDxIds = useMemo(
    () => incidentQuery.data?.diagnosticIds ?? [],
    [incidentQuery.data],
  );

  const dxQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", [...incidentDxIds].sort().join(",")],
    queryFn: () => listCustomDiagnostics({ ids: incidentDxIds, pageSize: 200 }),
    enabled: incidentDxIds.length > 0,
  });

  // Kept in the incident's own order, and any id that no longer resolves to a
  // live diagnostic is dropped rather than rendered as a blank chip.
  const incidentDx = useMemo(() => {
    const byId = new Map((dxQuery.data?.items ?? []).map((d) => [d.id, d]));
    return incidentDxIds.flatMap((id) => {
      const d = byId.get(id);
      return d ? [d] : [];
    });
  }, [incidentDxIds, dxQuery.data]);

  const fieldsQuery = useQuery({
    queryKey: ["report-fields", report?.reportTypeId],
    queryFn: () => listReportFields(report!.reportTypeId),
    enabled: report != null,
  });

  const providerOptions = useProviderOptions();
  const clinicOptions = useClinicOptions();

  // ─── Editable form state (hydrated from the loaded report) ───
  const [reportDate, setReportDate] = useState("");
  const [providerId, setProviderId] = useState<string | null>(null);
  const [clinicId, setClinicId] = useState<string | null>(null);
  const [isNoShow, setIsNoShow] = useState(false);
  const [height, setHeight] = useState("");
  const [weight, setWeight] = useState("");
  const [systolic, setSystolic] = useState("");
  const [diastolic, setDiastolic] = useState("");
  const [pulse, setPulse] = useState("");
  const [temperature, setTemperature] = useState("");
  const [values, setValues] = useState<Record<number, string>>({});
  const [addendumText, setAddendumText] = useState("");
  const [reviewerProviderId, setReviewerProviderId] = useState<string | null>(null);
  const [associatedProblemIds, setAssociatedProblemIds] = useState<string[]>([]);
  const [proceduresOpen, setProceduresOpen] = useState(false);
  // The Plan field the open Procedures Performed dialog was launched from; its macro
  // text inserts there rather than into a globally-resolved Plan field.
  const [proceduresFieldId, setProceduresFieldId] = useState<number | null>(null);
  // Non-null = the field id the Import Allergies / Import Medications dialog inserts into.
  const [allergiesFieldId, setAllergiesFieldId] = useState<number | null>(null);
  const [medicationsFieldId, setMedicationsFieldId] = useState<number | null>(null);

  // Refs to each field's textarea so macro-insert can splice at the caret.
  const fieldRefs = useRef<Record<number, HTMLTextAreaElement | null>>({});

  // ─── Draft persistence (Part C) ───
  // `suppress` arms before hydration: the snapshot change hydration causes
  // must NOT be written back as a draft — an untouched draft equal to the
  // server copy would later shadow genuinely newer server data.
  const suppressDraftWriteRef = useRef(false);
  const draftDirtyRef = useRef(false);
  // Which reportId the form state was last hydrated for. Until hydration
  // has run for the CURRENT report, the write effect must stay inert —
  // otherwise the still-empty initial state (or the previous report's
  // state right after a pill switch) gets scheduled as a draft and can
  // clobber a real one before the report query resolves.
  const hydratedForReportRef = useRef<string | null>(null);

  useEffect(() => {
    if (!report) return;
    suppressDraftWriteRef.current = true;
    draftDirtyRef.current = false;
    const draft = report.isSigned ? null : readReportDraft(reportId);
    if (draft) {
      // Unsaved local edits win over the server copy until Save/Sign.
      setReportDate(draft.reportDate);
      setProviderId(draft.providerId);
      setClinicId(draft.clinicId);
      setIsNoShow(draft.isNoShow);
      setHeight(draft.height);
      setWeight(draft.weight);
      setSystolic(draft.systolic);
      setDiastolic(draft.diastolic);
      setPulse(draft.pulse);
      setTemperature(draft.temperature);
      setValues(draft.values);
    } else {
      setReportDate(report.reportDate.slice(0, 10));
      setProviderId(report.providerId ?? null);
      setClinicId(report.clinicId ?? null);
      setIsNoShow(report.isNoShow);
      setHeight(report.vitals.heightInches != null ? String(report.vitals.heightInches) : "");
      setWeight(report.vitals.weightLbs != null ? String(report.vitals.weightLbs) : "");
      setSystolic(report.vitals.systolic != null ? String(report.vitals.systolic) : "");
      setDiastolic(report.vitals.diastolic != null ? String(report.vitals.diastolic) : "");
      setPulse(report.vitals.pulse != null ? String(report.vitals.pulse) : "");
      setTemperature(report.vitals.temperatureF != null ? String(report.vitals.temperatureF) : "");
      const map: Record<number, string> = {};
      for (const fv of report.fieldValues) map[fv.reportFieldId] = fv.text;
      setValues(map);
    }
    // Never drafted — these save through their own mutations (Request
    // Review / Save Associated Problems), so the server copy always wins.
    setReviewerProviderId(report.reviewerProviderId ?? null);
    setAssociatedProblemIds(report.associatedProblemIds ?? []);
    hydratedForReportRef.current = reportId;
  }, [report, reportId]);

  // One snapshot of everything the draft persists — the ONLY draft-write
  // instrumentation point (spec: instrument once, not per input).
  const draftSnapshot = useMemo<ReportDraft>(
    () => ({
      reportDate,
      providerId,
      clinicId,
      isNoShow,
      height,
      weight,
      systolic,
      diastolic,
      pulse,
      temperature,
      values,
    }),
    [
      reportDate,
      providerId,
      clinicId,
      isNoShow,
      height,
      weight,
      systolic,
      diastolic,
      pulse,
      temperature,
      values,
    ],
  );
  const draftSnapshotRef = useRef(draftSnapshot);
  draftSnapshotRef.current = draftSnapshot;

  const isSignedRef = useRef(isSigned);
  isSignedRef.current = isSigned;

  // Read via a ref for the same reason as isSigned: the draft effect below
  // keys off form state, not these flags, and must not re-run when they flip.
  const isForeignIncidentRef = useRef(isForeignIncident);
  isForeignIncidentRef.current = isForeignIncident;

  // Debounced draft write on any form change. The hydration effect above
  // arms `suppress`, so the snapshot change IT causes is skipped; only
  // real typing marks the draft dirty and schedules a write.
  useEffect(() => {
    if (hydratedForReportRef.current !== reportId) return;
    if (suppressDraftWriteRef.current) {
      suppressDraftWriteRef.current = false;
      return;
    }
    if (isSignedRef.current || !canUpdate || isForeignIncidentRef.current) return;
    draftDirtyRef.current = true;
    const t = window.setTimeout(() => {
      writeReportDraft(reportId, draftSnapshotRef.current);
      draftDirtyRef.current = false;
    }, DRAFT_WRITE_DEBOUNCE_MS);
    return () => window.clearTimeout(t);
  }, [draftSnapshot, reportId, canUpdate]);

  // Flush a pending (sub-debounce) write when the panel unmounts — e.g.
  // switching to another open report's pill, or client-side navigation
  // away from the chart. (A hard reload mid-debounce can lose <500ms of
  // typing — accepted; effect cleanups don't run on browser unload.)
  useEffect(() => {
    return () => {
      if (draftDirtyRef.current) {
        writeReportDraft(reportId, draftSnapshotRef.current);
        draftDirtyRef.current = false;
      }
    };
  }, [reportId]);

  const bmi = useMemo(() => computeBmi(toNum(height), toNum(weight)), [height, weight]);

  const groups = useMemo(() => groupByCategory(fieldsQuery.data ?? []), [fieldsQuery.data]);

  const supportsVitals = useMemo(
    () => typeSupportsVitals(fieldsQuery.data ?? []),
    [fieldsQuery.data],
  );

  // Procedures Performed macro text lands in the Plan field the dialog was opened from
  // (legacy behavior).
  const onProcedureMacroText = (text: string) => {
    if (proceduresFieldId == null) {
      toast.info("No Plan field on this report type — macro text was not inserted.");
      return;
    }
    insertMacro(proceduresFieldId, text);
  };

  const buildFieldValues = (): ReportFieldValue[] =>
    Object.entries(values)
      .filter(([, text]) => text.trim().length > 0)
      .map(([id, text]) => ({ reportFieldId: Number(id), text }));

  const saveMutation = useMutation({
    mutationFn: updateReport,
    onSuccess: () => {
      toast.success("Report saved.");
      clearReportDraft(reportId);
      draftDirtyRef.current = false;
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: (err) => toast.error("Failed to save report.", { description: describe(err) }),
  });

  const signMutation = useMutation({
    mutationFn: (id: string) => signReport(id),
    onSuccess: () => {
      toast.success("Report signed.");
      clearReportDraft(reportId);
      draftDirtyRef.current = false;
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: (err) => toast.error("Failed to sign report.", { description: describe(err) }),
  });

  const addendumMutation = useMutation({
    mutationFn: addAddendum,
    onSuccess: () => {
      toast.success("Addendum added.");
      setAddendumText("");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
    },
    onError: (err) => toast.error("Failed to add addendum.", { description: describe(err) }),
  });

  const problemsQuery = useQuery({
    queryKey: ["problems", patientId, "all-for-report"],
    queryFn: () =>
      searchPatientProblems({
        patientId,
        includeResolved: true,
        includeInactive: true,
        pageSize: 200,
      }),
  });
  const patientProblems = problemsQuery.data?.items ?? [];

  const setProblemsMutation = useMutation({
    mutationFn: (problemIds: string[]) => setReportProblems(reportId, problemIds),
    onSuccess: () => {
      toast.success("Associated problems saved.");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
    },
    onError: (err) => toast.error("Failed to save associated problems.", { description: describe(err) }),
  });

  const toggleProblem = (id: string) =>
    setAssociatedProblemIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id],
    );

  const requestReviewMutation = useMutation({
    mutationFn: requestReview,
    onSuccess: () => {
      toast.success("Review requested.");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: (err) => toast.error("Failed to request review.", { description: describe(err) }),
  });

  const reviewSignMutation = useMutation({
    mutationFn: (id: string) => reviewSign(id),
    onSuccess: () => {
      toast.success("Review signed.");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: (err) => toast.error("Failed to review-sign report.", { description: describe(err) }),
  });

  const onSave = () => {
    if (!reportId || !reportDate) return;
    // Belt-and-braces: the buttons are already gone when the report isn't on the
    // active incident, but never let a save slip through against another one.
    if (isForeignIncident) return;
    saveMutation.mutate({
      reportId,
      reportDate,
      providerId,
      clinicId,
      isNoShow,
      // A report type without a Clinical Exam category never records vitals, so
      // anything lingering in form state (e.g. an old draft) saves as null rather
      // than attaching vitals to a Daily Visit / No Show report.
      vitals: supportsVitals
        ? {
            heightInches: toNum(height),
            weightLbs: toNum(weight),
            bmi,
            systolic: toNum(systolic),
            diastolic: toNum(diastolic),
            pulse: toNum(pulse),
            temperatureF: toNum(temperature),
          }
        : {
            heightInches: null,
            weightLbs: null,
            bmi: null,
            systolic: null,
            diastolic: null,
            pulse: null,
            temperatureF: null,
          },
      fieldValues: buildFieldValues(),
    });
  };

  const onSign = () => {
    if (!reportId || isForeignIncident) return;
    // Persist any pending edits first, then sign.
    onSave();
    signMutation.mutate(reportId);
  };

  const insertMacro = (fieldId: number, text: string) => {
    setValues((prev) => {
      const current = prev[fieldId] ?? "";
      const ta = fieldRefs.current[fieldId];
      // Splice at the caret when the textarea is focused; otherwise append.
      if (ta && document.activeElement === ta) {
        const start = ta.selectionStart ?? current.length;
        const end = ta.selectionEnd ?? current.length;
        const next = current.slice(0, start) + text + current.slice(end);
        return { ...prev, [fieldId]: next };
      }
      const sep = current && !current.endsWith("\n") ? "\n" : "";
      return { ...prev, [fieldId]: current + sep + text };
    });
  };

  // Import the report's associated diagnoses into the field as "{code} - {description}"
  // lines (mirrors BackChart's "Import Dx Codes" on Clinical Impression / Assessment).
  const importDxCodes = (fieldId: number) => {
    const dx = patientProblems
      .filter((p) => associatedProblemIds.includes(p.id))
      .map((p) => `${p.diagnosticCode}${p.diagnosticDescription ? ` - ${p.diagnosticDescription}` : ""}`);
    if (dx.length === 0) {
      toast.info("No associated problems to import. Add them in the Associated Problems section first.");
      return;
    }
    insertMacro(fieldId, dx.join("\n"));
  };

  const fullName = patientQuery.data
    ? [
        patientQuery.data.demographics.firstName,
        patientQuery.data.demographics.middleInitial,
        patientQuery.data.demographics.lastName,
      ]
        .filter(Boolean)
        .join(" ")
    : "";

  if (reportQuery.isLoading) {
    return <div data-testid="report-editor-panel" className="skeleton h-64 rounded-xl" />;
  }

  if (!report) {
    return (
      <div
        data-testid="report-editor-panel"
        className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-6 text-[13px] text-[var(--color-muted-foreground)]"
      >
        Report not found.
      </div>
    );
  }

  const readOnly = isSigned || !canUpdate || isForeignIncident;
  const isPending = saveMutation.isPending || signMutation.isPending;

  return (
    <div data-testid="report-editor-panel" className="min-w-0 space-y-4 sm:space-y-6">
      {/* Patient + status strip */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <div className="flex items-center gap-3">
          <div className="grid size-10 place-items-center rounded-xl bg-[var(--color-primary-soft)] text-[var(--color-primary)]">
            <FileText className="size-5" />
          </div>
          <div>
            <p className="text-[15px] font-semibold leading-tight">{fullName || "Patient"}</p>
            {/* Which incident this report is filed under — sits with the patient
                name so an open report can never be mistaken for one belonging to
                another incident. */}
            <IncidentRef
              incident={incidentQuery.data}
              testId="report-incident-ref"
              className="block text-[11px] font-medium"
            />
            {/* What this visit is being coded against, in view while the note is
                written. Renders even when empty — an absent line reads as "still
                loading" rather than "no codes on this incident". */}
            <p
              data-testid="report-dx-codes"
              className="mt-0.5 flex flex-wrap items-center gap-x-2 text-[11px] text-[var(--color-muted-foreground)]"
            >
              <span className="font-semibold uppercase tracking-wider">Dx Codes</span>
              {dxQuery.isLoading ? null : incidentDx.length === 0 ? (
                <span>—</span>
              ) : (
                incidentDx.map((d) => (
                  <span
                    key={d.id}
                    title={d.description ?? d.longDescription ?? d.code}
                    className="font-medium tabular-nums text-[var(--color-foreground)]"
                  >
                    {d.code}
                  </span>
                ))
              )}
            </p>
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              Report · {formatDate(report.reportDate)}
              {report.version > 1 ? ` · v${report.version}` : ""}
            </p>
          </div>
        </div>
        <span
          className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[11px] font-semibold uppercase tracking-wider ${
            isSigned
              ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
              : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
          }`}
        >
          {report.workflowStatus}
        </span>
      </div>

      {/* Locked out: this report is filed under an incident the chart isn't
          working in. Saving here would write against the wrong incident, so the
          form is read-only until the user switches to the report's own incident
          — which is one click away rather than a hunt through the chooser. */}
      {isForeignIncident && (
        <div
          data-testid="foreign-incident-notice"
          className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] p-4"
        >
          <div className="flex items-start gap-2">
            <AlertTriangle className="mt-0.5 size-4 shrink-0 text-[var(--color-destructive)]" />
            <div className="min-w-0">
              <p className="text-[13px] font-semibold text-[var(--color-destructive)]">
                Read-only — a different incident is selected
              </p>
              <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
                This report belongs to another incident, so it can't be edited or signed
                from here. Switch to its incident to make changes.
              </p>
            </div>
          </div>
          <Button
            size="sm"
            variant="outline"
            className="shrink-0"
            onClick={() => requestIncidentSwitch(report.incidentId)}
          >
            Switch to this incident
          </Button>
        </div>
      )}

      {/* Header fields */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <div className="grid gap-3 sm:grid-cols-3">
          <Field id="rpt-date" label="Report Date" required>
            <Input
              id="rpt-date"
              type="date"
              value={reportDate}
              onChange={(e) => setReportDate(e.target.value)}
              disabled={readOnly}
            />
          </Field>
          <Field id="rpt-provider" label="Provider">
            <Combobox
              id="rpt-provider"
              label="Provider"
              value={providerId}
              onChange={setProviderId}
              options={providerOptions ?? []}
              placeholder="Select provider…"
              searchable
              clearable
              disabled={readOnly}
            />
          </Field>
          <Field id="rpt-clinic" label="Clinic">
            <Combobox
              id="rpt-clinic"
              label="Clinic"
              value={clinicId}
              onChange={setClinicId}
              options={clinicOptions ?? []}
              placeholder="Select clinic…"
              searchable
              clearable
              disabled={readOnly}
            />
          </Field>
        </div>
        <label className="mt-3 flex w-fit items-center gap-2 text-[13px] cursor-pointer">
          <input
            type="checkbox"
            checked={isNoShow}
            onChange={(e) => setIsNoShow(e.target.checked)}
            disabled={readOnly}
            className="rounded border-[var(--color-border)]"
          />
          <span>No Show</span>
        </label>
      </div>

      {/* Vitals — only for report types whose template includes the Clinical Exam
          category (legacy: Initial Evaluation / Progress / Discharge; never Daily
          Visit or No Show). */}
      {supportsVitals && (
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <h3 className="mb-3 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
          Vitals
        </h3>
        <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-7">
          <Field id="v-height" label="Height (in)">
            <Input id="v-height" inputMode="decimal" value={height} onChange={(e) => setHeight(e.target.value)} disabled={readOnly} />
          </Field>
          <Field id="v-weight" label="Weight (lb)">
            <Input id="v-weight" inputMode="decimal" value={weight} onChange={(e) => setWeight(e.target.value)} disabled={readOnly} />
          </Field>
          <Field id="v-bmi" label="BMI">
            <Input id="v-bmi" value={bmi != null ? String(bmi) : ""} readOnly disabled placeholder="—" />
          </Field>
          <Field id="v-sys" label="Systolic">
            <Input id="v-sys" inputMode="numeric" value={systolic} onChange={(e) => setSystolic(e.target.value)} disabled={readOnly} />
          </Field>
          <Field id="v-dia" label="Diastolic">
            <Input id="v-dia" inputMode="numeric" value={diastolic} onChange={(e) => setDiastolic(e.target.value)} disabled={readOnly} />
          </Field>
          <Field id="v-pulse" label="Pulse">
            <Input id="v-pulse" inputMode="numeric" value={pulse} onChange={(e) => setPulse(e.target.value)} disabled={readOnly} />
          </Field>
          <Field id="v-temp" label="Temp (°F)">
            <Input id="v-temp" inputMode="decimal" value={temperature} onChange={(e) => setTemperature(e.target.value)} disabled={readOnly} />
          </Field>
        </div>
      </div>
      )}

      {/* Field sections grouped by category */}
      {fieldsQuery.isLoading ? (
        <div className="skeleton h-40 rounded-xl" />
      ) : (
        groups.map((group) => (
          <div key={group.category} className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
            <h3 className="mb-3 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
              {group.category}
            </h3>
            <div className="space-y-4">
              {group.fields.map((f) => (
                <div key={f.id} className="space-y-1.5">
                  <div className="flex items-center justify-between gap-2">
                    <label
                      htmlFor={`f-${f.id}`}
                      className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]"
                    >
                      {f.name}
                    </label>
                    {((canViewSuperBills && fieldIsPlan(f.name)) || !readOnly) && (
                      <div className="flex items-center gap-1.5">
                        {/* Legacy parity: the super bill stays editable after the report
                            is signed, so this affordance ignores readOnly. */}
                        {canViewSuperBills && fieldIsPlan(f.name) && (
                          <button
                            type="button"
                            title="Record the procedures performed for this report"
                            onClick={() => {
                              setProceduresFieldId(f.id);
                              setProceduresOpen(true);
                            }}
                            className="inline-flex h-7 items-center gap-1 rounded-md border border-[var(--color-border)] px-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
                          >
                            <ClipboardList className="size-3.5" />
                            Procedures Performed
                          </button>
                        )}
                        {!readOnly && fieldImportsAllergies(f.name) && (
                          <button
                            type="button"
                            title="Import the patient's allergies into this field"
                            onClick={() => setAllergiesFieldId(f.id)}
                            className="inline-flex h-7 items-center gap-1 rounded-md border border-[var(--color-border)] px-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
                          >
                            <Pill className="size-3.5" />
                            Import Allergies
                          </button>
                        )}
                        {!readOnly && fieldImportsMedications(f.name) && (
                          <button
                            type="button"
                            title="Import the patient's medications into this field"
                            onClick={() => setMedicationsFieldId(f.id)}
                            className="inline-flex h-7 items-center gap-1 rounded-md border border-[var(--color-border)] px-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
                          >
                            <Tablets className="size-3.5" />
                            Import Medications
                          </button>
                        )}
                        {!readOnly && fieldImportsDx(f.name) && (
                          <button
                            type="button"
                            title="Import the report's associated diagnoses into this field"
                            onClick={() => importDxCodes(f.id)}
                            className="inline-flex h-7 items-center gap-1 rounded-md border border-[var(--color-border)] px-2 text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)] transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]"
                          >
                            <FileDown className="size-3.5" />
                            Import Dx Codes
                          </button>
                        )}
                        {!readOnly && (
                          <MacroInsert
                            reportFieldId={f.id}
                            fieldName={f.name}
                            value={values[f.id] ?? ""}
                            onCommit={(text) => setValues((prev) => ({ ...prev, [f.id]: text }))}
                          />
                        )}
                      </div>
                    )}
                  </div>
                  <Textarea
                    id={`f-${f.id}`}
                    ref={(el) => {
                      fieldRefs.current[f.id] = el;
                    }}
                    value={values[f.id] ?? ""}
                    onChange={(e) => setValues((prev) => ({ ...prev, [f.id]: e.target.value }))}
                    rows={4}
                    maxLength={16000}
                    disabled={readOnly}
                    placeholder={readOnly ? "—" : `Enter ${f.name.toLowerCase()}…`}
                  />
                </div>
              ))}
            </div>
          </div>
        ))
      )}

      {/* Associated Problems */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <h3 className="mb-3 flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
          <Stethoscope className="size-3.5" />
          Associated Problems
        </h3>
        {problemsQuery.isLoading ? (
          <div className="skeleton h-12 rounded-lg" />
        ) : patientProblems.length === 0 ? (
          <p className="text-[12px] text-[var(--color-muted-foreground)]">
            This patient has no problems to associate.
          </p>
        ) : (
          <>
            <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
              {patientProblems.map((p) => (
                <li key={p.id}>
                  <label className="flex cursor-pointer items-center gap-3 px-3 py-2 text-[13px]">
                    <input
                      type="checkbox"
                      checked={associatedProblemIds.includes(p.id)}
                      onChange={() => toggleProblem(p.id)}
                      disabled={!canUpdate || isForeignIncident}
                      className="rounded border-[var(--color-border)]"
                    />
                    <span className="min-w-0 flex-1">
                      <span className="font-medium">{p.diagnosticCode}</span>
                      {p.diagnosticDescription ? (
                        <span className="text-[var(--color-muted-foreground)]"> — {p.diagnosticDescription}</span>
                      ) : null}
                    </span>
                    <span className="shrink-0 text-[11px] uppercase tracking-wider text-[var(--color-muted-foreground)]">
                      {p.status}
                    </span>
                  </label>
                </li>
              ))}
            </ul>
            {canUpdate && !isForeignIncident && (
              <Button
                size="sm"
                className="mt-3"
                variant="outline"
                disabled={setProblemsMutation.isPending}
                onClick={() => setProblemsMutation.mutate(associatedProblemIds)}
              >
                <Save className="size-4" />
                {setProblemsMutation.isPending ? "Saving…" : "Save Associated Problems"}
              </Button>
            )}
          </>
        )}
      </div>

      {/* Signature (read-only snapshot) */}
      {isSigned && (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <h3 className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
            Signature
          </h3>
          <p className="text-[13px]">
            Signed by <span className="font-medium">{report.signedByName ?? "—"}</span>
            {" · "}
            <span className="text-[var(--color-muted-foreground)]">
              {formatDateTimeMono(report.signedOnUtc)}
            </span>
          </p>
          {report.signatureImageUrl && (
            <img
              src={report.signatureImageUrl}
              alt="Signature"
              className="mt-2 max-h-24 rounded-md border border-[var(--color-border)] bg-white p-1"
            />
          )}
        </div>
      )}

      {/* Review workflow */}
      {isSigned && (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <h3 className="mb-3 flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
            <UserCheck className="size-3.5" />
            Peer Review
          </h3>

          {report.workflowStatus === "Reviewed" ? (
            <div className="space-y-2">
              <p className="flex items-center gap-1.5 text-[13px]">
                <CheckCircle2 className="size-4 text-[var(--color-primary)]" />
                Reviewed by <span className="font-medium">{report.reviewSignedByName ?? "—"}</span>
                {" · "}
                <span className="text-[var(--color-muted-foreground)]">
                  {formatDateTimeMono(report.reviewSignedOnUtc)}
                </span>
              </p>
              {report.reviewSignatureImageUrl && (
                <img
                  src={report.reviewSignatureImageUrl}
                  alt="Reviewer signature"
                  className="max-h-24 rounded-md border border-[var(--color-border)] bg-white p-1"
                />
              )}
            </div>
          ) : report.workflowStatus === "ReviewRequested" ? (
            <div className="space-y-3">
              <p className="text-[13px] text-[var(--color-muted-foreground)]">
                Review requested on {formatDateTimeMono(report.reviewRequestedOnUtc)}
                {report.reviewerProviderId
                  ? ` for ${resolveProviderLabel(report.reviewerProviderId, providerOptions)}`
                  : ""}
                .
              </p>
              {canReview && !isForeignIncident && (
                <Button
                  size="sm"
                  disabled={reviewSignMutation.isPending}
                  onClick={() => reviewSignMutation.mutate(reportId)}
                >
                  <PenLine className="size-4" />
                  {reviewSignMutation.isPending ? "Signing…" : "Review-Sign"}
                </Button>
              )}
            </div>
          ) : canReview && !isForeignIncident ? (
            <div className="flex flex-wrap items-end gap-3">
              <div className="w-64">
                <Field id="rpt-reviewer" label="Reviewer">
                  <Combobox
                    id="rpt-reviewer"
                    label="Reviewer"
                    value={reviewerProviderId}
                    onChange={setReviewerProviderId}
                    options={providerOptions ?? []}
                    placeholder="Select reviewer…"
                    searchable
                  />
                </Field>
              </div>
              <Button
                size="sm"
                disabled={!reviewerProviderId || requestReviewMutation.isPending}
                onClick={() =>
                  reviewerProviderId &&
                  requestReviewMutation.mutate({ reportId, reviewerProviderId })
                }
              >
                <UserCheck className="size-4" />
                {requestReviewMutation.isPending ? "Requesting…" : "Request Review"}
              </Button>
            </div>
          ) : (
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              No review has been requested for this report.
            </p>
          )}
        </div>
      )}

      {/* Addendums */}
      {isSigned && (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <h3 className="mb-3 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
            Addendums
          </h3>
          {report.addendums.length === 0 ? (
            <p className="text-[12px] text-[var(--color-muted-foreground)]">No addendums yet.</p>
          ) : (
            <ul className="space-y-3">
              {report.addendums.map((a) => (
                <li key={a.id} className="rounded-lg border border-[var(--color-border)] p-3">
                  <p className="whitespace-pre-wrap text-[13px]">{a.text}</p>
                  <p className="mt-1.5 text-[11px] text-[var(--color-muted-foreground)]">
                    {a.createdByName ?? "—"} · {formatDateTimeMono(a.createdAtUtc)}
                  </p>
                </li>
              ))}
            </ul>
          )}

          {canUpdate && !isForeignIncident && (
            <div className="mt-3 space-y-2">
              <Textarea
                value={addendumText}
                onChange={(e) => setAddendumText(e.target.value)}
                rows={3}
                maxLength={16000}
                placeholder="Add an addendum…"
              />
              <Button
                size="sm"
                disabled={!addendumText.trim() || addendumMutation.isPending}
                onClick={() => addendumMutation.mutate({ reportId, text: addendumText.trim() })}
              >
                <Plus className="size-4" />
                Add Addendum
              </Button>
            </div>
          )}
        </div>
      )}

      {/* Action bar (draft only) — gone entirely when the report belongs to a
          different incident than the chart's active one. */}
      {!isSigned && !isForeignIncident && (canUpdate || canSign) && (
        <div className="sticky bottom-4 flex items-center justify-end gap-2 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3 shadow-md">
          {canUpdate && (
            <Button variant="outline" onClick={onSave} disabled={isPending}>
              <Save className="size-4" />
              {saveMutation.isPending ? "Saving…" : "Save Draft"}
            </Button>
          )}
          {canSign && (
            <Button onClick={onSign} disabled={isPending}>
              <PenLine className="size-4" />
              {signMutation.isPending ? "Signing…" : "Sign Report"}
            </Button>
          )}
        </div>
      )}

      {canViewSuperBills && (
        <ProceduresPerformedDialog
          patientId={patientId}
          patientName={fullName || undefined}
          incidentId={report.incidentId}
          reportId={reportId}
          open={proceduresOpen}
          onClose={() => setProceduresOpen(false)}
          // No macro-text option when the field can't be edited (signed report / no update
          // permission) — inserted text would render into a disabled textarea and silently
          // evaporate on reload since Save is unreachable.
          onMacroText={readOnly ? undefined : onProcedureMacroText}
        />
      )}

      <ImportAllergiesDialog
        patientId={patientId}
        open={allergiesFieldId != null}
        onClose={() => setAllergiesFieldId(null)}
        onDone={(text) => {
          if (allergiesFieldId != null) insertMacro(allergiesFieldId, text);
        }}
      />

      <ImportMedicationsDialog
        patientId={patientId}
        open={medicationsFieldId != null}
        onClose={() => setMedicationsFieldId(null)}
        onDone={(text) => {
          if (medicationsFieldId != null) insertMacro(medicationsFieldId, text);
        }}
      />
    </div>
  );
}
