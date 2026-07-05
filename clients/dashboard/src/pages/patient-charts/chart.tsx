import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  AlertTriangle,
  ArrowLeft,
  CalendarDays,
  ClipboardList,
  Eye,
  FilePlus,
  FileSearch,
  FileText,
  Lock,
  Pencil,
  Pill,
  Plus,
  Stethoscope,
  StickyNote,
  Tablets,
  Trash2,
} from "lucide-react";
import { toast } from "sonner";
import { getPatientById } from "@/api/patients";
import {
  searchPatientIncidents,
  closeIncident,
  deleteIncident,
  type PatientIncidentListItemDto,
} from "@/api/incidents";
import {
  listReportTypes,
  useDepartmentOptions,
  useIncidentTypeOptions,
} from "@/api/administration";
import { createReport, deleteReport, searchPatientReports } from "@/api/reports";
import { searchPatientProblems } from "@/api/problems";
import { searchPatientNotes } from "@/api/patient-notes";
import {
  ALLERGY_PERMISSIONS,
  INCIDENT_PERMISSIONS,
  MEDICATION_PERMISSIONS,
  NOTE_PERMISSIONS,
  PROBLEM_PERMISSIONS,
  REPORT_PERMISSIONS,
} from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { AllergyListDialog } from "@/pages/patient-charts/allergy-list-dialog";
import { IncidentDialog } from "@/pages/patient-charts/incident-dialog";
import { IncidentViewDialog } from "@/pages/patient-charts/incident-view-dialog";
import { MedicationListDialog } from "@/pages/patient-charts/medication-list-dialog";
import { PatientNotesDialog } from "@/pages/patient-charts/patient-notes-dialog";
import { ProblemListDialog } from "@/pages/patient-charts/problem-list-dialog";
import { ReportSearchDialog } from "@/pages/patient-charts/report-search-dialog";
import {
  SelectAppointmentDialog,
  type AppointmentSelection,
} from "@/pages/patient-charts/select-appointment-dialog";

type ClosedFilter = "all" | "open" | "closed";

const DESKTOP_COLS = "grid-cols-[1fr_1fr_1fr_84px_72px_auto]";

function resolveLabel(
  id: string | null | undefined,
  options: { value: string; label: string }[] | undefined,
): string {
  if (!id || !options) return id ? "—" : "—";
  return options.find((o) => o.value === id)?.label ?? "—";
}

function ageFromDob(dob: string | null | undefined): string {
  if (!dob) return "—";
  const d = new Date(dob);
  if (Number.isNaN(d.getTime())) return "—";
  const now = new Date();
  let age = now.getFullYear() - d.getFullYear();
  const m = now.getMonth() - d.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < d.getDate())) age--;
  return String(age);
}

function SidebarRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-2">
      <span className="text-[12px] font-medium text-[var(--color-muted-foreground)]">{label}</span>
      <span className="text-right text-[12px] font-medium">{value}</span>
    </div>
  );
}

function IconShortcut({
  label,
  onClick,
  disabled,
  tone = "default",
  children,
}: {
  label: string;
  onClick(): void;
  disabled?: boolean;
  tone?: "default" | "destructive";
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      disabled={disabled}
      onClick={onClick}
      className={[
        "inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)]",
        "transition-colors hover:bg-[var(--color-accent)] disabled:pointer-events-none disabled:opacity-40",
        tone === "destructive"
          ? "text-[var(--color-destructive)] hover:text-[var(--color-destructive)]"
          : "text-[var(--color-foreground)]",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

export function PatientChartDetailPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [closedFilter, setClosedFilter] = useState<ClosedFilter>("all");
  const [showDeleted, setShowDeleted] = useState(false);
  const [transferOnly, setTransferOnly] = useState(false);
  const [deptFilter, setDeptFilter] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);

  const [activeIncidentId, setActiveIncidentId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editIncidentId, setEditIncidentId] = useState<string | null>(null);
  const [viewIncidentId, setViewIncidentId] = useState<string | null>(null);
  const [reportSearchOpen, setReportSearchOpen] = useState(false);
  // Report type chosen from "Add Report", pending appointment selection before creation.
  const [pendingReportType, setPendingReportType] = useState<{ id: number; name: string } | null>(null);

  const canCreate = user?.permissions?.includes(INCIDENT_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(INCIDENT_PERMISSIONS.update) ?? false;
  const canClose = user?.permissions?.includes(INCIDENT_PERMISSIONS.close) ?? false;
  const canDelete = user?.permissions?.includes(INCIDENT_PERMISSIONS.delete) ?? false;

  const canViewReports = user?.permissions?.includes(REPORT_PERMISSIONS.view) ?? false;
  const canCreateReports = user?.permissions?.includes(REPORT_PERMISSIONS.create) ?? false;
  const canUpdateReports = user?.permissions?.includes(REPORT_PERMISSIONS.update) ?? false;
  const canDeleteReports = user?.permissions?.includes(REPORT_PERMISSIONS.delete) ?? false;

  const canViewProblems = user?.permissions?.includes(PROBLEM_PERMISSIONS.view) ?? false;
  const canViewAllergies = user?.permissions?.includes(ALLERGY_PERMISSIONS.view) ?? false;
  const canViewMedications = user?.permissions?.includes(MEDICATION_PERMISSIONS.view) ?? false;
  const canViewNotes = user?.permissions?.includes(NOTE_PERMISSIONS.view) ?? false;

  const [problemListOpen, setProblemListOpen] = useState(false);
  const [allergyListOpen, setAllergyListOpen] = useState(false);
  const [medicationListOpen, setMedicationListOpen] = useState(false);
  const [notesOpen, setNotesOpen] = useState(false);

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId!),
    enabled: !!patientId,
  });

  const isClosed = closedFilter === "all" ? null : closedFilter === "closed";

  const incidentsQuery = useQuery({
    queryKey: ["incidents", patientId, closedFilter, showDeleted],
    queryFn: () =>
      searchPatientIncidents({
        patientId: patientId!,
        isClosed,
        includeDeleted: showDeleted,
        pageSize: 100,
      }),
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
    onError: (err) => toast.error("Failed to close incident.", { description: describe(err) }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteIncident(id),
    onSuccess: () => {
      toast.success("Incident deleted.");
      void queryClient.invalidateQueries({ queryKey: ["incidents", patientId] });
    },
    onError: (err) => toast.error("Failed to delete incident.", { description: describe(err) }),
  });

  // ─── Patient reports (scoped to the active incident) ───
  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
    enabled: canViewReports || canCreateReports,
  });

  const reportsQuery = useQuery({
    queryKey: ["reports", activeIncidentId],
    queryFn: () => searchPatientReports({ incidentId: activeIncidentId!, pageSize: 100 }),
    enabled: canViewReports && !!activeIncidentId,
    placeholderData: keepPreviousData,
  });

  const reportTypeLabel = (id: number): string =>
    reportTypesQuery.data?.find((t) => t.id === id)?.name ?? "Report";

  const createReportMutation = useMutation({
    mutationFn: createReport,
    onSuccess: (reportId) => {
      setPendingReportType(null);
      void queryClient.invalidateQueries({ queryKey: ["reports", activeIncidentId] });
      navigate(`/patient-charts/${patientId}/reports/${reportId}`);
    },
    onError: (err) => toast.error("Failed to create report.", { description: describe(err) }),
  });

  const deleteReportMutation = useMutation({
    mutationFn: (id: string) => deleteReport(id),
    onSuccess: () => {
      toast.success("Report deleted.");
      void queryClient.invalidateQueries({ queryKey: ["reports", activeIncidentId] });
    },
    onError: (err) => toast.error("Failed to delete report.", { description: describe(err) }),
  });

  // Adding a report opens the Select Appointment dialog first (BackChart parity);
  // the report is created only once an appointment — or a manual date — is chosen.
  const onAddReport = (reportTypeId: number) => {
    if (!patientId || !activeIncidentId) return;
    setPendingReportType({ id: reportTypeId, name: reportTypeLabel(reportTypeId) });
  };

  const onConfirmAppointment = (selection: AppointmentSelection) => {
    if (!patientId || !activeIncidentId || !pendingReportType) return;
    createReportMutation.mutate({
      incidentId: activeIncidentId,
      patientId,
      reportTypeId: pendingReportType.id,
      reportDate: selection.reportDate,
      providerId: selection.providerId,
      clinicId: selection.clinicId,
      appointmentId: selection.appointmentId,
      isNoShow: false,
    });
  };

  // Medical-alert problems power the chart banner; the full list lives in the
  // Problem List dialog. Keyed under ["problems", patientId, ...] so the dialog's
  // mutations (which invalidate that prefix) also refresh this banner.
  const medicalAlertsQuery = useQuery({
    queryKey: ["problems", patientId, "alerts"],
    queryFn: () =>
      searchPatientProblems({ patientId: patientId!, medicalAlertsOnly: true, pageSize: 100 }),
    enabled: canViewProblems && !!patientId,
    placeholderData: keepPreviousData,
  });

  const medicalAlertProblems = useMemo(
    () => medicalAlertsQuery.data?.items ?? [],
    [medicalAlertsQuery.data],
  );

  // Medical-alert notes power the chart banner alongside medical-alert problems;
  // the full list lives in the Patient Notes dialog. Keyed under
  // ["patient-notes", patientId, ...] so the dialog's mutations (which invalidate
  // that prefix) also refresh this banner.
  const noteAlertsQuery = useQuery({
    queryKey: ["patient-notes", patientId, "alerts"],
    queryFn: () =>
      searchPatientNotes({ patientId: patientId!, medicalAlertsOnly: true, pageSize: 100 }),
    enabled: !!patientId && canViewNotes,
  });

  const medicalAlertNotes = useMemo(
    () => noteAlertsQuery.data?.items ?? [],
    [noteAlertsQuery.data],
  );

  const patient = patientQuery.data;
  const allIncidents = useMemo(() => incidentsQuery.data?.items ?? [], [incidentsQuery.data]);

  // Client-side refinement (the API filters by closed/deleted; these narrow further).
  const incidents = useMemo(
    () =>
      allIncidents.filter((x) => {
        if (deptFilter && x.departmentId !== deptFilter) return false;
        if (typeFilter && x.incidentTypeId !== typeFilter) return false;
        if (transferOnly && !x.isTransfer) return false;
        return true;
      }),
    [allIncidents, deptFilter, typeFilter, transferOnly],
  );

  // Keep an active incident selected (mirrors BackChart's ActiveIncident).
  useEffect(() => {
    if (incidents.length === 0) {
      setActiveIncidentId(null);
      return;
    }
    if (!activeIncidentId || !incidents.some((x) => x.id === activeIncidentId)) {
      setActiveIncidentId(incidents[0].id);
    }
  }, [incidents, activeIncidentId]);

  const activeIncident = useMemo<PatientIncidentListItemDto | null>(
    () => incidents.find((x) => x.id === activeIncidentId) ?? null,
    [incidents, activeIncidentId],
  );

  const fullName = patient
    ? [
        patient.demographics.firstName,
        patient.demographics.middleInitial,
        patient.demographics.lastName,
      ]
        .filter(Boolean)
        .join(" ")
    : "";

  return (
    <div className="space-y-4 sm:space-y-6">
      {/* Back link */}
      <Link
        to="/patient-charts"
        className="inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      >
        <ArrowLeft className="size-4" />
        Patient Chart
      </Link>

      {/* Medical alerts surfaced from the problem list and patient notes */}
      {((canViewProblems && medicalAlertProblems.length > 0) ||
        (canViewNotes && medicalAlertNotes.length > 0)) && (
        <div className="flex items-start gap-2.5 rounded-xl border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-4 py-3">
          <AlertTriangle className="mt-0.5 size-4 shrink-0 text-[var(--color-destructive)]" />
          <div className="min-w-0">
            <p className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-destructive)]">
              Medical Alerts
            </p>
            <ul className="mt-0.5 space-y-0.5">
              {medicalAlertProblems.map((p) => (
                <li key={p.id} className="text-[13px]">
                  <span className="font-medium">{p.diagnosticCode}</span>
                  {p.diagnosticDescription ? ` — ${p.diagnosticDescription}` : ""}
                </li>
              ))}
              {canViewNotes &&
                medicalAlertNotes.map((n) => (
                  <li key={n.id} className="text-[13px]">
                    <span className="font-medium">{n.name}</span>
                    {n.description ? ` — ${n.description}` : ""}
                  </li>
                ))}
            </ul>
          </div>
        </div>
      )}

      {/* Chart actions — opens per-section dialogs (Problem List today; Allergy/
          Medication/Notes/Export/Outcome/Documents to follow). */}
      <div className="flex flex-wrap items-center gap-2">
        {canViewProblems && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            onClick={() => setProblemListOpen(true)}
          >
            <Stethoscope className="size-4" />
            Problem List
          </Button>
        )}
        {canViewAllergies && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            onClick={() => setAllergyListOpen(true)}
          >
            <Pill className="size-4" />
            Allergy List
          </Button>
        )}
        {canViewMedications && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            onClick={() => setMedicationListOpen(true)}
          >
            <Tablets className="size-4" />
            Medication List
          </Button>
        )}
        {canViewNotes && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            onClick={() => setNotesOpen(true)}
          >
            <StickyNote className="size-4" />
            Patient Notes
          </Button>
        )}
      </div>

      <div className="grid gap-4 lg:grid-cols-[330px_1fr]">
        {/* ─── Left: patient minimal info + incident shortcuts ─── */}
        <div className="space-y-4 lg:sticky lg:top-4 lg:self-start">
          {/* Patient Info card */}
          {patientQuery.isLoading ? (
            <div className="skeleton h-64 rounded-xl" />
          ) : patient ? (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[13px]">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  Patient Info
                </h2>
                <div className="flex items-center gap-2">
                  <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
                    {patient.isActive ? "Active" : "Inactive"}
                  </EntityStatusBadge>
                  <Link
                    to={`/patients/${patientId}`}
                    title="Edit patient info"
                    aria-label="Edit patient info"
                    className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <Pencil className="size-4" />
                  </Link>
                </div>
              </div>

              <p className="text-[15px] font-semibold leading-tight">{fullName}</p>

              <div className="mt-3 space-y-1.5">
                <SidebarRow label="Code" value={patient.patientCode} />
                <SidebarRow
                  label="DOB"
                  value={`${formatDate(patient.demographics.dateOfBirth)} · ${ageFromDob(patient.demographics.dateOfBirth)}y`}
                />
                <SidebarRow label="Gender" value={patient.demographics.gender || "—"} />
              </div>

              <div className="mt-3 rounded-lg border border-[var(--color-border)] p-2">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                  Insurance
                </p>
                <p className="mt-0.5 text-[13px]">
                  {patient.insurance?.insuredFullName || "—"}
                </p>
              </div>

              {patient.demographics.medicalAlertNotes && (
                <div className="mt-3 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-[12px] font-medium text-[var(--color-destructive)]">
                  ⚠ Medical Alert: {patient.demographics.medicalAlertNotes}
                </div>
              )}

              {/* Appointments / visit dates */}
              <div className="mt-3 flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                <CalendarDays className="size-3.5" />
                Appointments
              </div>
              <div className="mt-1.5 space-y-1.5">
                <SidebarRow label="Last Visit" value={formatDate(patient.lastVisitDate)} />
                <SidebarRow label="Next Visit" value={formatDate(patient.nextVisitDate)} />
              </div>
            </div>
          ) : (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[13px] text-[var(--color-muted-foreground)]">
              Patient not found.
            </div>
          )}

          {/* Incident shortcuts card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[13px]">
            <div className="mb-2 flex items-center justify-between">
              <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                Incident
              </h2>
              <div className="flex items-center gap-1.5">
                {canCreate && (
                  <IconShortcut label="Add incident" onClick={() => setCreateOpen(true)}>
                    <Plus className="size-4" />
                  </IconShortcut>
                )}
                {canUpdate && (
                  <IconShortcut
                    label="Edit selected incident"
                    disabled={!activeIncident}
                    onClick={() => activeIncident && setEditIncidentId(activeIncident.id)}
                  >
                    <Pencil className="size-4" />
                  </IconShortcut>
                )}
                <IconShortcut
                  label="View selected incident"
                  disabled={!activeIncident}
                  onClick={() => activeIncident && setViewIncidentId(activeIncident.id)}
                >
                  <Eye className="size-4" />
                </IconShortcut>
                <IconShortcut
                  label="Search patient reports"
                  disabled={!activeIncident}
                  onClick={() => setReportSearchOpen(true)}
                >
                  <FileSearch className="size-4" />
                </IconShortcut>
              </div>
            </div>

            {activeIncident ? (
              <div className="space-y-1.5 rounded-lg border border-[var(--color-border)] p-2.5">
                <SidebarRow
                  label="Date of Initial Visit"
                  value={formatDate(activeIncident.dateOfInitialVisit)}
                />
                <SidebarRow label="Date of Loss" value={formatDate(activeIncident.dateOfLoss)} />
                <SidebarRow
                  label="Incident Type"
                  value={resolveLabel(activeIncident.incidentTypeId, incidentTypeOptions)}
                />
                <SidebarRow
                  label="Department"
                  value={resolveLabel(activeIncident.departmentId, departmentOptions)}
                />
                <div className="flex flex-wrap gap-1.5 pt-1">
                  <EntityStatusBadge tone={activeIncident.isClosed ? "default" : "success"}>
                    {activeIncident.isClosed ? "Closed" : "Open"}
                  </EntityStatusBadge>
                  {activeIncident.isTransfer && (
                    <EntityStatusBadge tone="info">Transfer</EntityStatusBadge>
                  )}
                  {activeIncident.isAccident && (
                    <EntityStatusBadge tone="warning">Accident</EntityStatusBadge>
                  )}
                </div>
              </div>
            ) : (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                No incident selected. Add one or pick a row from the list.
              </p>
            )}
          </div>
        </div>

        {/* ─── Right: incidents list ─── */}
        <div>
          <EntityPageHeader
            icon={ClipboardList}
            title="Incidents"
            total={incidents.length}
            unit="incident"
            description="Clinical incidents (episodes of care) for this patient."
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

          {/* Filters */}
          <div className="mt-3 flex flex-wrap items-center gap-3">
            <EntityFilterPill
              label="Status"
              value={closedFilter}
              onChange={(v) => setClosedFilter(v as ClosedFilter)}
              options={[
                { value: "all", label: "All" },
                { value: "open", label: "Open" },
                { value: "closed", label: "Closed" },
              ]}
            />
            <div className="w-44">
              <Combobox
                id="filter-type"
                label="Incident type"
                value={typeFilter}
                onChange={setTypeFilter}
                options={incidentTypeOptions ?? []}
                placeholder="All types"
              />
            </div>
            <div className="w-44">
              <Combobox
                id="filter-dept"
                label="Department"
                value={deptFilter}
                onChange={setDeptFilter}
                options={departmentOptions ?? []}
                placeholder="All departments"
              />
            </div>
            <label className="flex items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={transferOnly}
                onChange={(e) => setTransferOnly(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Transfers only</span>
            </label>
            <label className="flex items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={showDeleted}
                onChange={(e) => setShowDeleted(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Show deleted</span>
            </label>
          </div>

          <div className="mt-4">
            {incidentsQuery.isLoading && allIncidents.length === 0 ? (
              <EntityListLoading desktopColumns={DESKTOP_COLS} />
            ) : incidents.length === 0 ? (
              <EntityEmpty
                icon={ClipboardList}
                title="No incidents"
                body={
                  closedFilter !== "all" || deptFilter || typeFilter || transferOnly || showDeleted
                    ? "No incidents match the current filters."
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
                    className={`${DESKTOP_COLS} cursor-pointer ${
                      incident.id === activeIncidentId ? "bg-[var(--color-accent)]" : ""
                    }`}
                    isLast={i === incidents.length - 1}
                    onClick={() => setActiveIncidentId(incident.id)}
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
                    <div
                      className="flex items-center justify-end gap-1"
                      onClick={(e) => e.stopPropagation()}
                    >
                      <IconShortcut label="View incident" onClick={() => setViewIncidentId(incident.id)}>
                        <Eye className="size-4" />
                      </IconShortcut>
                      {canUpdate && (
                        <IconShortcut
                          label="Edit incident"
                          onClick={() => setEditIncidentId(incident.id)}
                        >
                          <Pencil className="size-4" />
                        </IconShortcut>
                      )}
                      {canClose && !incident.isClosed && (
                        <IconShortcut
                          label="Close incident"
                          disabled={closeMutation.isPending}
                          onClick={() => closeMutation.mutate(incident.id)}
                        >
                          <Lock className="size-4" />
                        </IconShortcut>
                      )}
                      {canDelete && (
                        <IconShortcut
                          label="Delete incident"
                          tone="destructive"
                          disabled={deleteMutation.isPending}
                          onClick={() => deleteMutation.mutate(incident.id)}
                        >
                          <Trash2 className="size-4" />
                        </IconShortcut>
                      )}
                    </div>
                  </EntityListRow>
                ))}
              </EntityListCard>
            )}
          </div>
        </div>
      </div>

      {/* ─── Patient Reports (for the active incident) ─── */}
      {canViewReports && (
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <div className="mb-3 flex items-center justify-between gap-3">
            <h2 className="flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
              <FileText className="size-3.5" />
              Patient Reports
            </h2>
            {canCreateReports && (
              <DropdownMenu>
                <DropdownMenuTrigger asChild disabled={!activeIncident || createReportMutation.isPending}>
                  <Button
                    size="sm"
                    className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                    disabled={!activeIncident || createReportMutation.isPending}
                  >
                    <FilePlus className="size-4" />
                    Add Report
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end" className="max-h-[min(340px,55vh)] w-56 overflow-y-auto">
                  <DropdownMenuLabel>Report Type</DropdownMenuLabel>
                  {(reportTypesQuery.data ?? []).length === 0 ? (
                    <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
                      No report types defined.
                    </p>
                  ) : (
                    (reportTypesQuery.data ?? []).map((t) => (
                      <DropdownMenuItem key={t.id} onSelect={() => onAddReport(t.id)}>
                        {t.name}
                      </DropdownMenuItem>
                    ))
                  )}
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>

          {!activeIncident ? (
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              Select an incident to view its reports.
            </p>
          ) : reportsQuery.isLoading ? (
            <div className="skeleton h-16 rounded-lg" />
          ) : (reportsQuery.data?.items ?? []).length === 0 ? (
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              No reports for this incident yet.
            </p>
          ) : (
            <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
              {(reportsQuery.data?.items ?? []).map((r) => (
                <li key={r.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                  <div className="min-w-0">
                    <p className="truncate text-[13px] font-medium">{reportTypeLabel(r.reportTypeId)}</p>
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      {formatDate(r.reportDate)}
                      {r.signedByName ? ` · Signed by ${r.signedByName}` : ""}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <EntityStatusBadge tone={r.isSigned ? "info" : "default"}>
                      {r.workflowStatus}
                    </EntityStatusBadge>
                    <div className="flex items-center gap-1">
                      <IconShortcut
                        label="View report"
                        onClick={() => navigate(`/patient-charts/${patientId}/reports/${r.id}`)}
                      >
                        <Eye className="size-4" />
                      </IconShortcut>
                      {canUpdateReports && !r.isSigned && (
                        <IconShortcut
                          label="Edit report"
                          onClick={() => navigate(`/patient-charts/${patientId}/reports/${r.id}`)}
                        >
                          <Pencil className="size-4" />
                        </IconShortcut>
                      )}
                      {canDeleteReports && (
                        <IconShortcut
                          label="Delete report"
                          tone="destructive"
                          disabled={deleteReportMutation.isPending}
                          onClick={() => deleteReportMutation.mutate(r.id)}
                        >
                          <Trash2 className="size-4" />
                        </IconShortcut>
                      )}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {/* Create dialog */}
      {patientId && (
        <IncidentDialog
          patientId={patientId}
          open={createOpen}
          onClose={() => setCreateOpen(false)}
        />
      )}

      {/* Edit dialog */}
      {patientId && editIncidentId && (
        <IncidentDialog
          patientId={patientId}
          open={!!editIncidentId}
          onClose={() => setEditIncidentId(null)}
          incidentId={editIncidentId}
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
                const id = viewIncidentId;
                setViewIncidentId(null);
                setEditIncidentId(id);
              }
            : undefined
        }
      />

      {/* Problem List dialog (opens add/edit problem dialogs from within) */}
      {patientId && (
        <ProblemListDialog
          patientId={patientId}
          open={problemListOpen}
          onClose={() => setProblemListOpen(false)}
          incidentId={activeIncidentId}
        />
      )}

      {/* Allergy List dialog (opens add/edit allergy dialog from within) */}
      {patientId && (
        <AllergyListDialog
          patientId={patientId}
          open={allergyListOpen}
          onClose={() => setAllergyListOpen(false)}
        />
      )}

      {/* Medication List dialog (opens add/edit medication + reconciliation dialogs from within) */}
      {patientId && (
        <MedicationListDialog
          patientId={patientId}
          open={medicationListOpen}
          onClose={() => setMedicationListOpen(false)}
        />
      )}

      {/* Patient Notes dialog (opens add/edit note dialog from within) */}
      {patientId && (
        <PatientNotesDialog
          patientId={patientId}
          open={notesOpen}
          onClose={() => setNotesOpen(false)}
        />
      )}

      {/* Report search */}
      <ReportSearchDialog
        open={reportSearchOpen}
        onClose={() => setReportSearchOpen(false)}
        incident={activeIncident}
        incidentTypeLabel={resolveLabel(activeIncident?.incidentTypeId, incidentTypeOptions)}
      />

      {patientId && pendingReportType && (
        <SelectAppointmentDialog
          patientId={patientId}
          reportTypeName={pendingReportType.name}
          open={!!pendingReportType}
          creating={createReportMutation.isPending}
          onCancel={() => setPendingReportType(null)}
          onConfirm={onConfirmAppointment}
        />
      )}
    </div>
  );
}
