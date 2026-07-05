import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  getMedicationReconciledDates,
  markMedicationsReconciled,
  type PatientMedication,
} from "@/api/medications";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  medications: PatientMedication[];
  canReconcile: boolean;
};

/** Legacy Reconciliation dialog, minus CCD import (a stub even in BackChart):
 *  current meds on the left, "Mark Reconciled Today" + Dates Reconciled history on the right. */
export function MedicationReconciliationDialog({
  patientId,
  open,
  onClose,
  medications,
  canReconcile,
}: Props) {
  const queryClient = useQueryClient();

  const datesQuery = useQuery({
    queryKey: ["medication-reconciled-dates", patientId],
    queryFn: () => getMedicationReconciledDates(patientId),
    enabled: open,
  });

  const markMutation = useMutation({
    mutationFn: () => markMedicationsReconciled(patientId),
    onSuccess: () => {
      toast.success("Medications marked reconciled.");
      void queryClient.invalidateQueries({ queryKey: ["medication-reconciled-dates", patientId] });
    },
    onError: (err) => toast.error("Failed to mark reconciled.", { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-3xl">
        <DialogHeader>
          <DialogTitle>Medication Reconciliation</DialogTitle>
        </DialogHeader>

        <DialogBody>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <h3 className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                Current Medications
              </h3>
              {medications.length === 0 ? (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">No medications.</p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {medications.map((m) => (
                    <li key={m.id} className="flex items-center justify-between gap-2 px-3 py-2">
                      <div className="min-w-0">
                        <p className="truncate text-[13px]">{m.drugName}</p>
                        <p className="text-[12px] text-[var(--color-muted-foreground)]">
                          {formatDate(m.startDate)}
                          {m.rxCode ? ` · Rx ${m.rxCode}` : ""}
                        </p>
                      </div>
                      <EntityStatusBadge tone={m.isActive ? "success" : "warning"}>
                        {m.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <h3 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                  Dates Reconciled
                </h3>
                {canReconcile && (
                  <Button
                    size="sm"
                    className="h-8 rounded-lg px-3 text-[13px] font-semibold"
                    disabled={markMutation.isPending}
                    onClick={() => markMutation.mutate()}
                  >
                    Mark Reconciled Today
                  </Button>
                )}
              </div>
              {datesQuery.isLoading ? (
                <div className="skeleton h-16 rounded-lg" />
              ) : (datesQuery.data ?? []).length === 0 ? (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  No reconciliation recorded yet.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {(datesQuery.data ?? []).map((d) => (
                    <li key={d.id} className="px-3 py-2 text-[13px]">
                      {formatDate(d.reconciledOn)}
                      {d.createdByName && (
                        <span className="text-[12px] text-[var(--color-muted-foreground)]">
                          {" "}— {d.createdByName}
                        </span>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
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
  );
}
