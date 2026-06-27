import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  createProblem,
  updateProblem,
  type PatientProblem,
  type ProblemStatus,
} from "@/api/problems";
import { listDiagnostics } from "@/api/administration";
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
import { Combobox, Field, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";

const STATUS_OPTIONS: ComboboxOption[] = [
  { value: "Active", label: "Active" },
  { value: "Resolved", label: "Resolved" },
  { value: "Inactive", label: "Inactive" },
];

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** When set, the dialog is in edit mode and pre-fills from this problem. */
  problem?: PatientProblem | null;
  /** Optional incident context for new problems. */
  incidentId?: string | null;
};

export function ProblemDialog({ patientId, open, onClose, problem, incidentId }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!problem;

  const [diagnosticId, setDiagnosticId] = useState<number | null>(null);
  const [diagnosticCode, setDiagnosticCode] = useState("");
  const [diagnosticDescription, setDiagnosticDescription] = useState<string | null>(null);
  const [diagnosisDate, setDiagnosisDate] = useState("");
  const [status, setStatus] = useState<ProblemStatus>("Active");
  const [notes, setNotes] = useState("");
  const [isMedicalAlert, setIsMedicalAlert] = useState(false);
  const [dxSearch, setDxSearch] = useState("");

  useEffect(() => {
    if (!open) return;
    if (problem) {
      setDiagnosticId(problem.diagnosticId);
      setDiagnosticCode(problem.diagnosticCode);
      setDiagnosticDescription(problem.diagnosticDescription ?? null);
      setDiagnosisDate(problem.diagnosisDate ? problem.diagnosisDate.slice(0, 10) : "");
      setStatus(problem.status);
      setNotes(problem.notes ?? "");
      setIsMedicalAlert(problem.isMedicalAlert);
    } else {
      setDiagnosticId(null);
      setDiagnosticCode("");
      setDiagnosticDescription(null);
      setDiagnosisDate("");
      setStatus("Active");
      setNotes("");
      setIsMedicalAlert(false);
    }
    setDxSearch("");
  }, [open, problem]);

  const dxQuery = useQuery({
    queryKey: ["dx-icd-search", dxSearch],
    queryFn: () => listDiagnostics({ search: dxSearch, isActive: true, pageSize: 50 }),
    enabled: open && dxSearch.length >= 2,
  });

  const dxOptions = useMemo(
    () =>
      (dxQuery.data?.items ?? []).map((d) => ({
        id: d.id,
        code: d.code,
        description: d.description ?? null,
        label: `${d.code}${d.description ? " — " + d.description : ""}`,
      })),
    [dxQuery.data],
  );

  const pickDx = (opt: { id: number; code: string; description: string | null }) => {
    setDiagnosticId(opt.id);
    setDiagnosticCode(opt.code);
    setDiagnosticDescription(opt.description);
    setDxSearch("");
  };

  const createMutation = useMutation({
    mutationFn: createProblem,
    onSuccess: () => {
      toast.success("Problem added.");
      void queryClient.invalidateQueries({ queryKey: ["problems", patientId] });
      onClose();
    },
    onError: (err) => toast.error("Failed to add problem.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateProblem,
    onSuccess: () => {
      toast.success("Problem updated.");
      void queryClient.invalidateQueries({ queryKey: ["problems", patientId] });
      onClose();
    },
    onError: (err) => toast.error("Failed to update problem.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!diagnosticId || !diagnosticCode) return;
    if (isEdit && problem) {
      updateMutation.mutate({
        problemId: problem.id,
        diagnosticId,
        diagnosticCode,
        diagnosticDescription,
        diagnosisDate: diagnosisDate || null,
        status,
        notes: notes.trim() || null,
        isMedicalAlert,
      });
    } else {
      createMutation.mutate({
        patientId,
        diagnosticId,
        diagnosticCode,
        diagnosticDescription,
        diagnosisDate: diagnosisDate || null,
        status,
        notes: notes.trim() || null,
        isMedicalAlert,
        incidentId: incidentId ?? null,
      });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Problem" : "Add Problem"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            {/* DX code picker */}
            <div className="space-y-2">
              <label className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                Diagnosis (DX Code)
              </label>
              {diagnosticCode ? (
                <div className="flex items-start justify-between gap-2 rounded-lg border border-[var(--color-border)] p-2.5">
                  <div className="min-w-0">
                    <p className="text-[13px] font-medium">{diagnosticCode}</p>
                    {diagnosticDescription && (
                      <p className="text-[12px] text-[var(--color-muted-foreground)]">
                        {diagnosticDescription}
                      </p>
                    )}
                  </div>
                  <Button type="button" variant="ghost" size="xs" onClick={() => setDiagnosticCode("")}>
                    Change
                  </Button>
                </div>
              ) : (
                <>
                  <Input
                    type="text"
                    value={dxSearch}
                    onChange={(e) => setDxSearch(e.target.value)}
                    placeholder="Search by code or description (min 2 chars)…"
                    autoFocus
                  />
                  {dxSearch.length >= 2 && dxOptions.length > 0 && (
                    <ul className="max-h-40 overflow-y-auto rounded-md border border-[var(--color-border)] bg-[var(--color-card)]">
                      {dxOptions.map((opt) => (
                        <li key={opt.id}>
                          <button
                            type="button"
                            onClick={() => pickDx(opt)}
                            className="w-full px-3 py-1.5 text-left text-[12px] hover:bg-[var(--color-accent)]"
                          >
                            {opt.label}
                          </button>
                        </li>
                      ))}
                    </ul>
                  )}
                </>
              )}
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="prob-date" label="Diagnosis Date">
                <Input
                  id="prob-date"
                  type="date"
                  value={diagnosisDate}
                  onChange={(e) => setDiagnosisDate(e.target.value)}
                />
              </Field>
              <Field id="prob-status" label="Status">
                <Combobox
                  id="prob-status"
                  label="Status"
                  value={status}
                  onChange={(v) => setStatus((v ?? "Active") as ProblemStatus)}
                  options={STATUS_OPTIONS}
                  placeholder="Select…"
                />
              </Field>
            </div>

            <Field id="prob-notes" label="Notes">
              <Textarea
                id="prob-notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                rows={3}
                maxLength={4000}
                placeholder="Notes…"
              />
            </Field>

            <label className="flex w-fit items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={isMedicalAlert}
                onChange={(e) => setIsMedicalAlert(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Flag as medical alert</span>
            </label>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !diagnosticId || !diagnosticCode}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Add Problem"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
