import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Stethoscope } from "lucide-react";
import { getPatientById } from "@/api/patients";
import { Button } from "@/components/ui/button";
import { EntityDetailBack, ErrorBand } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { PatientInfoEditor } from "@/pages/patients/patient-info-editor";

export function PatientDetailPage() {
  const { patientId = "" } = useParams<{ patientId: string }>();
  const navigate = useNavigate();

  const patientQuery = useQuery({
    queryKey: ["patients", "detail", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: !!patientId,
  });
  const patient = patientQuery.data;

  return (
    <div className="pb-12">
      <EntityDetailBack to={`/patient-charts/${patientId}`} label="Back to chart" />
      {patientQuery.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(patientQuery.error)} />
        </div>
      )}
      {patientQuery.isLoading ? (
        <div className="skeleton h-96 rounded-xl" />
      ) : patient ? (
        <>
          <h1 className="mb-5 text-[22px] font-semibold">
            {[patient.demographics.firstName, patient.demographics.middleInitial, patient.demographics.lastName]
              .filter(Boolean)
              .join(" ")}
          </h1>
          <PatientInfoEditor patient={patient} onDeleted={() => navigate("/patient-charts")} />
        </>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-8 py-16 text-center">
          <Stethoscope className="mb-4 size-7 text-[var(--color-primary)]" />
          <h3 className="mb-1.5 text-[17px] font-semibold">Patient not found</h3>
          <p className="mb-6 text-[13px] text-[var(--color-muted-foreground)]">
            It may have been deleted, or the link may be wrong.
          </p>
          <Button asChild variant="outline" size="sm">
            <Link to="/patient-charts">Back to Patient Chart</Link>
          </Button>
        </div>
      )}
    </div>
  );
}
