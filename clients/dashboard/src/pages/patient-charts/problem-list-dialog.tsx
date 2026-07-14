import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { deleteProblem, searchPatientProblems, type PatientProblem } from "@/api/problems";
import { PROBLEM_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { EntityStatusBadge, type EntityStatusTone } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { ProblemDialog } from "@/pages/patient-charts/problem-dialog";

function statusTone(status: string): EntityStatusTone {
  if (status === "Active") return "success";
  if (status === "Resolved") return "default";
  return "warning";
}

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** Optional incident context for newly created problems. */
  incidentId?: string | null;
};

export function ProblemListDialog({ patientId, open, onClose, incidentId }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(PROBLEM_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(PROBLEM_PERMISSIONS.update) ?? false;
  const canDelete = user?.permissions?.includes(PROBLEM_PERMISSIONS.delete) ?? false;

  const [showResolved, setShowResolved] = useState(false);
  const [showInactive, setShowInactive] = useState(false);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editProblem, setEditProblem] = useState<PatientProblem | null>(null);
  const [pendingDelete, setPendingDelete] = useState<PatientProblem | null>(null);

  const problemsQuery = useQuery({
    queryKey: ["problems", patientId, showResolved, showInactive],
    queryFn: () =>
      searchPatientProblems({
        patientId,
        includeResolved: showResolved,
        includeInactive: showInactive,
        pageSize: 200,
      }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const problems = useMemo(() => problemsQuery.data?.items ?? [], [problemsQuery.data]);

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteProblem(id),
    onSuccess: () => {
      toast.success("Problem deleted.");
      void queryClient.invalidateQueries({ queryKey: ["problems", patientId] });
      setPendingDelete(null);
    },
    onError: (err) => toast.error("Failed to delete problem.", { description: describe(err) }),
  });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Problem List</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex flex-wrap items-center gap-3">
                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={showResolved}
                    onChange={(e) => setShowResolved(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Show resolved</span>
                </label>
                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={showInactive}
                    onChange={(e) => setShowInactive(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Show inactive</span>
                </label>
              </div>
              {canCreate && (
                <Button
                  size="sm"
                  className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                  onClick={() => {
                    setEditProblem(null);
                    setEditorOpen(true);
                  }}
                >
                  <Plus className="size-4" />
                  Add Problem
                </Button>
              )}
            </div>

            {problemsQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : problems.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                No problems recorded for this patient.
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {problems.map((p) => (
                  <li key={p.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="flex items-center gap-1.5 truncate text-[13px] font-medium">
                        {p.diagnosticCode}
                        {p.isMedicalAlert && (
                          <AlertTriangle className="size-3.5 text-[var(--color-destructive)]" />
                        )}
                      </p>
                      {p.diagnosticDescription && (
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {p.diagnosticDescription}
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      {p.diagnosisDate && (
                        <span className="hidden text-[12px] text-[var(--color-muted-foreground)] sm:inline">
                          {formatDate(p.diagnosisDate)}
                        </span>
                      )}
                      <EntityStatusBadge tone={statusTone(p.status)}>{p.status}</EntityStatusBadge>
                      <div className="flex items-center gap-1">
                        {canUpdate && (
                          <button
                            type="button"
                            title="Edit problem"
                            aria-label="Edit problem"
                            onClick={() => {
                              setEditProblem(p);
                              setEditorOpen(true);
                            }}
                            className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                          >
                            <Pencil className="size-4" />
                          </button>
                        )}
                        {canDelete && (
                          <button
                            type="button"
                            title="Delete problem"
                            aria-label="Delete problem"
                            disabled={deleteMutation.isPending}
                            onClick={() => setPendingDelete(p)}
                            className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-destructive)] transition-colors hover:bg-[var(--color-accent)] disabled:pointer-events-none disabled:opacity-40"
                          >
                            <Trash2 className="size-4" />
                          </button>
                        )}
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Close
              </Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Add/edit problem — stacked over the list dialog */}
      <ProblemDialog
        patientId={patientId}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditProblem(null);
        }}
        problem={editProblem}
        incidentId={incidentId}
      />

      <ConfirmDialog
        open={pendingDelete !== null}
        eyebrow="Delete problem"
        title="Delete this problem?"
        description={
          <>
            <span className="font-medium text-[var(--color-foreground)]">
              {pendingDelete?.diagnosticCode}
              {pendingDelete?.diagnosticDescription ? ` — ${pendingDelete.diagnosticDescription}` : ""}
            </span>{" "}
            will be removed from this patient&apos;s problem list. This can&apos;t be undone.
          </>
        }
        confirmLabel="Delete problem"
        pending={deleteMutation.isPending}
        onCancel={() => setPendingDelete(null)}
        onConfirm={() => pendingDelete && deleteMutation.mutate(pendingDelete.id)}
      />
    </>
  );
}
