import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getPatientById } from "@/api/patients";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { usePatientWorkspace } from "@/state/patient-workspace-context";
import { PatientInfoEditor } from "@/pages/patients/patient-info-editor";
import { formatDate } from "@/lib/list-helpers";

function ageOf(dob: string): number {
  const d = new Date(dob);
  if (Number.isNaN(d.getTime())) return 0;
  const now = new Date();
  let age = now.getFullYear() - d.getFullYear();
  const m = now.getMonth() - d.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < d.getDate())) age--;
  return age;
}

export function PatientInfoDialog({
  patientId,
  open,
  onClose,
}: {
  patientId: string;
  open: boolean;
  onClose: () => void;
}) {
  const navigate = useNavigate();
  const { closePatient } = usePatientWorkspace();

  // Same key the chart card reads → shared cache, single invalidation refreshes both.
  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: open && !!patientId,
  });
  const patient = patientQuery.data;

  const handleDeleted = () => {
    onClose();
    closePatient(patientId);
    navigate("/patient-charts");
  };

  const title = patient
    ? [patient.demographics.firstName, patient.demographics.middleInitial, patient.demographics.lastName]
        .filter(Boolean)
        .join(" ")
    : "Patient Info";

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-4xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {patient && (
            <DialogDescription>
              {patient.patientCode} · {formatDate(patient.demographics.dateOfBirth)} (
              {ageOf(patient.demographics.dateOfBirth)} yrs)
            </DialogDescription>
          )}
        </DialogHeader>
        <DialogBody className="max-h-[70vh] overflow-y-auto">
          {patientQuery.isLoading ? (
            <div className="skeleton h-96 rounded-xl" />
          ) : patient ? (
            <PatientInfoEditor patient={patient} onDeleted={handleDeleted} />
          ) : (
            <p className="py-12 text-center text-[13px] text-[var(--color-muted-foreground)]">
              Patient not found.
            </p>
          )}
        </DialogBody>
      </DialogContent>
    </Dialog>
  );
}
