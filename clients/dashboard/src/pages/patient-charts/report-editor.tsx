import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, FileText, PenLine, Plus, Save } from "lucide-react";
import { toast } from "sonner";
import { getPatientById } from "@/api/patients";
import {
  listReportFields,
  useClinicOptions,
  useProviderOptions,
  type ReportFieldDto,
} from "@/api/administration";
import {
  addAddendum,
  getReport,
  signReport,
  updateReport,
  type ReportFieldValue,
} from "@/api/reports";
import { REPORT_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { MacroInsert } from "@/components/ui/macro-insert";
import { Combobox, Field } from "@/components/list";
import { describe, formatDate, formatDateTimeMono } from "@/lib/list-helpers";

/** lbs + inches → BMI (rounded to 1 decimal); null when either is missing/0. */
function computeBmi(heightInches: number | null, weightLbs: number | null): number | null {
  if (!heightInches || !weightLbs) return null;
  const bmi = (weightLbs / (heightInches * heightInches)) * 703;
  if (!Number.isFinite(bmi)) return null;
  return Math.round(bmi * 10) / 10;
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

export function ReportEditorPage() {
  const { patientId, reportId } = useParams<{ patientId: string; reportId: string }>();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canUpdate = user?.permissions?.includes(REPORT_PERMISSIONS.update) ?? false;
  const canSign = user?.permissions?.includes(REPORT_PERMISSIONS.sign) ?? false;

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId!),
    enabled: !!patientId,
  });

  const reportQuery = useQuery({
    queryKey: ["report", reportId],
    queryFn: () => getReport(reportId!),
    enabled: !!reportId,
  });

  const report = reportQuery.data;
  const isSigned = report?.isSigned ?? false;

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

  // Refs to each field's textarea so macro-insert can splice at the caret.
  const fieldRefs = useRef<Record<number, HTMLTextAreaElement | null>>({});

  useEffect(() => {
    if (!report) return;
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
  }, [report]);

  const bmi = useMemo(() => computeBmi(toNum(height), toNum(weight)), [height, weight]);

  const groups = useMemo(() => groupByCategory(fieldsQuery.data ?? []), [fieldsQuery.data]);

  const buildFieldValues = (): ReportFieldValue[] =>
    Object.entries(values)
      .filter(([, text]) => text.trim().length > 0)
      .map(([id, text]) => ({ reportFieldId: Number(id), text }));

  const saveMutation = useMutation({
    mutationFn: updateReport,
    onSuccess: () => {
      toast.success("Report saved.");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
    onError: (err) => toast.error("Failed to save report.", { description: describe(err) }),
  });

  const signMutation = useMutation({
    mutationFn: (id: string) => signReport(id),
    onSuccess: () => {
      toast.success("Report signed.");
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

  const onSave = () => {
    if (!reportId || !reportDate) return;
    saveMutation.mutate({
      reportId,
      reportDate,
      providerId,
      clinicId,
      isNoShow,
      vitals: {
        heightInches: toNum(height),
        weightLbs: toNum(weight),
        bmi,
        systolic: toNum(systolic),
        diastolic: toNum(diastolic),
        pulse: toNum(pulse),
        temperatureF: toNum(temperature),
      },
      fieldValues: buildFieldValues(),
    });
  };

  const onSign = () => {
    if (!reportId) return;
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
    return <div className="h-64 animate-pulse rounded-xl bg-[var(--color-muted)]" />;
  }

  if (!report) {
    return (
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-6 text-[13px] text-[var(--color-muted-foreground)]">
        Report not found.
      </div>
    );
  }

  const readOnly = isSigned || !canUpdate;
  const isPending = saveMutation.isPending || signMutation.isPending;

  return (
    <div className="space-y-4 sm:space-y-6">
      <Link
        to={`/patient-charts/${patientId}`}
        className="inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      >
        <ArrowLeft className="size-4" />
        Back to Chart
      </Link>

      {/* Patient + status strip */}
      <div className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
        <div className="flex items-center gap-3">
          <div className="grid size-10 place-items-center rounded-xl bg-[var(--color-primary-soft)] text-[var(--color-primary)]">
            <FileText className="size-5" />
          </div>
          <div>
            <p className="text-[15px] font-semibold leading-tight">{fullName || "Patient"}</p>
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

      {/* Vitals */}
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

      {/* Field sections grouped by category */}
      {fieldsQuery.isLoading ? (
        <div className="h-40 animate-pulse rounded-xl bg-[var(--color-muted)]" />
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
                    {!readOnly && (
                      <MacroInsert reportFieldId={f.id} onInsert={(text) => insertMacro(f.id, text)} />
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

          {canUpdate && (
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
                onClick={() =>
                  reportId && addendumMutation.mutate({ reportId, text: addendumText.trim() })
                }
              >
                <Plus className="size-4" />
                Add Addendum
              </Button>
            </div>
          )}
        </div>
      )}

      {/* Action bar (draft only) */}
      {!isSigned && (canUpdate || canSign) && (
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
    </div>
  );
}
