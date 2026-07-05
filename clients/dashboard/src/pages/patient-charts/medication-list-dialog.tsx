import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
import { toast } from "sonner";
import {
  searchPatientMedications,
  setNoKnownMedications,
  type PatientMedication,
} from "@/api/medications";
import { getPatientById } from "@/api/patients";
import { MEDICATION_PERMISSIONS, PATIENT_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
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
import { MedicationDialog } from "@/pages/patient-charts/medication-dialog";
import { MedicationReconciliationDialog } from "@/pages/patient-charts/medication-reconciliation-dialog";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
};

export function MedicationListDialog({ patientId, open, onClose }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(MEDICATION_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(MEDICATION_PERMISSIONS.update) ?? false;
  const canToggleActive = user?.permissions?.includes(MEDICATION_PERMISSIONS.delete) ?? false;
  // SetNoKnownMedications is a patient-record mutation and requires Patients.Update, not any Medications.* permission.
  const canUpdatePatient = user?.permissions?.includes(PATIENT_PERMISSIONS.update) ?? false;

  const [showInactive, setShowInactive] = useState(false);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editMedication, setEditMedication] = useState<PatientMedication | null>(null);
  const [reconciliationOpen, setReconciliationOpen] = useState(false);

  const medicationsQuery = useQuery({
    queryKey: ["patient-medications", patientId, showInactive],
    queryFn: () =>
      searchPatientMedications({ patientId, includeInactive: showInactive, pageSize: 200 }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: open,
  });

  const medications = useMemo(() => medicationsQuery.data?.items ?? [], [medicationsQuery.data]);
  const noKnownMedications = patientQuery.data?.hasNoKnownMedications ?? false;

  const noMedicationsMutation = useMutation({
    mutationFn: (value: boolean) => setNoKnownMedications(patientId, value),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["patients", patientId] });
    },
    onError: (err) => {
      // Legacy rule surfaces here as a 409 with the exact message.
      toast.warning("Could not change No Medications.", { description: describe(err) });
      void queryClient.invalidateQueries({ queryKey: ["patients", patientId] });
    },
  });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Medication List</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                <input
                  type="checkbox"
                  checked={showInactive}
                  onChange={(e) => setShowInactive(e.target.checked)}
                  className="rounded border-[var(--color-border)]"
                />
                <span>Show inactive</span>
              </label>
              <div className="flex items-center gap-3">
                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={noKnownMedications}
                    disabled={!canUpdatePatient || noMedicationsMutation.isPending}
                    onChange={(e) => noMedicationsMutation.mutate(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Set No Medications</span>
                </label>
                <Button
                  size="sm"
                  variant="outline"
                  className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                  onClick={() => setReconciliationOpen(true)}
                >
                  Reconciliation
                </Button>
                {canCreate && (
                  <Button
                    size="sm"
                    className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                    onClick={() => {
                      setEditMedication(null);
                      setEditorOpen(true);
                    }}
                  >
                    <Plus className="size-4" />
                    Add Medication
                  </Button>
                )}
              </div>
            </div>

            {medicationsQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : medications.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                {noKnownMedications
                  ? "Patient marked as having no known medications."
                  : "No medications recorded for this patient."}
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {medications.map((m) => (
                  <li key={m.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium">{m.drugName}</p>
                      <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                        {[
                          m.doseValue != null ? `${m.doseValue}${m.dosePeriodUnit ? "/" + m.dosePeriodUnit : ""}` : null,
                          m.prescriber,
                        ]
                          .filter(Boolean)
                          .join(" · ")}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="hidden text-[12px] text-[var(--color-muted-foreground)] sm:inline">
                        {formatDate(m.startDate)}
                        {m.endDate ? ` – ${formatDate(m.endDate)}` : ""}
                      </span>
                      <EntityStatusBadge tone={m.isActive ? "success" : "warning"}>
                        {m.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                      {canUpdate && (
                        <button
                          type="button"
                          title="Edit medication"
                          aria-label="Edit medication"
                          onClick={() => {
                            setEditMedication(m);
                            setEditorOpen(true);
                          }}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                        >
                          <Pencil className="size-4" />
                        </button>
                      )}
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

      <MedicationDialog
        patientId={patientId}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditMedication(null);
        }}
        medication={editMedication}
        canToggleActive={canToggleActive}
      />

      <MedicationReconciliationDialog
        patientId={patientId}
        open={reconciliationOpen}
        onClose={() => setReconciliationOpen(false)}
        medications={medications}
        canReconcile={canUpdate}
      />
    </>
  );
}
