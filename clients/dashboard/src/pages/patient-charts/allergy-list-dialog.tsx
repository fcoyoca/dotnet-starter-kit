import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
import { toast } from "sonner";
import { searchPatientAllergies, setNoKnownAllergies, type PatientAllergy } from "@/api/allergies";
import { getPatientById } from "@/api/patients";
import { ALLERGY_PERMISSIONS, PATIENT_PERMISSIONS } from "@/lib/patient-permissions";
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
import { AllergyDialog } from "@/pages/patient-charts/allergy-dialog";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
};

export function AllergyListDialog({ patientId, open, onClose }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(ALLERGY_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(ALLERGY_PERMISSIONS.update) ?? false;
  const canToggleActive = user?.permissions?.includes(ALLERGY_PERMISSIONS.delete) ?? false;
  // SetNoKnownAllergies is a patient-record mutation and requires Patients.Update, not any Allergies.* permission.
  const canUpdatePatient = user?.permissions?.includes(PATIENT_PERMISSIONS.update) ?? false;

  const [showInactive, setShowInactive] = useState(false);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editAllergy, setEditAllergy] = useState<PatientAllergy | null>(null);

  const allergiesQuery = useQuery({
    queryKey: ["patient-allergies", patientId, showInactive],
    queryFn: () =>
      searchPatientAllergies({ patientId, includeInactive: showInactive, pageSize: 200 }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: open,
  });

  const allergies = useMemo(() => allergiesQuery.data?.items ?? [], [allergiesQuery.data]);
  const noKnownAllergies = patientQuery.data?.hasNoKnownAllergies ?? false;

  const noAllergiesMutation = useMutation({
    mutationFn: (value: boolean) => setNoKnownAllergies(patientId, value),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["patients", patientId] });
    },
    onError: (err) => {
      // Legacy rule surfaces here as a 409 with the exact message.
      toast.warning("Could not change No Allergies.", { description: describe(err) });
      void queryClient.invalidateQueries({ queryKey: ["patients", patientId] });
    },
  });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Allergy List</DialogTitle>
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
                    checked={noKnownAllergies}
                    disabled={!canUpdatePatient || noAllergiesMutation.isPending}
                    onChange={(e) => noAllergiesMutation.mutate(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Set No Allergies</span>
                </label>
                {canCreate && (
                  <Button
                    size="sm"
                    className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                    onClick={() => {
                      setEditAllergy(null);
                      setEditorOpen(true);
                    }}
                  >
                    <Plus className="size-4" />
                    Add Allergy
                  </Button>
                )}
              </div>
            </div>

            {allergiesQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : allergies.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                {noKnownAllergies
                  ? "Patient marked as having no known allergies."
                  : "No allergies recorded for this patient."}
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {allergies.map((a) => (
                  <li key={a.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium">{a.drugName}</p>
                      {(a.reaction || a.comments) && (
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {[a.reaction, a.comments].filter(Boolean).join(" · ")}
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="hidden text-[12px] text-[var(--color-muted-foreground)] sm:inline">
                        {formatDate(a.dateNoted)}
                      </span>
                      <EntityStatusBadge tone={a.isActive ? "success" : "warning"}>
                        {a.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                      {canUpdate && (
                        <button
                          type="button"
                          title="Edit allergy"
                          aria-label="Edit allergy"
                          onClick={() => {
                            setEditAllergy(a);
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

      <AllergyDialog
        patientId={patientId}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditAllergy(null);
        }}
        allergy={editAllergy}
        canToggleActive={canToggleActive}
      />
    </>
  );
}
