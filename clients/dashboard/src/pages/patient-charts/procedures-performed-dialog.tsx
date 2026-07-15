import { useEffect, useMemo, useRef, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus, Save, X } from "lucide-react";
import { toast } from "sonner";
import { getPatientIncident } from "@/api/incidents";
import { searchPatientReports } from "@/api/reports";
import { pickPrimaryInsuranceType, searchPatientInsurancePolicies } from "@/api/patient-insurance";
import {
  listCustomDiagnostics,
  listInsuranceTypeProcedures,
  listProcedureCodes,
  useInsuranceTypeOptions,
  useProcedureCategoryOptions,
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
import { DiagnosticCodesDialog } from "@/pages/patient-charts/diagnostic-codes-dialog";

type Props = {
  patientId: string;
  /** Shown in the nested Diagnostic Codes dialog's add-to-problems confirm. */
  patientName?: string;
  incidentId: string;
  /** When null (chart-shortcut context) the dialog shows a report-picker phase first. */
  reportId: string | null;
  open: boolean;
  onClose(): void;
  /** Report-editor context only: receives a picked procedure's macro text for the Plan field. */
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

  const [rows, setRows] = useState<ProcRow[]>([]);
  const [hydratedFor, setHydratedFor] = useState<string | null>(null);
  const [addMacroText, setAddMacroText] = useState(true);
  const [insuranceTypeId, setInsuranceTypeId] = useState<string | null>(null);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [dxDialogOpen, setDxDialogOpen] = useState(false);
  const rowKey = useRef(0);

  useEffect(() => {
    if (!open) {
      setPickedReportId(reportId);
      setRows([]);
      setHydratedFor(null);
      setDxDialogOpen(false);
    }
  }, [open, reportId]);

  // ── Report picker phase (chart-shortcut context) ──
  const reportsQuery = useQuery({
    queryKey: ["reports", incidentId],
    queryFn: () => searchPatientReports({ incidentId, pageSize: 100 }),
    enabled: open && reportId === null && pickedReportId === null,
    placeholderData: keepPreviousData,
  });

  // ── Incident dx codes (the checkbox axis) ──
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
    setHydratedFor(activeReportId);
  }, [open, activeReportId, proceduresQuery.data, hydratedFor]);

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

  // Default the insurance type to the patient's primary active policy's type when it resolves
  // to a known (active) option; otherwise the first active admin insurance type. Waits for the
  // policies query so the patient's primary wins over the fallback. User can still override.
  useEffect(() => {
    if (!open || insuranceTypeId !== null) return;
    if (!insuranceOptions || insuranceOptions.length === 0) return;
    if (policiesQuery.isPending) return;
    const primary = pickPrimaryInsuranceType(policiesQuery.data?.items ?? []);
    const primaryIsKnown = !!primary && insuranceOptions.some((o) => o.value === primary.id);
    setInsuranceTypeId(primaryIsKnown ? primary!.id : insuranceOptions[0].value);
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

  // ── Row operations ──
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
    if (addMacroText && onMacroText && picked.macroText) {
      onMacroText(picked.macroText);
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

  // ── Save (replace-all) ──
  const saveMutation = useMutation({
    mutationFn: setReportProcedures,
    onSuccess: (_data, variables) => {
      toast.success("Procedures saved.");
      void queryClient.invalidateQueries({ queryKey: ["report-procedures", variables.reportId] });
      setHydratedFor(null);
      onClose();
    },
    onError: (err) => toast.error("Failed to save procedures.", { description: describe(err) }),
  });

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
    saveMutation.mutate({ reportId: activeReportId, procedures });
  };

  const showPicker = reportId === null && pickedReportId === null;

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={(o) => {
          if (!o) onClose();
        }}
      >
        <DialogContent className="!max-w-4xl">
          <DialogHeader>
            <DialogTitle>Procedures Performed</DialogTitle>
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
                <div className="flex flex-wrap items-center justify-between gap-3">
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
                  <p className="text-[13px] font-semibold">Procedures Administered in this Report</p>
                  {onMacroText ? (
                    <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                      <input
                        type="checkbox"
                        checked={addMacroText}
                        onChange={(e) => setAddMacroText(e.target.checked)}
                        className="rounded border-[var(--color-border)]"
                      />
                      <span>Add Macro Text to Plan Comments</span>
                    </label>
                  ) : (
                    <span />
                  )}
                </div>

                {proceduresQuery.isLoading ? (
                  <div className="skeleton h-24 rounded-lg" />
                ) : rows.length === 0 ? (
                  <p className="rounded-lg border border-[var(--color-border)] px-3 py-4 text-center text-[13px] text-[var(--color-muted-foreground)]">
                    No procedures yet — pick from the list below.
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

                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  Select a category and click a procedure to add it to this report. Double-click a
                  row above to remove it.
                </p>

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
                  <div className="skeleton h-32 rounded-lg" />
                ) : (
                  <div className="max-h-56 overflow-auto rounded-lg border border-[var(--color-border)]">
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

      {/* Edit the incident's dx set; the incident query invalidation refreshes the checkboxes. */}
      {dxDialogOpen && (
        <DiagnosticCodesDialog
          patientId={patientId}
          patientName={patientName}
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
