import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  ArrowLeft,
  ClipboardList,
  Lock,
  LockOpen,
  Plus,
  Trash2,
  Eye,
  Pencil,
} from "lucide-react";
import { toast } from "sonner";
import { getPatientById } from "@/api/patients";
import {
  searchPatientIncidents,
  closeIncident,
  deleteIncident,
  type PatientIncidentListItemDto,
} from "@/api/incidents";
import { useDepartmentOptions, useIncidentTypeOptions } from "@/api/administration";
import { INCIDENT_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  EntityEmpty,
  EntityFilterPill,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
import { formatDate } from "@/lib/list-helpers";
import { IncidentDialog } from "@/pages/patient-charts/incident-dialog";
import { IncidentViewDialog } from "@/pages/patient-charts/incident-view-dialog";

type ClosedFilter = "all" | "open" | "closed";

const DESKTOP_COLS = "grid-cols-[1fr_1fr_1fr_80px_80px_auto]";

function resolveLabel(id: string | null | undefined, options: { value: string; label: string }[] | undefined): string {
  if (!id || !options) return id ?? "—";
  return options.find((o) => o.value === id)?.label ?? id;
}

export function PatientChartDetailPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [closedFilter, setClosedFilter] = useState<ClosedFilter>("all");
  const [createOpen, setCreateOpen] = useState(false);
  const [editIncident, setEditIncident] = useState<PatientIncidentListItemDto | null>(null);
  const [viewIncidentId, setViewIncidentId] = useState<string | null>(null);

  const canCreate = user?.permissions?.includes(INCIDENT_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(INCIDENT_PERMISSIONS.update) ?? false;
  const canClose  = user?.permissions?.includes(INCIDENT_PERMISSIONS.close) ?? false;
  const canDelete = user?.permissions?.includes(INCIDENT_PERMISSIONS.delete) ?? false;

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId!),
    enabled: !!patientId,
  });

  const isClosed =
    closedFilter === "all" ? null : closedFilter === "closed";

  const incidentsQuery = useQuery({
    queryKey: ["incidents", patientId, closedFilter],
    queryFn: () =>
      searchPatientIncidents({ patientId: patientId!, isClosed, pageSize: 100 }),
    enabled: !!patientId,
    placeholderData: keepPreviousData,
  });

  const departmentOptions = useDepartmentOptions();
  const incidentTypeOptions = useIncidentTypeOptions();

  const closeMutation = useMutation({
    mutationFn: (id: string) => closeIncident(id),
    onSuccess: () => {
      toast.success("Incident closed.");
      void queryClient.invalidateQueries({ queryKey: ["incidents", patientId] });
    },
    onError: () => toast.error("Failed to close incident."),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteIncident(id),
    onSuccess: () => {
      toast.success("Incident deleted.");
      void queryClient.invalidateQueries({ queryKey: ["incidents", patientId] });
    },
    onError: () => toast.error("Failed to delete incident."),
  });

  const patient = patientQuery.data;
  const incidents = incidentsQuery.data?.items ?? [];
  const fullName = patient
    ? [patient.demographics.firstName, patient.demographics.middleInitial, patient.demographics.lastName]
        .filter(Boolean)
        .join(" ")
    : "";

  return (
    <div className="space-y-6">
      {/* Back link */}
      <Link
        to="/patient-charts"
        className="inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      >
        <ArrowLeft className="size-4" />
        Patient Chart
      </Link>

      {/* Patient header */}
      {patientQuery.isLoading ? (
        <div className="h-24 animate-pulse rounded-xl bg-[var(--color-muted)]" />
      ) : patient ? (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-5">
          {patient.demographics.medicalAlertNotes && (
            <div className="mb-4 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-[12px] font-medium text-[var(--color-destructive)]">
              ⚠ Medical Alert: {patient.demographics.medicalAlertNotes}
            </div>
          )}
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h1 className="text-[20px] font-semibold">{fullName}</h1>
              <p className="mt-0.5 text-[13px] text-[var(--color-muted-foreground)]">
                {patient.patientCode} · DOB {formatDate(patient.demographics.dateOfBirth)} · {patient.demographics.gender}
              </p>
              {patient.insurance?.insuredFullName && (
                <p className="mt-1 text-[13px] text-[var(--color-muted-foreground)]">
                  Insured: {patient.insurance.insuredFullName}
                </p>
              )}
            </div>
            <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
              {patient.isActive ? "Active" : "Inactive"}
            </EntityStatusBadge>
          </div>
        </div>
      ) : null}

      {/* Incidents section */}
      <div>
        <EntityPageHeader
          icon={ClipboardList}
          title="Incidents"
          total={incidentsQuery.data?.totalCount ?? null}
          unit="incident"
          description="Clinical incidents for this patient."
        >
          {canCreate && (
            <Button
              onClick={() => setCreateOpen(true)}
              className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
            >
              <Plus className="size-4" />
              Add Incident
            </Button>
          )}
        </EntityPageHeader>

        <div className="mt-3 flex flex-wrap items-center gap-2">
          <EntityFilterPill
            label="Status"
            value={closedFilter}
            onChange={(v) => setClosedFilter(v as ClosedFilter)}
            options={[
              { value: "all",    label: "All" },
              { value: "open",   label: "Open" },
              { value: "closed", label: "Closed" },
            ]}
          />
        </div>

        <div className="mt-4">
          {incidentsQuery.isLoading && incidents.length === 0 ? (
            <EntityListLoading desktopColumns={DESKTOP_COLS} />
          ) : incidents.length === 0 ? (
            <EntityEmpty
              icon={ClipboardList}
              title="No incidents"
              body={
                closedFilter !== "all"
                  ? "No incidents match the current filter."
                  : "No incidents have been recorded for this patient yet."
              }
              action={
                canCreate ? (
                  <Button
                    onClick={() => setCreateOpen(true)}
                    className="h-9 rounded-lg px-4 text-[13px]"
                  >
                    <Plus className="mr-1.5 size-4" />
                    Add Incident
                  </Button>
                ) : undefined
              }
            />
          ) : (
            <EntityListCard>
              <EntityListHeader className={DESKTOP_COLS}>
                <span>Date of Loss</span>
                <span>Incident Type</span>
                <span>Department</span>
                <span>Status</span>
                <span>Transfer</span>
                <span />
              </EntityListHeader>

              {incidents.map((incident, i) => (
                <EntityListRow
                  key={incident.id}
                  className={DESKTOP_COLS}
                  isLast={i === incidents.length - 1}
                >
                  <span className="text-[13px]">{formatDate(incident.dateOfLoss)}</span>
                  <span className="truncate text-[13px] text-[var(--color-muted-foreground)]">
                    {resolveLabel(incident.incidentTypeId, incidentTypeOptions)}
                  </span>
                  <span className="truncate text-[13px] text-[var(--color-muted-foreground)]">
                    {resolveLabel(incident.departmentId, departmentOptions)}
                  </span>
                  <EntityStatusBadge tone={incident.isClosed ? "default" : "success"}>
                    {incident.isClosed ? "Closed" : "Open"}
                  </EntityStatusBadge>
                  <EntityStatusBadge tone={incident.isTransfer ? "info" : "default"}>
                    {incident.isTransfer ? "Yes" : "No"}
                  </EntityStatusBadge>
                  <div className="flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="icon"
                      className="size-7"
                      aria-label="View incident"
                      onClick={() => setViewIncidentId(incident.id)}
                    >
                      <Eye className="size-4" />
                    </Button>
                    {canUpdate && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-7"
                        aria-label="Edit incident"
                        onClick={() => setEditIncident(incident)}
                      >
                        <Pencil className="size-4" />
                      </Button>
                    )}
                    {canClose && !incident.isClosed && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-7"
                        aria-label="Close incident"
                        disabled={closeMutation.isPending}
                        onClick={() => closeMutation.mutate(incident.id)}
                      >
                        <Lock className="size-4" />
                      </Button>
                    )}
                    {canDelete && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="size-7 text-[var(--color-destructive)] hover:text-[var(--color-destructive)]"
                        aria-label="Delete incident"
                        disabled={deleteMutation.isPending}
                        onClick={() => deleteMutation.mutate(incident.id)}
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    )}
                  </div>
                </EntityListRow>
              ))}
            </EntityListCard>
          )}
        </div>
      </div>

      {/* Create dialog */}
      {patientId && (
        <IncidentDialog
          patientId={patientId}
          open={createOpen}
          onClose={() => setCreateOpen(false)}
        />
      )}

      {/* Edit dialog */}
      {patientId && editIncident && (
        <IncidentDialog
          patientId={patientId}
          open={!!editIncident}
          onClose={() => setEditIncident(null)}
          incidentId={editIncident.id}
        />
      )}

      {/* View dialog */}
      <IncidentViewDialog
        incidentId={viewIncidentId}
        open={!!viewIncidentId}
        onClose={() => setViewIncidentId(null)}
        onEdit={
          canUpdate && viewIncidentId
            ? () => {
                const inc = incidents.find((x) => x.id === viewIncidentId);
                if (inc) {
                  setViewIncidentId(null);
                  setEditIncident(inc);
                }
              }
            : undefined
        }
      />
    </div>
  );
}
