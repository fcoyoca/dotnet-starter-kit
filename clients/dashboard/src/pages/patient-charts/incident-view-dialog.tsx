import { useQuery } from "@tanstack/react-query";
import { Pencil } from "lucide-react";
import { getPatientIncident } from "@/api/incidents";
import { useDepartmentOptions, useIncidentTypeOptions } from "@/api/administration";
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
import { formatDate } from "@/lib/list-helpers";

type Props = {
  incidentId: string | null;
  open: boolean;
  onClose(): void;
  onEdit?(): void;
};

function Row({ label, value }: { label: string; value?: string | null | React.ReactNode }) {
  if (!value && value !== 0) return null;
  return (
    <div>
      <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--color-muted-foreground)]">
        {label}
      </p>
      <p className="mt-0.5 text-[13px]">{value}</p>
    </div>
  );
}

export function IncidentViewDialog({ incidentId, open, onClose, onEdit }: Props) {
  const { data: incident, isLoading } = useQuery({
    queryKey: ["incident", incidentId],
    queryFn: () => getPatientIncident(incidentId!),
    enabled: !!incidentId && open,
  });

  const departmentOptions = useDepartmentOptions();
  const incidentTypeOptions = useIncidentTypeOptions();

  const resolveLabel = (id: string | null | undefined, options: { value: string; label: string }[] | undefined) => {
    if (!id || !options) return id ?? "—";
    return options.find((o) => o.value === id)?.label ?? id;
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
        <DialogHeader>
          <DialogTitle>Incident Details</DialogTitle>
        </DialogHeader>

        <DialogBody>
          {isLoading ? (
            <div className="space-y-3">
              {[1, 2, 3, 4].map((i) => (
                <div key={i} className="skeleton h-8 rounded" />
              ))}
            </div>
          ) : incident ? (
            <div className="grid gap-4 sm:grid-cols-2">
              {/* Left column — core incident info */}
              <div className="space-y-4">
                <Row label="Date of Loss" value={formatDate(incident.dateOfLoss)} />
                <Row
                  label="Date of Initial Visit"
                  value={incident.dateOfInitialVisit ? formatDate(incident.dateOfInitialVisit) : null}
                />
                <Row
                  label="Incident Type"
                  value={resolveLabel(incident.incidentTypeId, incidentTypeOptions)}
                />
                <Row
                  label="Department"
                  value={resolveLabel(incident.departmentId, departmentOptions)}
                />
                <div>
                  <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--color-muted-foreground)]">
                    Status
                  </p>
                  <div className="mt-1 flex flex-wrap gap-1.5">
                    <EntityStatusBadge tone={incident.isClosed ? "default" : "success"}>
                      {incident.isClosed ? "Closed" : "Open"}
                    </EntityStatusBadge>
                    {incident.isTransfer && (
                      <EntityStatusBadge tone="info">Transfer</EntityStatusBadge>
                    )}
                  </div>
                </div>
                {incident.isAccident && (
                  <div className="space-y-2">
                    <Row label="Accident Type" value={incident.accidentType ?? null} />
                    <Row label="Accident State" value={incident.accidentState ?? null} />
                  </div>
                )}
                <Row label="Patient Status" value={incident.patientStatus} />
                {incident.adherenceToPlan !== null && incident.adherenceToPlan !== undefined && (
                  <Row label="Adherence to Plan" value={`${incident.adherenceToPlan} / 10`} />
                )}
                {incident.comments && (
                  <div>
                    <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--color-muted-foreground)]">
                      Comments
                    </p>
                    <p className="mt-0.5 whitespace-pre-wrap text-[13px]">{incident.comments}</p>
                  </div>
                )}
                {incident.summaryOfCare && (
                  <div>
                    <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--color-muted-foreground)]">
                      Summary of Care
                    </p>
                    <p className="mt-0.5 whitespace-pre-wrap text-[13px]">{incident.summaryOfCare}</p>
                  </div>
                )}
              </div>

              {/* Right column — DX codes */}
              <div>
                <p className="text-[11px] font-medium uppercase tracking-wide text-[var(--color-muted-foreground)]">
                  DX Codes ({incident.diagnosticIds.length})
                </p>
                {incident.diagnosticIds.length === 0 ? (
                  <p className="mt-1 text-[13px] text-[var(--color-muted-foreground)]">None</p>
                ) : (
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    {incident.diagnosticIds.map((id) => (
                      <span
                        key={id}
                        className="inline-flex items-center rounded-full bg-[var(--color-accent)] px-2 py-0.5 text-[11px] font-medium"
                      >
                        {id.slice(0, 8)}…
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          ) : null}
        </DialogBody>

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Close
            </Button>
          </DialogClose>
          {onEdit && (
            <Button type="button" onClick={onEdit}>
              <Pencil className="mr-1.5 size-4" />
              Edit
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
