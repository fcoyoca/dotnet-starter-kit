import { useEffect, useMemo, useRef, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ClipboardList,
  FileText,
  Pencil,
  Plus,
  Receipt,
  Save,
  Stethoscope,
  User,
  X,
} from "lucide-react";
import { toast } from "sonner";
import { getPatientIncident } from "@/api/incidents";
import { searchPatientReports, getReport, updateReport, type ReportFieldValue } from "@/api/reports";
import { getPatientById } from "@/api/patients";
import {
  pickPrimaryInsuranceType,
  searchPatientInsurancePolicies,
  type PatientInsurancePolicy,
} from "@/api/patient-insurance";
import {
  listCustomDiagnostics,
  listInsuranceTypeProcedures,
  listProcedureCodes,
  listReportFields,
  listReportTypes,
  useDepartmentOptions,
  useIncidentTypeOptions,
  useInsuranceTypeOptions,
  useProcedureCategoryOptions,
  useProviderOptions,
} from "@/api/administration";
import {
  getReportProcedures,
  setReportProcedures,
  type ReportProcedureInput,
} from "@/api/report-procedures";
import { SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field } from "@/components/list";
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { fieldIsPlan } from "@/lib/report-plan";
import { DiagnosticCodesDialog } from "@/pages/patient-charts/diagnostic-codes-dialog";
import { ViewBillDialog } from "@/pages/billing/view-bill-dialog";
import { MiniCard, MiniDxRow, MiniRow } from "@/pages/billing/mini-info";
import type { BillSummaryData } from "@/pages/billing/bill-summary";

type Props = {
  patientId: string;
  /** Shown in the nested Diagnostic Codes dialog's add-to-problems confirm. */
  patientName?: string;
  incidentId: string;
  /** When null (chart-shortcut context) the dialog shows a report-picker phase first. */
  reportId: string | null;
  open: boolean;
  onClose(): void;
  /**
   * Report-editor context only: receives a picked procedure's macro text for the Plan field.
   * When provided, the report editor owns the Plan field, so this dialog routes macro text to
   * it and does NOT render/persist its own Plan editor (avoids double-writes to the report).
   */
  onMacroText?: (text: string) => void;
};

/** One editable procedure row. `charge` stays a string while editing; parsed on save. */
type ProcRow = {
  key: number;
  procedureCodeId: string;
  code: string;
  description: string | null;
  charge: string;
  dxIds: string[];
};

export function ProceduresPerformedDialog({
  patientId,
  patientName,
  incidentId,
  reportId,
  open,
  onClose,
  onMacroText,
}: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canManage = user?.permissions?.includes(SUPERBILL_PERMISSIONS.manage) ?? false;

  // Two-phase: chart context picks a report first; report-editor context skips the picker.
  const [pickedReportId, setPickedReportId] = useState<string | null>(reportId);
  const activeReportId = reportId ?? pickedReportId;
  // In-chart context (no editor mounted) is the one that owns/persists the Plan field itself.
  const ownsPlan = !onMacroText;

  const [rows, setRows] = useState<ProcRow[]>([]);
  const [hydratedFor, setHydratedFor] = useState<string | null>(null);
  const [addMacroText, setAddMacroText] = useState(true);
  const [insuranceTypeId, setInsuranceTypeId] = useState<string | null>(null);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [dxDialogOpen, setDxDialogOpen] = useState(false);
  const [viewBillOpen, setViewBillOpen] = useState(false);
  const [planText, setPlanText] = useState("");
  const [planHydratedFor, setPlanHydratedFor] = useState<string | null>(null);
  const [planDirty, setPlanDirty] = useState(false);
  const rowKey = useRef(0);

  useEffect(() => {
    if (!open) {
      setPickedReportId(reportId);
      setRows([]);
      setHydratedFor(null);
      setDxDialogOpen(false);
      setViewBillOpen(false);
      setPlanText("");
      setPlanHydratedFor(null);
      setPlanDirty(false);
    }
  }, [open, reportId]);

  // ── Report picker phase (chart-shortcut context) ──
  const reportsQuery = useQuery({
    queryKey: ["reports", incidentId],
    queryFn: () => searchPatientReports({ incidentId, pageSize: 100 }),
    enabled: open && reportId === null && pickedReportId === null,
    placeholderData: keepPreviousData,
  });

  // ── Patient (mini-card + View Bill header) ──
  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: open,
  });

  // ── Incident (mini-card + dx checkbox axis) ──
  const incidentQuery = useQuery({
    queryKey: ["incident", incidentId],
    queryFn: () => getPatientIncident(incidentId),
    enabled: open,
  });
  const dxIds = useMemo(() => incidentQuery.data?.diagnosticIds ?? [], [incidentQuery.data]);

  const dxDetailsQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", [...dxIds].sort().join(",")],
    queryFn: () => listCustomDiagnostics({ ids: dxIds, pageSize: 200 }),
    enabled: open && dxIds.length > 0,
  });
  const dxLabel = useMemo(() => {
    const map = new Map<string, { code: string; description: string | null }>();
    for (const d of dxDetailsQuery.data?.items ?? []) {
      map.set(d.id, { code: d.code, description: d.description ?? null });
    }
    return map;
  }, [dxDetailsQuery.data]);

  // ── Active report detail (mini-card + Plan field source of truth) ──
  const reportDetailQuery = useQuery({
    queryKey: ["report", activeReportId],
    queryFn: () => getReport(activeReportId!),
    enabled: open && !!activeReportId,
  });
  const reportDetail = reportDetailQuery.data;
  const reportSigned = reportDetail?.isSigned ?? false;

  const reportFieldsQuery = useQuery({
    queryKey: ["report-fields", reportDetail?.reportTypeId],
    queryFn: () => listReportFields(reportDetail!.reportTypeId),
    enabled: open && reportDetail != null,
  });
  const planFieldId = useMemo(
    () => reportFieldsQuery.data?.find((f) => fieldIsPlan(f.name))?.id ?? null,
    [reportFieldsQuery.data],
  );

  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
    enabled: open,
  });
  const reportTypeName =
    reportTypesQuery.data?.find((t) => t.id === reportDetail?.reportTypeId)?.name ?? "—";

  const providerOptions = useProviderOptions();
  const providerName = useMemo(
    () => providerOptions?.find((o) => o.value === reportDetail?.providerId)?.label ?? null,
    [providerOptions, reportDetail?.providerId],
  );
  const incidentTypeOptions = useIncidentTypeOptions();
  const incidentTypeName = useMemo(
    () => incidentTypeOptions?.find((o) => o.value === incidentQuery.data?.incidentTypeId)?.label ?? null,
    [incidentTypeOptions, incidentQuery.data?.incidentTypeId],
  );
  const departmentOptions = useDepartmentOptions();
  const departmentName = useMemo(
    () => departmentOptions?.find((o) => o.value === incidentQuery.data?.departmentId)?.label ?? null,
    [departmentOptions, incidentQuery.data?.departmentId],
  );

  // ── Existing super bill (hydrates rows once per report) ──
  const proceduresQuery = useQuery({
    queryKey: ["report-procedures", activeReportId],
    queryFn: () => getReportProcedures(activeReportId!),
    enabled: open && !!activeReportId,
  });

  useEffect(() => {
    if (!open || !activeReportId || !proceduresQuery.data) return;
    if (hydratedFor === activeReportId) return;
    setRows(
      proceduresQuery.data.procedures.map((p) => ({
        key: rowKey.current++,
        procedureCodeId: p.procedureCodeId,
        code: p.code,
        description: p.description ?? null,
        charge: String(p.charge),
        dxIds: p.diagnosticIds,
      })),
    );
    // An existing bill's stored insurance type wins over the patient-primary default below —
    // it's what the report was actually priced under. Absent (legacy/new bill), leave it null
    // so the patient-primary effect fills it in.
    if (proceduresQuery.data.insuranceTypeId) {
      setInsuranceTypeId(proceduresQuery.data.insuranceTypeId);
    }
    setHydratedFor(activeReportId);
  }, [open, activeReportId, proceduresQuery.data, hydratedFor]);

  // Keep each procedure's linked DX pointers in sync with the incident's current DX set: when a
  // diagnosis is removed from the incident (via Edit Dx Codes), drop it from every procedure line
  // so the bill never saves a pointer to a diagnosis no longer on the incident. Mirrors legacy
  // BackChart's AddCodesToExistingProcedures. Prune-only — newly added DX codes stay opt-in.
  // Gated on a loaded incident so a transient empty set during load can't wipe hydrated pointers.
  useEffect(() => {
    if (!open || !incidentQuery.isSuccess) return;
    const valid = new Set(dxIds);
    setRows((prev) => {
      let changed = false;
      const next = prev.map((r) => {
        const filtered = r.dxIds.filter((id) => valid.has(id));
        if (filtered.length !== r.dxIds.length) {
          changed = true;
          return { ...r, dxIds: filtered };
        }
        return r;
      });
      return changed ? next : prev;
    });
  }, [open, incidentQuery.isSuccess, dxIds]);

  // Seed the Plan editor from the report's stored Plan field, once per report (chart context).
  useEffect(() => {
    if (!open || !ownsPlan || !activeReportId || planFieldId == null || !reportDetail) return;
    if (planHydratedFor === activeReportId) return;
    const existing = reportDetail.fieldValues.find((f) => f.reportFieldId === planFieldId)?.text ?? "";
    setPlanText(existing);
    setPlanDirty(false);
    setPlanHydratedFor(activeReportId);
  }, [open, ownsPlan, activeReportId, planFieldId, reportDetail, planHydratedFor]);

  // ── Picker: insurance type + category → codes with negotiated prices ──
  const insuranceOptions = useInsuranceTypeOptions();
  const categoryOptions = useProcedureCategoryOptions();

  // The patient's policies — used to default the picker to their primary insurance type,
  // matching legacy BackChart's superbill behavior. Same query key as the chart sidebar, so
  // it's served from cache when the chart already loaded it.
  const policiesQuery = useQuery({
    queryKey: ["patient-insurance-policies", patientId, false],
    queryFn: () => searchPatientInsurancePolicies({ patientId, pageSize: 200 }),
    enabled: open,
  });

  // Default the insurance type to the patient's primary active policy's type — but only when it
  // maps to a known (active) admin option. When the patient has no insurance on file (or its
  // type is inactive/unknown) we leave the picker unset so the user consciously chooses one,
  // rather than silently pricing under an arbitrary payer. Waits for the policies query so the
  // patient's primary wins over list order. User can still override.
  useEffect(() => {
    if (!open || insuranceTypeId !== null) return;
    if (policiesQuery.isPending) return;
    const primary = pickPrimaryInsuranceType(policiesQuery.data?.items ?? []);
    if (primary && insuranceOptions?.some((o) => o.value === primary.id)) {
      setInsuranceTypeId(primary.id);
    }
  }, [open, insuranceOptions, insuranceTypeId, policiesQuery.isPending, policiesQuery.data]);

  const codesQuery = useQuery({
    queryKey: ["procedure-codes", "picker", categoryId ?? "all"],
    queryFn: () =>
      listProcedureCodes({
        procedureCategoryId: categoryId ?? undefined,
        isActive: true,
        pageSize: 200,
        sortBy: "code",
      }),
    enabled: open && !!activeReportId,
    placeholderData: keepPreviousData,
  });

  const pricesQuery = useQuery({
    queryKey: ["insurance-type-procedures", insuranceTypeId],
    queryFn: () => listInsuranceTypeProcedures(insuranceTypeId!),
    enabled: open && !!insuranceTypeId,
  });
  const priceByCodeId = useMemo(() => {
    const map = new Map<string, number>();
    for (const p of pricesQuery.data ?? []) map.set(p.procedureCodeId, p.price);
    return map;
  }, [pricesQuery.data]);

  // ── Insurance for mini-card + View Bill (highest-priority active policies) ──
  const primaryPolicy = useMemo<PatientInsurancePolicy | null>(() => {
    const active = (policiesQuery.data?.items ?? []).filter((p) => p.isActive);
    return active.find((p) => p.priority === "Primary") ?? active[0] ?? null;
  }, [policiesQuery.data]);
  const secondaryPolicy = useMemo<PatientInsurancePolicy | null>(() => {
    const active = (policiesQuery.data?.items ?? []).filter((p) => p.isActive);
    return (
      active.find((p) => p.priority === "Secondary") ??
      active.find((p) => p.id !== primaryPolicy?.id) ??
      null
    );
  }, [policiesQuery.data, primaryPolicy]);

  // ── Row operations ──
  const appendMacro = (text: string) => {
    if (onMacroText) {
      onMacroText(text); // editor context: route into the report editor's Plan field
      return;
    }
    // chart context: skip when there's no Plan field, or the signed report can't be edited
    if (planFieldId == null || reportSigned) return;
    setPlanText((prev) => (prev && !prev.endsWith("\n") ? `${prev}\n${text}` : `${prev}${text}`));
    setPlanDirty(true);
  };

  const addProcedure = (codeId: string) => {
    const picked = codesQuery.data?.items.find((c) => c.id === codeId);
    if (!picked) return;
    if (insuranceTypeId && pricesQuery.isPending) {
      toast.info("Please wait — procedure prices are still loading.");
      return;
    }
    if (dxIds.length === 0) {
      toast.warning("Please add at least one DX code before picking procedures.");
      return;
    }
    const price = priceByCodeId.get(picked.id);
    setRows((prev) => [
      ...prev,
      {
        key: rowKey.current++,
        procedureCodeId: picked.id,
        code: picked.code,
        description: picked.description ?? picked.name ?? null,
        charge: String(price ?? 0),
        dxIds: [...dxIds], // legacy default: the full incident dx set, checked
      },
    ]);
    if (addMacroText && picked.macroText) {
      appendMacro(picked.macroText);
    }
  };

  const removeRow = (key: number) => setRows((prev) => prev.filter((r) => r.key !== key));

  const toggleDx = (key: number, dxId: string) =>
    setRows((prev) =>
      prev.map((r) =>
        r.key === key
          ? {
              ...r,
              dxIds: r.dxIds.includes(dxId)
                ? r.dxIds.filter((x) => x !== dxId)
                : [...r.dxIds, dxId],
            }
          : r,
      ),
    );

  const setCharge = (key: number, charge: string) =>
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, charge } : r)));

  // ── Save (replace-all procedures + optional Plan write-back) ──
  const showPlanEditor = ownsPlan && planFieldId != null;
  // Editor context already passes onMacroText only when the Plan is editable; chart context
  // gates on the signed guard so we never offer to append text that can't be saved.
  const showMacroToggle = onMacroText != null || (showPlanEditor && !reportSigned);

  const saveMutation = useMutation({
    mutationFn: async (vars: {
      reportId: string;
      procedures: ReportProcedureInput[];
      planUpdate: ReturnType<typeof buildReportUpdate> | null;
    }) => {
      await setReportProcedures({
        reportId: vars.reportId,
        procedures: vars.procedures,
        insuranceTypeId,
      });
      if (vars.planUpdate) await updateReport(vars.planUpdate);
    },
    onSuccess: (_data, vars) => {
      toast.success("Procedures saved.");
      void queryClient.invalidateQueries({ queryKey: ["report-procedures", vars.reportId] });
      if (vars.planUpdate) {
        void queryClient.invalidateQueries({ queryKey: ["report", vars.reportId] });
        void queryClient.invalidateQueries({ queryKey: ["reports"] });
      }
      setHydratedFor(null);
      onClose();
    },
    onError: (err) => toast.error("Failed to save procedures.", { description: describe(err) }),
  });

  // Rebuild the FULL report so the Plan write-back never drops the report's other fields/vitals.
  function buildReportUpdate() {
    if (!reportDetail || planFieldId == null) return null;
    const others = reportDetail.fieldValues.filter((f) => f.reportFieldId !== planFieldId);
    const merged: ReportFieldValue[] = [...others, { reportFieldId: planFieldId, text: planText }];
    return {
      reportId: reportDetail.id,
      reportDate: reportDetail.reportDate,
      providerId: reportDetail.providerId ?? null,
      clinicId: reportDetail.clinicId ?? null,
      isNoShow: reportDetail.isNoShow,
      vitals: reportDetail.vitals,
      fieldValues: merged,
    };
  }

  const onSave = () => {
    if (!activeReportId) return;
    const unlinked = rows.filter((r) => r.dxIds.length === 0);
    if (unlinked.length > 0) {
      toast.warning("Every procedure must have at least one DX code checked.");
      return;
    }
    const procedures: ReportProcedureInput[] = rows.map((r) => ({
      procedureCodeId: r.procedureCodeId,
      code: r.code,
      description: r.description,
      charge: Number(r.charge) >= 0 && Number.isFinite(Number(r.charge)) ? Number(r.charge) : 0,
      diagnosticIds: r.dxIds,
    }));
    // Persist the Plan only in chart context, when it changed and the report isn't signed.
    const planUpdate = showPlanEditor && planDirty && !reportSigned ? buildReportUpdate() : null;
    saveMutation.mutate({ reportId: activeReportId, procedures, planUpdate });
  };

  // ── Derived bill summary for the View Bill dialog ──
  const billData = useMemo<BillSummaryData>(() => {
    const demo = patientQuery.data?.demographics;
    const contact = patientQuery.data?.contact;
    const name = demo ? `${demo.lastName}, ${demo.firstName}`.trim().replace(/^,|,$/g, "") : "—";
    const addr: string[] = [];
    if (contact?.address1) addr.push(contact.address1);
    if (contact?.address2) addr.push(contact.address2);
    const cityLine = [contact?.city, contact?.state].filter(Boolean).join(", ");
    if (cityLine || contact?.zipCode) addr.push([cityLine, contact?.zipCode].filter(Boolean).join(" "));

    const subscriberOf = (policy: PatientInsurancePolicy | null): string | null =>
      !policy
        ? null
        : policy.subscriberRelationship === "Self"
          ? name
          : [policy.subscriberLastName, policy.subscriberFirstName].filter(Boolean).join(", ") || null;

    const insBlock = (policy: PatientInsurancePolicy | null, typeName?: string | null) =>
      policy
        ? {
            type: typeName ?? policy.insuranceTypeName,
            provider: policy.insuranceCompanyName,
            groupNumber: policy.groupNumber,
            policyNumber: policy.policyNumber,
            subscriber: subscriberOf(policy),
          }
        : null;

    return {
      patient: {
        name,
        code: patientQuery.data?.patientCode,
        dob: demo?.dateOfBirth,
        phone: contact?.phone,
        address: addr.length ? addr : undefined,
      },
      // Primary is priced under the picker's insurance type when the bill named one.
      primaryInsurance: insBlock(
        primaryPolicy,
        insuranceOptions?.find((o) => o.value === insuranceTypeId)?.label ?? primaryPolicy?.insuranceTypeName,
      ),
      secondaryInsurance: insBlock(secondaryPolicy),
      encounter: { reportDate: reportDetail?.reportDate },
      lines: rows.map((r) => ({
        code: r.code,
        description: r.description,
        charge: Number.isFinite(Number(r.charge)) ? Number(r.charge) : 0,
        dxCodes: r.dxIds.map((id) => dxLabel.get(id)?.code ?? id.slice(0, 8)),
      })),
    };
  }, [
    patientQuery.data,
    primaryPolicy,
    secondaryPolicy,
    insuranceOptions,
    insuranceTypeId,
    reportDetail,
    rows,
    dxLabel,
  ]);

  const showPicker = reportId === null && pickedReportId === null;
  const patientDisplayName =
    patientName ?? (billData.patient.name !== "—" ? billData.patient.name : undefined);

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
      >
        <DialogContent className="!max-w-5xl">
          <DialogHeader>
            <div className="flex items-center justify-between gap-3">
              <DialogTitle>Procedures Performed</DialogTitle>
              {!showPicker && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="mr-6"
                  onClick={() => setViewBillOpen(true)}
                >
                  <Receipt className="size-4" />
                  View Bill
                </Button>
              )}
            </div>
          </DialogHeader>

          <DialogBody className="space-y-4">
            {showPicker ? (
              /* ── Phase 1 (chart context): pick the report ── */
              reportsQuery.isLoading ? (
                <div className="skeleton h-20 rounded-lg" />
              ) : (reportsQuery.data?.items ?? []).length === 0 ? (
                <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                  No reports for this incident yet. Create a report first.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {(reportsQuery.data?.items ?? []).map((r) => (
                    <li key={r.id}>
                      <button
                        type="button"
                        onClick={() => setPickedReportId(r.id)}
                        className="flex w-full items-center justify-between gap-3 px-3 py-2.5 text-left hover:bg-[var(--color-accent)]"
                      >
                        <span className="text-[13px] font-medium">{formatDate(r.reportDate)}</span>
                        <EntityStatusBadge tone={r.isSigned ? "info" : "default"}>
                          {r.workflowStatus}
                        </EntityStatusBadge>
                      </button>
                    </li>
                  ))}
                </ul>
              )
            ) : (
              /* ── Phase 2: the procedures editor ── */
              <>
                {/* Mini-info cards: Patient · Incident · Report */}
                <div className="grid gap-3 md:grid-cols-3">
                  <MiniCard icon={<User className="size-3.5" />} title="Patient">
                    <MiniRow label="Name">{billData.patient.name}</MiniRow>
                    <MiniRow label="DOB">{formatDate(billData.patient.dob)}</MiniRow>
                    <MiniRow label="Code">{billData.patient.code}</MiniRow>
                    <MiniRow label="Insurance">{primaryPolicy?.insuranceCompanyName}</MiniRow>
                  </MiniCard>

                  <MiniCard icon={<Stethoscope className="size-3.5" />} title="Incident">
                    <MiniRow label="ACC">{incidentTypeName}</MiniRow>
                    <MiniRow label="DOIV">{formatDate(incidentQuery.data?.dateOfInitialVisit)}</MiniRow>
                    <MiniRow label="DOL">{formatDate(incidentQuery.data?.dateOfLoss)}</MiniRow>
                    <MiniDxRow
                      ids={dxIds}
                      label={(id) => dxLabel.get(id) ?? { code: `${id.slice(0, 8)}…` }}
                    />
                  </MiniCard>

                  <MiniCard
                    icon={<FileText className="size-3.5" />}
                    title="Report"
                    action={
                      reportDetail ? (
                        <EntityStatusBadge tone={reportSigned ? "info" : "default"}>
                          {reportDetail.workflowStatus}
                        </EntityStatusBadge>
                      ) : undefined
                    }
                  >
                    <MiniRow label="Date">{formatDate(reportDetail?.reportDate)}</MiniRow>
                    <MiniRow label="Type">{reportTypeName}</MiniRow>
                    <MiniRow label="Department">{departmentName}</MiniRow>
                    <MiniRow label="Provider">{providerName}</MiniRow>
                  </MiniCard>
                </div>

                {/* Current procedures */}
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <p className="text-[13px] font-semibold">Procedures Administered in this Report</p>
                  <div className="flex items-center gap-3">
                    {showMacroToggle && (
                      <label className="flex cursor-pointer items-center gap-2 text-[13px]">
                        <input
                          type="checkbox"
                          checked={addMacroText}
                          onChange={(e) => setAddMacroText(e.target.checked)}
                          className="rounded border-[var(--color-border)]"
                        />
                        <span>Add Macro Text to Plan</span>
                      </label>
                    )}
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => setDxDialogOpen(true)}
                      disabled={!canManage}
                    >
                      <Pencil className="size-4" />
                      Edit Dx Codes
                    </Button>
                  </div>
                </div>

                {proceduresQuery.isLoading ? (
                  <div className="skeleton h-24 rounded-lg" />
                ) : rows.length === 0 ? (
                  <p className="rounded-lg border border-[var(--color-border)] px-3 py-4 text-center text-[13px] text-[var(--color-muted-foreground)]">
                    No procedures yet — add one from the picker below.
                  </p>
                ) : (
                  <div className="max-h-56 overflow-auto rounded-lg border border-[var(--color-border)]">
                    <table className="w-full text-[13px]">
                      <thead className="sticky top-0 bg-[var(--color-card)]">
                        <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                          <th className="px-3 py-2 w-28">Code</th>
                          <th className="px-3 py-2">DX Codes</th>
                          <th className="px-3 py-2 w-28">Charge</th>
                          <th className="px-3 py-2 w-10" />
                        </tr>
                      </thead>
                      <tbody>
                        {rows.map((row) => (
                          <tr
                            key={row.key}
                            onDoubleClick={() => canManage && removeRow(row.key)}
                            title={row.description ?? undefined}
                            className="border-b border-[var(--color-border)] last:border-b-0 align-top"
                          >
                            <td className="px-3 py-2 font-medium">{row.code}</td>
                            <td className="px-3 py-2">
                              <div className="flex flex-wrap gap-x-4 gap-y-1">
                                {dxIds.map((dxId) => (
                                  <label
                                    key={dxId}
                                    title={dxLabel.get(dxId)?.description ?? undefined}
                                    className="flex items-center gap-1.5 cursor-pointer"
                                  >
                                    <input
                                      type="checkbox"
                                      checked={row.dxIds.includes(dxId)}
                                      onChange={() => toggleDx(row.key, dxId)}
                                      disabled={!canManage}
                                      className="rounded border-[var(--color-border)]"
                                    />
                                    <span>{dxLabel.get(dxId)?.code ?? `${dxId.slice(0, 8)}…`}</span>
                                  </label>
                                ))}
                              </div>
                            </td>
                            <td className="px-3 py-2">
                              <Input
                                inputMode="decimal"
                                aria-label={`Charge for ${row.code}`}
                                value={row.charge}
                                onChange={(e) => setCharge(row.key, e.target.value)}
                                disabled={!canManage}
                                className="h-8"
                              />
                            </td>
                            <td className="px-3 py-2">
                              {canManage && (
                                <button
                                  type="button"
                                  title="Remove procedure"
                                  aria-label={`Remove ${row.code}`}
                                  onClick={() => removeRow(row.key)}
                                  className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                                >
                                  <X className="size-3.5" />
                                </button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                {/* Plan field (chart context owns/persists it; editor context keeps its own) */}
                {showPlanEditor && (
                  <Field id="pp-plan" label="Plan">
                    <Textarea
                      id="pp-plan"
                      rows={3}
                      value={planText}
                      onChange={(e) => {
                        setPlanText(e.target.value);
                        setPlanDirty(true);
                      }}
                      readOnly={reportSigned || !canManage}
                      placeholder={reportSigned ? "" : "Plan / treatment notes…"}
                    />
                    {reportSigned && (
                      <p className="mt-1 text-[12px] italic text-[var(--color-muted-foreground)]">
                        This report has been signed. The plan cannot be edited.
                      </p>
                    )}
                  </Field>
                )}

                {/* Add Procedure */}
                <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-muted)]/20 p-3">
                  <div className="mb-2 flex items-center gap-2">
                    <ClipboardList className="size-4 text-[var(--color-primary)]" />
                    <p className="text-[13px] font-semibold">Add Procedure</p>
                    <span className="text-[12px] text-[var(--color-muted-foreground)]">
                      Pick a category, then click a code to add it. Double-click a bill row above to remove.
                    </span>
                  </div>

                  <div className="grid gap-3 sm:grid-cols-2">
                    <Field id="pp-insurance" label="Insurance">
                      <Combobox
                        id="pp-insurance"
                        label="Insurance"
                        value={insuranceTypeId}
                        onChange={setInsuranceTypeId}
                        options={insuranceOptions ?? []}
                        placeholder="Select insurance…"
                        searchable
                      />
                    </Field>
                    <Field id="pp-category" label="Procedure Category">
                      <Combobox
                        id="pp-category"
                        label="Procedure Category"
                        value={categoryId}
                        onChange={setCategoryId}
                        options={categoryOptions ?? []}
                        placeholder="All categories"
                        searchable
                        clearable
                      />
                    </Field>
                  </div>

                  {codesQuery.isLoading ? (
                    <div className="skeleton mt-3 h-32 rounded-lg" />
                  ) : (
                    <div className="mt-3 max-h-56 overflow-auto rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]">
                      <table className="w-full text-[13px]">
                        <thead className="sticky top-0 bg-[var(--color-card)]">
                          <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                            <th className="px-3 py-2 w-28">Code</th>
                            <th className="px-3 py-2">Description</th>
                            <th className="px-3 py-2 w-24">Price</th>
                            <th className="px-3 py-2 w-14" />
                          </tr>
                        </thead>
                        <tbody>
                          {(codesQuery.data?.items ?? []).map((c) => (
                            <tr
                              key={c.id}
                              onDoubleClick={() => canManage && addProcedure(c.id)}
                              className="border-b border-[var(--color-border)] last:border-b-0 hover:bg-[var(--color-accent)]"
                            >
                              <td className="px-3 py-2 font-medium">{c.code}</td>
                              <td className="px-3 py-2">{c.description ?? c.name ?? "—"}</td>
                              <td className="px-3 py-2">
                                {priceByCodeId.has(c.id)
                                  ? `$${priceByCodeId.get(c.id)!.toFixed(2)}`
                                  : "—"}
                              </td>
                              <td className="px-3 py-2">
                                {canManage && (
                                  <button
                                    type="button"
                                    title={`Add ${c.code}`}
                                    aria-label={`Add ${c.code}`}
                                    onClick={() => addProcedure(c.id)}
                                    className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-card)]"
                                  >
                                    <Plus className="size-3.5" />
                                  </button>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              </>
            )}
          </DialogBody>

          <DialogFooter>
            {!showPicker && canManage && (
              <Button type="button" onClick={onSave} disabled={saveMutation.isPending}>
                <Save className="size-4" />
                {saveMutation.isPending ? "Saving…" : "Save"}
              </Button>
            )}
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Close
              </Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Read-only bill review + Print/PDF */}
      <ViewBillDialog
        open={viewBillOpen}
        onClose={() => setViewBillOpen(false)}
        data={billData}
        title="View Bill"
      />

      {/* Edit the incident's dx set; the incident query invalidation refreshes the checkboxes. */}
      {dxDialogOpen && (
        <DiagnosticCodesDialog
          patientId={patientId}
          patientName={patientDisplayName}
          incidentId={incidentId}
          open={dxDialogOpen}
          onClose={() => {
            setDxDialogOpen(false);
            void queryClient.invalidateQueries({ queryKey: ["incident", incidentId] });
          }}
        />
      )}
    </>
  );
}
