import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Save, Search, X } from "lucide-react";
import { toast } from "sonner";
import { getPatientIncident, setIncidentDiagnostics } from "@/api/incidents";
import {
  ensureCustomDiagnostic,
  listCustomDiagnostics,
  listDiagnosticCategories,
  listDiagnostics,
  type DiagnosticDto,
} from "@/api/administration";
import { createProblem } from "@/api/problems";
import { INCIDENT_PERMISSIONS } from "@/lib/patient-permissions";
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
import { describe } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  patientName?: string;
  incidentId: string;
  open: boolean;
  onClose(): void;
  onSaved?(): void;
};

/** One row in the incident's dx list. `customId` set = an already-persisted custom
 * diagnostic (existing incident dx); `globalId` set = added from the global ICD
 * catalog this session and not yet ensured into a custom diagnostic. */
type Row = {
  key: number;
  customId?: string;
  globalId?: number;
  code: string;
  description: string | null;
  isChiropractic?: boolean;
  addToProblems?: boolean;
};

type Mode = "category" | "search";

export function DiagnosticCodesDialog({
  patientId,
  patientName,
  incidentId,
  open,
  onClose,
  onSaved,
}: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canEdit = user?.permissions?.includes(INCIDENT_PERMISSIONS.update) ?? false;

  const [rows, setRows] = useState<Row[]>([]);
  const [hydrated, setHydrated] = useState(false);
  const [mode, setMode] = useState<Mode>("category");
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [searchInput, setSearchInput] = useState("");
  const [committedSearch, setCommittedSearch] = useState("");
  const [hideNonChiro, setHideNonChiro] = useState(true);
  const [pendingConfirmKey, setPendingConfirmKey] = useState<number | null>(null);
  const rowKey = useRef(0);

  useEffect(() => {
    if (!open) {
      setRows([]);
      setHydrated(false);
      setMode("category");
      setCategoryId(null);
      setSearchInput("");
      setCommittedSearch("");
      setHideNonChiro(true);
      setPendingConfirmKey(null);
    }
  }, [open]);

  // ── Load the incident's current dx set, resolve to custom-diagnostic rows ──
  const incidentQuery = useQuery({
    queryKey: ["incident", incidentId],
    queryFn: () => getPatientIncident(incidentId),
    enabled: open,
  });
  const existingIds = incidentQuery.data?.diagnosticIds ?? [];

  const customDxQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", incidentId, [...existingIds].sort().join(",")],
    queryFn: () => listCustomDiagnostics({ ids: existingIds, pageSize: 200 }),
    enabled: open && !hydrated && existingIds.length > 0,
  });

  useEffect(() => {
    if (!open || hydrated) return;
    if (!incidentQuery.data) return;
    if (existingIds.length === 0) {
      setRows([]);
      setHydrated(true);
      return;
    }
    if (!customDxQuery.data) return;
    const byId = new Map(customDxQuery.data.items.map((d) => [d.id, d]));
    setRows(
      existingIds.flatMap((id) => {
        const d = byId.get(id);
        if (!d) return [];
        return [
          {
            key: rowKey.current++,
            customId: d.id,
            code: d.code,
            description: d.description ?? null,
            isChiropractic: d.isChiropractic,
          },
        ];
      }),
    );
    setHydrated(true);
    // existingIds is derived from incidentQuery.data (already a dep) via a new array
    // literal each render; adding it here would make the effect re-fire every render
    // this component re-renders while open, purely from reference churn — the
    // `hydrated` guard makes that a no-op, but it's unnecessary work, not a bug.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, hydrated, incidentQuery.data, customDxQuery.data]);

  // ── Left panel: DX Categories (default) or DX Search ──
  const categoriesQuery = useQuery({
    queryKey: ["diagnostic-categories", "dx-dialog"],
    queryFn: () => listDiagnosticCategories({ isActive: true, pageSize: 200 }),
    enabled: open,
  });
  const categoryOptions = useMemo(
    () => (categoriesQuery.data?.items ?? []).map((c) => ({ value: c.id, label: c.name })),
    [categoriesQuery.data],
  );

  const categoryResultsQuery = useQuery({
    queryKey: ["diagnostics", "by-category", categoryId],
    queryFn: () => listDiagnostics({ categoryId, isActive: true, pageSize: 200 }),
    enabled: open && mode === "category" && !!categoryId,
  });

  const searchResultsQuery = useQuery({
    queryKey: ["diagnostics", "search", committedSearch, hideNonChiro],
    queryFn: () =>
      listDiagnostics({
        search: committedSearch,
        isChiropractic: hideNonChiro ? true : undefined,
        isActive: true,
        pageSize: 200,
      }),
    enabled: open && mode === "search" && committedSearch.length >= 3,
  });

  const resultsQuery = mode === "category" ? categoryResultsQuery : searchResultsQuery;
  const results = resultsQuery.data?.items ?? [];

  const runSearch = () => {
    const trimmed = searchInput.trim();
    if (trimmed.length < 3) {
      toast.warning("Please enter at least 3 characters.");
      return;
    }
    setCommittedSearch(trimmed);
  };

  // ── Add / remove rows (nothing persists until Save) ──
  const addRow = (d: DiagnosticDto) => {
    if (!canEdit) return;
    // A fast double-click (or click+dblclick) can fire this twice before the confirm
    // modal has painted; without this guard the second call would queue a second
    // add/confirm cycle. Bail while a confirm is already pending.
    if (pendingConfirmKey !== null) return;
    const dup = rows.some((r) => r.code.toLowerCase() === d.code.toLowerCase());
    if (dup) {
      toast.warning("Item already in the list.");
      return;
    }
    const key = rowKey.current++;
    setRows((prev) => [
      ...prev,
      {
        key,
        globalId: d.id,
        code: d.code,
        description: d.description ?? null,
        isChiropractic: d.isChiropractic,
      },
    ]);
    setPendingConfirmKey(key);
  };

  const removeRow = (key: number) => {
    if (!canEdit) return;
    setRows((prev) => prev.filter((r) => r.key !== key));
  };

  const confirmAddToProblems = (yes: boolean) => {
    if (yes && pendingConfirmKey !== null) {
      setRows((prev) =>
        prev.map((r) => (r.key === pendingConfirmKey ? { ...r, addToProblems: true } : r)),
      );
    }
    setPendingConfirmKey(null);
  };

  // ── Save: ensure new global rows → guids, replace the incident's dx set,
  // then create any confirmed problems. Single mutation; rows travel via mutate(arg).
  // Progress (resolved customId, consumed addToProblems) is patched back into row
  // state as each step completes so a retried Save — the user clicking Save again
  // after a failure — only redoes the work that didn't finish, instead of re-running
  // ensure/createProblem for rows already committed on the previous attempt. ──
  const saveMutation = useMutation({
    mutationFn: async (toSave: Row[]) => {
      const diagnosticIds: string[] = [];
      const problemRows: Row[] = [];
      for (const row of toSave) {
        let customId = row.customId;
        if (!customId) {
          customId = await ensureCustomDiagnostic({
            code: row.code,
            description: row.description,
            longDescription: null,
            isChiropractic: row.isChiropractic ?? false,
          });
          const resolvedId = customId;
          setRows((prev) =>
            prev.map((r) => (r.key === row.key ? { ...r, customId: resolvedId } : r)),
          );
        }
        diagnosticIds.push(customId);
        if (row.addToProblems) problemRows.push(row);
      }
      await setIncidentDiagnostics({ incidentId, diagnosticIds });
      for (const row of problemRows) {
        if (row.globalId == null) continue;
        await createProblem({
          patientId,
          diagnosticId: row.globalId,
          diagnosticCode: row.code,
          diagnosticDescription: row.description,
          status: "Active",
          isMedicalAlert: false,
          incidentId,
        });
        setRows((prev) =>
          prev.map((r) => (r.key === row.key ? { ...r, addToProblems: false } : r)),
        );
      }
    },
    onSuccess: () => {
      toast.success("Diagnostic codes saved.");
      void queryClient.invalidateQueries({ queryKey: ["incident", incidentId] });
      void queryClient.invalidateQueries({ queryKey: ["problems", patientId] });
      onSaved?.();
      onClose();
    },
    onError: (err) =>
      toast.error("Failed to save diagnostic codes.", { description: describe(err) }),
  });

  const onSave = () => saveMutation.mutate(rows);

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
            <DialogTitle>Diagnostic Codes</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            {!hydrated ? (
              <div className="skeleton h-24 rounded-lg" />
            ) : (
              <>
                <div className="grid gap-4 sm:grid-cols-[220px_1fr]">
                  <div className="space-y-3">
                    {mode === "category" ? (
                      <Field id="dx-category" label="DX Categories">
                        <Combobox
                          id="dx-category"
                          label="DX Categories"
                          value={categoryId}
                          onChange={setCategoryId}
                          options={categoryOptions}
                          placeholder="Select category…"
                          searchable
                          clearable
                        />
                      </Field>
                    ) : (
                      <div className="space-y-2">
                        <Field id="dx-search" label="Search">
                          <Input
                            id="dx-search"
                            value={searchInput}
                            onChange={(e) => setSearchInput(e.target.value)}
                            onKeyDown={(e) => {
                              if (e.key === "Enter") {
                                e.preventDefault();
                                runSearch();
                              }
                            }}
                            placeholder="Code or description…"
                          />
                        </Field>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          className="w-full"
                          onClick={runSearch}
                        >
                          <Search className="size-4" />
                          Search
                        </Button>
                        <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                          <input
                            type="checkbox"
                            checked={hideNonChiro}
                            onChange={(e) => setHideNonChiro(e.target.checked)}
                            className="rounded border-[var(--color-border)]"
                          />
                          <span>Hide non-chiropractic codes</span>
                        </label>
                      </div>
                    )}
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="w-full"
                      onClick={() => setMode((m) => (m === "category" ? "search" : "category"))}
                    >
                      DX Search
                    </Button>
                  </div>

                  <div className="space-y-1.5">
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      Double click a DX to add it to this incident
                    </p>
                    {resultsQuery.isLoading ? (
                      <div className="skeleton h-40 rounded-lg" />
                    ) : (
                      <div className="max-h-48 overflow-auto rounded-lg border border-[var(--color-border)]">
                        <table className="w-full text-[13px]">
                          <thead className="sticky top-0 bg-[var(--color-card)]">
                            <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                              <th className="px-3 py-2 w-28">Code</th>
                              <th className="px-3 py-2">Description</th>
                              <th className="px-3 py-2 w-10" />
                            </tr>
                          </thead>
                          <tbody>
                            {results.map((d) => (
                              <tr
                                key={d.id}
                                onDoubleClick={() => addRow(d)}
                                className="border-b border-[var(--color-border)] last:border-b-0 hover:bg-[var(--color-accent)]"
                              >
                                <td className="px-3 py-2 font-medium">{d.code}</td>
                                <td className="px-3 py-2">{d.description ?? "—"}</td>
                                <td className="px-3 py-2">
                                  {canEdit && (
                                    <button
                                      type="button"
                                      title={`Add ${d.code}`}
                                      aria-label={`Add ${d.code}`}
                                      onClick={() => addRow(d)}
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
                </div>

                <div className="space-y-1.5">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-[13px] font-semibold">DX Codes</p>
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      Double click a DX to remove from this incident
                    </p>
                  </div>
                  {rows.length === 0 ? (
                    <p className="rounded-lg border border-[var(--color-border)] px-3 py-4 text-center text-[13px] text-[var(--color-muted-foreground)]">
                      No diagnostic codes yet — pick from the list above.
                    </p>
                  ) : (
                    <div className="max-h-48 overflow-auto rounded-lg border border-[var(--color-border)]">
                      <table className="w-full text-[13px]">
                        <thead className="sticky top-0 bg-[var(--color-card)]">
                          <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                            <th className="px-3 py-2 w-28">Code</th>
                            <th className="px-3 py-2">Description</th>
                            <th className="px-3 py-2 w-10" />
                          </tr>
                        </thead>
                        <tbody>
                          {rows.map((row) => (
                            <tr
                              key={row.key}
                              onDoubleClick={() => removeRow(row.key)}
                              className="border-b border-[var(--color-border)] last:border-b-0"
                            >
                              <td className="px-3 py-2 font-medium">{row.code}</td>
                              <td className="px-3 py-2">{row.description ?? "—"}</td>
                              <td className="px-3 py-2">
                                {canEdit && (
                                  <button
                                    type="button"
                                    title="Remove diagnostic"
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
                </div>
              </>
            )}
          </DialogBody>

          <DialogFooter>
            {canEdit && (
              <Button type="button" onClick={onSave} disabled={saveMutation.isPending || !hydrated}>
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

      {/* Mini confirm — offer to add the just-added code to the patient's problem list.
          Dismissal must be an explicit Yes/No click: a stray trailing click from a fast
          double-click on the results row can land on the overlay right as this mounts,
          and Radix would otherwise treat that (or Escape) as an implicit "No" the user
          never saw. */}
      {pendingConfirmKey !== null && (
        <Dialog open onOpenChange={(o) => !o && confirmAddToProblems(false)}>
          <DialogContent
            className="!max-w-sm"
            onPointerDownOutside={(e) => e.preventDefault()}
            onInteractOutside={(e) => e.preventDefault()}
            onEscapeKeyDown={(e) => e.preventDefault()}
          >
            <DialogHeader>
              <DialogTitle>Add Dx Code to Problems</DialogTitle>
            </DialogHeader>
            <DialogBody>
              <p className="text-[13px]">
                Would you like to add this DX Code to the Problem List for{" "}
                {patientName ?? "this patient"}?
              </p>
            </DialogBody>
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => confirmAddToProblems(false)}>
                No
              </Button>
              <Button type="button" onClick={() => confirmAddToProblems(true)}>
                Yes
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      )}
    </>
  );
}
