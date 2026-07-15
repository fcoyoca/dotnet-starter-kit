import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { RotateCcw } from "lucide-react";
import { toast } from "sonner";
import { restoreIncident, searchPatientIncidents } from "@/api/incidents";
import { restoreReport, searchPatientReports } from "@/api/reports";
import { listReportTypes, useDepartmentOptions, useIncidentTypeOptions } from "@/api/administration";
import { INCIDENT_PERMISSIONS, REPORT_PERMISSIONS } from "@/lib/patient-permissions";
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
import { describe, formatDate } from "@/lib/list-helpers";

type Props = {
  open: boolean;
  onClose(): void;
  patientId: string;
};

function resolveLabel(
  id: string | null | undefined,
  options: { value: string; label: string }[] | undefined,
): string {
  if (!id || !options) return "—";
  return options.find((o) => o.value === id)?.label ?? "—";
}

/**
 * Admin restore surface — lists this patient's soft-deleted incidents and
 * reports (fetched with `includeDeleted`, filtered to the deleted ones) and
 * offers a Restore action per row. Each section is gated by its own
 * `*.Restore` permission, so a user only sees what they can act on.
 */
export function DeletedItemsDialog({ open, onClose, patientId }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canRestoreIncidents = user?.permissions?.includes(INCIDENT_PERMISSIONS.restore) ?? false;
  const canRestoreReports = user?.permissions?.includes(REPORT_PERMISSIONS.restore) ?? false;

  const departmentOptions = useDepartmentOptions();
  const incidentTypeOptions = useIncidentTypeOptions();

  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
    enabled: open && canRestoreReports,
  });
  const reportTypeLabel = (id: number): string =>
    reportTypesQuery.data?.find((t) => t.id === id)?.name ?? "Report";

  const deletedIncidentsQuery = useQuery({
    queryKey: ["incidents", patientId, "deleted"],
    queryFn: () => searchPatientIncidents({ patientId, includeDeleted: true, pageSize: 200 }),
    enabled: open && canRestoreIncidents,
  });
  const deletedIncidents = useMemo(
    () => (deletedIncidentsQuery.data?.items ?? []).filter((x) => x.isDeleted),
    [deletedIncidentsQuery.data],
  );

  const deletedReportsQuery = useQuery({
    queryKey: ["reports", "by-patient", patientId, "deleted"],
    queryFn: () => searchPatientReports({ patientId, includeDeleted: true, pageSize: 200 }),
    enabled: open && canRestoreReports,
  });
  const deletedReports = useMemo(
    () => (deletedReportsQuery.data?.items ?? []).filter((x) => x.isDeleted),
    [deletedReportsQuery.data],
  );

  const invalidate = () => {
    // Incidents feed the chart's incident list; reports are keyed by incident,
    // so invalidate the whole ["reports"] prefix to cover whichever incident the
    // restored report belongs to, plus this dialog's own deleted-item queries.
    void queryClient.invalidateQueries({ queryKey: ["incidents", patientId] });
    void queryClient.invalidateQueries({ queryKey: ["reports"] });
  };

  const restoreIncidentMutation = useMutation({
    mutationFn: (id: string) => restoreIncident(id),
    onSuccess: () => {
      toast.success("Incident restored.");
      invalidate();
    },
    onError: (err) => toast.error("Failed to restore incident.", { description: describe(err) }),
  });

  const restoreReportMutation = useMutation({
    mutationFn: (id: string) => restoreReport(id),
    onSuccess: () => {
      toast.success("Report restored.");
      invalidate();
    },
    onError: (err) => toast.error("Failed to restore report.", { description: describe(err) }),
  });

  const restoring = restoreIncidentMutation.isPending || restoreReportMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <DialogHeader>
          <DialogTitle>Deleted Items</DialogTitle>
        </DialogHeader>

        <DialogBody className="space-y-5">
          {canRestoreIncidents && (
            <section>
              <h3 className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                Deleted Incidents
              </h3>
              {deletedIncidentsQuery.isLoading ? (
                <div className="skeleton h-12 rounded-lg" />
              ) : deletedIncidents.length === 0 ? (
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  No deleted incidents.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {deletedIncidents.map((incident) => (
                    <li
                      key={incident.id}
                      className="flex items-center justify-between gap-3 px-3 py-2.5 text-[13px]"
                    >
                      <div className="min-w-0">
                        <p className="truncate font-medium">
                          DOL: {formatDate(incident.dateOfLoss)}
                          {" · "}
                          {resolveLabel(incident.incidentTypeId, incidentTypeOptions)}
                        </p>
                        <p className="text-[12px] text-[var(--color-muted-foreground)]">
                          {resolveLabel(incident.departmentId, departmentOptions)}
                          {incident.dateOfInitialVisit
                            ? ` · DOIV: ${formatDate(incident.dateOfInitialVisit)}`
                            : ""}
                        </p>
                      </div>
                      <Button
                        size="sm"
                        variant="outline"
                        className="h-8 shrink-0 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                        disabled={restoring}
                        onClick={() => restoreIncidentMutation.mutate(incident.id)}
                      >
                        <RotateCcw className="size-4" />
                        Restore
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          )}

          {canRestoreReports && (
            <section>
              <h3 className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                Deleted Reports
              </h3>
              {deletedReportsQuery.isLoading ? (
                <div className="skeleton h-12 rounded-lg" />
              ) : deletedReports.length === 0 ? (
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  No deleted reports.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {deletedReports.map((r) => (
                    <li
                      key={r.id}
                      className="flex items-center justify-between gap-3 px-3 py-2.5 text-[13px]"
                    >
                      <div className="min-w-0">
                        <p className="truncate font-medium">{reportTypeLabel(r.reportTypeId)}</p>
                        <p className="text-[12px] text-[var(--color-muted-foreground)]">
                          {formatDate(r.reportDate)}
                          {r.signedByName ? ` · Signed by ${r.signedByName}` : ""}
                        </p>
                      </div>
                      <Button
                        size="sm"
                        variant="outline"
                        className="h-8 shrink-0 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                        disabled={restoring}
                        onClick={() => restoreReportMutation.mutate(r.id)}
                      >
                        <RotateCcw className="size-4" />
                        Restore
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
            </section>
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
  );
}
