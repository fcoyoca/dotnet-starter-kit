import { useEffect, useMemo, useRef, useState } from "react";
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
  CalendarPlus,
  ClipboardList,
  Eye,
  FileDown,
  FilePlus,
  FileSearch,
  FileText,
  FolderOpen,
  Pencil,
  Pill,
  Plus,
  Stethoscope,
  StickyNote,
  Tablets,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "sonner";
import { getPatientById, type PatientVisitRefDto } from "@/api/patients";
import {
  searchPatientIncidents,
  type PatientIncidentListItemDto,
} from "@/api/incidents";
import { listReportTypes, useClinicTimeZones, useIncidentTypeOptions } from "@/api/administration";
import { createReport, deleteReport, searchPatientReports } from "@/api/reports";
import { searchPatientProblems } from "@/api/problems";
import { searchPatientNotes } from "@/api/patient-notes";
import {
  ALLERGY_PERMISSIONS,
  DOCUMENT_PERMISSIONS,
  INCIDENT_PERMISSIONS,
  MEDICATION_PERMISSIONS,
  NOTE_PERMISSIONS,
  PROBLEM_PERMISSIONS,
  REPORT_PERMISSIONS,
  SUPERBILL_PERMISSIONS,
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
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate, formatDateTimeInTz } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";
import { usePatientTab, usePatientWorkspace } from "@/state/patient-workspace-context";
import { PatientTabStrip } from "@/components/layout/patient-tab-strip";
import { AllergyListDialog } from "@/pages/patient-charts/allergy-list-dialog";
import { DocumentsListDialog } from "@/pages/patient-charts/documents-list-dialog";
import { ExportReportsDialog } from "@/pages/patient-charts/export-reports-dialog";
import { IncidentDialog } from "@/pages/patient-charts/incident-dialog";
import { IncidentsListDialog } from "@/pages/patient-charts/incidents-list-dialog";
import { MedicationListDialog } from "@/pages/patient-charts/medication-list-dialog";
import { PatientNotesDialog } from "@/pages/patient-charts/patient-notes-dialog";
import { PatientInfoDialog } from "@/pages/patients/patient-info-dialog";
import { ProblemListDialog } from "@/pages/patient-charts/problem-list-dialog";
import { ProceduresPerformedDialog } from "@/pages/patient-charts/procedures-performed-dialog";
import { ReportEditorPanel } from "@/pages/patient-charts/report-editor-panel";
import { ReportSearchDialog } from "@/pages/patient-charts/report-search-dialog";
import {
  SelectAppointmentDialog,
  type AppointmentSelection,
} from "@/pages/patient-charts/select-appointment-dialog";

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

/**
 * A derived visit (last/next) rendered as its date + local time. Clicking opens
 * that appointment's dialog in the scheduler (BackChart parity). A patient with
 * no matching appointment shows a plain "—".
 */
function VisitDateLink({
  appt,
  clinicTimeZones,
  onOpen,
}: {
  appt: PatientVisitRefDto | null | undefined;
  clinicTimeZones: Map<string, string>;
  onOpen(appointmentId: string): void;
}) {
  if (!appt) return <>—</>;
  // Render the UTC instant in the owning clinic's timezone (matches the scheduler);
  // fall back to UTC until the clinics list has loaded / if the clinic is unknown.
  const timeZone = clinicTimeZones.get(appt.clinicId) ?? "UTC";
  return (
    <button
      type="button"
      title="Open appointment"
      onClick={() => onOpen(appt.appointmentId)}
      className="text-[var(--color-primary)] underline-offset-2 hover:underline"
    >
      {formatDateTimeInTz(appt.startUtc, timeZone)}
    </button>
  );
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
  const { openPatient, setActiveIncident, openReport, closeReport, setActiveReport } =
    usePatientWorkspace();
  const workspaceTab = usePatientTab(patientId);
  // clinicId → IANA tz, so the Last/Next visit rows render in the owning clinic's zone.
  const clinicTimeZones = useClinicTimeZones();
  const activeIncidentId = workspaceTab?.activeIncidentId ?? null;
  const openReportIds = workspaceTab?.openReportIds ?? [];
  const activeReportId = workspaceTab?.activeReportId ?? null;

  const [createOpen, setCreateOpen] = useState(false);
  const [editIncidentId, setEditIncidentId] = useState<string | null>(null);
  const [incidentsListOpen, setIncidentsListOpen] = useState(false);
  const [reportSearchOpen, setReportSearchOpen] = useState(false);
  // Report type chosen from "Add Report", pending appointment selection before creation.
  const [pendingReportType, setPendingReportType] = useState<{ id: number; name: string } | null>(null);

  const canCreate = user?.permissions?.includes(INCIDENT_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(INCIDENT_PERMISSIONS.update) ?? false;

  const canViewReports = user?.permissions?.includes(REPORT_PERMISSIONS.view) ?? false;
  const canCreateReports = user?.permissions?.includes(REPORT_PERMISSIONS.create) ?? false;
  const canUpdateReports = user?.permissions?.includes(REPORT_PERMISSIONS.update) ?? false;
  const canDeleteReports = user?.permissions?.includes(REPORT_PERMISSIONS.delete) ?? false;
  const canExportReports = user?.permissions?.includes(REPORT_PERMISSIONS.export) ?? false;
  const canViewSuperBills = user?.permissions?.includes(SUPERBILL_PERMISSIONS.view) ?? false;

  const canViewProblems = user?.permissions?.includes(PROBLEM_PERMISSIONS.view) ?? false;
  const canViewAllergies = user?.permissions?.includes(ALLERGY_PERMISSIONS.view) ?? false;
  const canViewMedications = user?.permissions?.includes(MEDICATION_PERMISSIONS.view) ?? false;
  const canViewNotes = user?.permissions?.includes(NOTE_PERMISSIONS.view) ?? false;
  const canViewDocuments = user?.permissions?.includes(DOCUMENT_PERMISSIONS.view) ?? false;

  const [problemListOpen, setProblemListOpen] = useState(false);
  const [allergyListOpen, setAllergyListOpen] = useState(false);
  const [medicationListOpen, setMedicationListOpen] = useState(false);
  const [notesOpen, setNotesOpen] = useState(false);
  const [documentsOpen, setDocumentsOpen] = useState(false);
  const [exportReportsOpen, setExportReportsOpen] = useState(false);
  const [proceduresOpen, setProceduresOpen] = useState(false);
  const [infoDialogOpen, setInfoDialogOpen] = useState(false);

  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId!),
    enabled: !!patientId,
  });

  const incidentsQuery = useQuery({
    queryKey: ["incidents", patientId],
    queryFn: () => searchPatientIncidents({ patientId: patientId!, pageSize: 100 }),
    enabled: !!patientId,
    placeholderData: keepPreviousData,
  });

  const incidentTypeOptions = useIncidentTypeOptions();

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
    // Use `variables.patientId` (the id the mutation was actually called
    // with), not the ambient `patientId` route param — this component stays
    // mounted across /patient-charts/:patientId navigations, so if the user
    // switches to a different patient's chart while this mutation is still
    // in flight, the ambient value would be the WRONG (new) patient by the
    // time onSuccess fires, opening the report under the wrong tab.
    onSuccess: (reportId, variables) => {
      setPendingReportType(null);
      void queryClient.invalidateQueries({ queryKey: ["reports", variables.incidentId] });
      openReport(variables.patientId, reportId);
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

  const fullName = patient
    ? [
        patient.demographics.firstName,
        patient.demographics.middleInitial,
        patient.demographics.lastName,
      ]
        .filter(Boolean)
        .join(" ")
    : "";

  // Calendar shortcut → scheduler with this patient carried in router state
  // (BackChart parity: the chart's "View Schedule" pre-seeds a New appointment).
  // The scheduler consumes the state once to open the create dialog pre-filled.
  const scheduleForPatient = () => {
    if (!patientId) return;
    const label = patient
      ? `${patient.demographics.lastName}, ${patient.demographics.firstName} · ${patient.patientCode}`
      : null;
    navigate("/scheduling/appointments", {
      state: { newApptPatientId: patientId, newApptPatientLabel: label },
    });
  };

  // Last/Next visit rows link to their appointment: navigate to the scheduler
  // carrying the appointment id, which the scheduler opens in its edit dialog.
  const openAppointment = (appointmentId: string) => {
    navigate("/scheduling/appointments", { state: { openAppointmentId: appointmentId } });
  };

  // Register/refresh this patient's workspace tab as soon as the patient
  // record loads — covers both "opened from search" (tab already exists,
  // just gets marked active) and a direct/bookmarked chart URL (creates
  // the tab). The cached label avoids a refetch just to render the tab strip.
  useEffect(() => {
    if (!patientId || !patient) return;
    openPatient(patientId, fullName || patient.patientCode);
  }, [patientId, patient, fullName, openPatient]);

  const incidents = useMemo(() => incidentsQuery.data?.items ?? [], [incidentsQuery.data]);

  // Keep an active incident selected (mirrors BackChart's ActiveIncident).
  // The "which incident is active" pointer now lives in the persisted
  // workspace context (keyed by patientId) instead of local useState, so
  // it survives navigating away from the chart and back.
  useEffect(() => {
    // Also depend on `workspaceTab` itself (not just the `activeIncidentId`
    // value derived from it): a brand-new tab starts with activeIncidentId
    // null, same as "no tab yet" — without this, setActiveIncident's no-op
    // (before the tab exists, see openPatient's registration effect above)
    // would never get retried once the tab actually gets created, since
    // every other dependency would look unchanged.
    if (!patientId || !workspaceTab) return;
    if (incidents.length === 0) {
      if (activeIncidentId !== null) setActiveIncident(patientId, null);
      return;
    }
    if (!activeIncidentId || !incidents.some((x) => x.id === activeIncidentId)) {
      setActiveIncident(patientId, incidents[0].id);
    }
  }, [incidents, activeIncidentId, patientId, setActiveIncident, workspaceTab]);

  const activeIncident = useMemo<PatientIncidentListItemDto | null>(
    () => incidents.find((x) => x.id === activeIncidentId) ?? null,
    [incidents, activeIncidentId],
  );

  // Open incidents feed the Incidents dialog (BackChart's chooser lists
  // open incidents only).
  const openIncidents = useMemo(() => incidents.filter((x) => !x.isClosed), [incidents]);

  // BackChart parity: when a patient chart first loads with more than one
  // open incident, the Incidents dialog pops up so the user picks which
  // incident to load. Prompt once per patient visit — refetches and filter
  // changes must not re-open it.
  const incidentsPromptedForRef = useRef<string | null>(null);
  useEffect(() => {
    if (!patientId || !incidentsQuery.data) return;
    if (incidentsPromptedForRef.current === patientId) return;
    incidentsPromptedForRef.current = patientId;
    if (openIncidents.length > 1) setIncidentsListOpen(true);
  }, [patientId, incidentsQuery.data, openIncidents]);

  // "Patient Reports" row in the Incident card scrolls to the reports list
  // (BackChart's card switches the workspace to the reports view; here the
  // list lives further down the rail).
  const reportsCardRef = useRef<HTMLDivElement | null>(null);

  return (
    <div className="flex flex-col gap-3 lg:h-full">
      {/* Back link */}
      <Link
        to="/patient-charts"
        className="inline-flex shrink-0 items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      >
        <ArrowLeft className="size-4" />
        Patient Chart
      </Link>

      {/* Open patient-chart tabs — scoped to the chart page (they used to live
          in the global AppShell and followed the user onto every route). */}
      <PatientTabStrip />

      <div className="grid gap-3 lg:min-h-0 lg:flex-1 lg:grid-cols-[380px_minmax(0,1fr)] lg:grid-rows-[minmax(0,1fr)]">
        {/* ─── Left rail: the Patient Info card (now also carrying the medical
            alerts and the chart-action buttons, BackChart-style), the incident
            shortcuts, and the reports list. On large screens the rail scrolls
            independently of the report box on the right. ─── */}
        <div className="min-w-0 space-y-3 lg:min-h-0 lg:overflow-y-auto lg:pr-1">
          {/* Patient Info card */}
          {patientQuery.isLoading ? (
            <div className="skeleton h-64 rounded-xl" />
          ) : patient ? (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3 text-[13px]">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  Patient Info
                </h2>
                <div className="flex items-center gap-2">
                  <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
                    {patient.isActive ? "Active" : "Inactive"}
                  </EntityStatusBadge>
                  <button
                    type="button"
                    title="Edit patient info"
                    aria-label="Edit patient info"
                    onClick={() => setInfoDialogOpen(true)}
                    className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <Pencil className="size-4" />
                  </button>
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

              {/* Medical alerts surfaced from the problem list and patient
                  notes — BackChart carries this inside the patient card. */}
              {((canViewProblems && medicalAlertProblems.length > 0) ||
                (canViewNotes && medicalAlertNotes.length > 0)) && (
                <div className="mt-3 flex items-start gap-2 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2">
                  <AlertTriangle className="mt-0.5 size-3.5 shrink-0 text-[var(--color-destructive)]" />
                  <div className="min-w-0">
                    <p className="text-[11px] font-semibold uppercase tracking-wide text-[var(--color-destructive)]">
                      Medical Alerts
                    </p>
                    <ul className="mt-0.5 space-y-0.5">
                      {medicalAlertProblems.map((p) => (
                        <li key={p.id} className="text-[12px]">
                          <span className="font-medium">{p.diagnosticCode}</span>
                          {p.diagnosticDescription ? ` — ${p.diagnosticDescription}` : ""}
                        </li>
                      ))}
                      {canViewNotes &&
                        medicalAlertNotes.map((n) => (
                          <li key={n.id} className="text-[12px]">
                            <span className="font-medium">{n.name}</span>
                            {n.description ? ` — ${n.description}` : ""}
                          </li>
                        ))}
                    </ul>
                  </div>
                </div>
              )}

              {/* Appointments / visit dates */}
              <div className="mt-3 flex items-center justify-between gap-1.5">
                <span className="flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  <CalendarDays className="size-3.5" />
                  Appointments
                </span>
                <IconShortcut label="Schedule appointment" onClick={scheduleForPatient}>
                  <CalendarPlus className="size-4" />
                </IconShortcut>
              </div>
              <div className="mt-1.5 space-y-1.5">
                <SidebarRow
                  label="Last Visit"
                  value={<VisitDateLink appt={patient.lastVisitAppointment} clinicTimeZones={clinicTimeZones} onOpen={openAppointment} />}
                />
                <SidebarRow
                  label="Next Visit"
                  value={<VisitDateLink appt={patient.nextVisitAppointment} clinicTimeZones={clinicTimeZones} onOpen={openAppointment} />}
                />
              </div>

              {/* Chart action shortcuts — BackChart parity: these live at the
                  bottom of the patient card, opening their per-section dialogs. */}
              {(canViewProblems ||
                canViewAllergies ||
                canViewMedications ||
                canViewNotes ||
                canViewDocuments ||
                canExportReports) && (
                <div className="mt-3 flex flex-wrap gap-1.5 border-t border-[var(--color-border)] pt-3">
                  {canViewProblems && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-2.5 text-[12px] font-semibold"
                      onClick={() => setProblemListOpen(true)}
                    >
                      <Stethoscope className="size-3.5" />
                      Problem List
                    </Button>
                  )}
                  {canViewAllergies && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-2.5 text-[12px] font-semibold"
                      onClick={() => setAllergyListOpen(true)}
                    >
                      <Pill className="size-3.5" />
                      Allergy List
                    </Button>
                  )}
                  {canViewMedications && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-2.5 text-[12px] font-semibold"
                      onClick={() => setMedicationListOpen(true)}
                    >
                      <Tablets className="size-3.5" />
                      Medication List
                    </Button>
                  )}
                  {canViewNotes && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-2.5 text-[12px] font-semibold"
                      onClick={() => setNotesOpen(true)}
                    >
                      <StickyNote className="size-3.5" />
                      Patient Notes
                    </Button>
                  )}
                  {canViewDocuments && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-2.5 text-[12px] font-semibold"
                      onClick={() => setDocumentsOpen(true)}
                    >
                      <FolderOpen className="size-3.5" />
                      Documents
                    </Button>
                  )}
                  {canExportReports && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-2.5 text-[12px] font-semibold"
                      disabled={!activeIncident}
                      onClick={() => setExportReportsOpen(true)}
                    >
                      <FileDown className="size-3.5" />
                      Export Reports
                    </Button>
                  )}
                </div>
              )}
            </div>
          ) : (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3 text-[13px] text-[var(--color-muted-foreground)]">
              Patient not found.
            </div>
          )}

          {/* Incident shortcuts card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3 text-[13px]">
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
                  label="View incidents"
                  disabled={openIncidents.length === 0}
                  onClick={() => setIncidentsListOpen(true)}
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

            {/* BackChart PatientChartCard parity: the card shows the selected
                incident's DOIV (click → Incidents dialog) and a Patient
                Reports shortcut, rather than a detail block. */}
            {activeIncident ? (
              <ul className="space-y-0.5">
                <li>
                  <button
                    type="button"
                    className="w-full rounded-md px-2 py-1.5 text-left text-[13px] font-semibold transition-colors hover:bg-[var(--color-accent)]"
                    onClick={() => setIncidentsListOpen(true)}
                  >
                    DOIV: {formatDate(activeIncident.dateOfInitialVisit)}
                  </button>
                </li>
                <li>
                  <button
                    type="button"
                    className="w-full rounded-md px-2 py-1.5 text-left text-[13px] font-semibold transition-colors hover:bg-[var(--color-accent)]"
                    onClick={() =>
                      reportsCardRef.current?.scrollIntoView({ behavior: "smooth", block: "start" })
                    }
                  >
                    Patient Reports
                  </button>
                </li>
              </ul>
            ) : (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                No incident selected. Add one or choose it from the Incidents dialog.
              </p>
            )}
          </div>

          {/* Patient Reports (for the active incident) — Add Report + rows.
              The open-report pill strip moved to the right panel. */}
          {canViewReports && (
            <div
              ref={reportsCardRef}
              className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3"
            >
              <div className="mb-3 flex items-center justify-between gap-3">
                <h2 className="flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  <FileText className="size-3.5" />
                  Patient Reports
                </h2>
                <div className="flex items-center gap-2">
                  {canViewSuperBills && (
                    <Button
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                      disabled={!activeIncident}
                      onClick={() => setProceduresOpen(true)}
                    >
                      <ClipboardList className="size-4" />
                      Procedures
                    </Button>
                  )}
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
                            onClick={() => patientId && openReport(patientId, r.id)}
                          >
                            <Eye className="size-4" />
                          </IconShortcut>
                          {canUpdateReports && !r.isSigned && (
                            <IconShortcut
                              label="Edit report"
                              onClick={() => patientId && openReport(patientId, r.id)}
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
        </div>

        {/* ─── Right: persistent report workspace (Part C) — a self-contained
            box whose interior is the only thing that scrolls (BackChart parity).
            The pill tab strip stays pinned as the box header; the active
            report's editor (rendered INLINE, no dialog) scrolls beneath it.
            Switching pills / navigating never closes a report; only a pill's
            explicit × removes it from openReportIds. ─── */}
        <div className="flex min-w-0 flex-col rounded-xl border border-[var(--color-border)] lg:min-h-0 lg:h-full">
          {openReportIds.length > 0 && (
            <div className="flex shrink-0 flex-wrap items-center gap-1.5 border-b border-[var(--color-border)] p-3">
              <span className="text-[11px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                Open reports
              </span>
              {openReportIds.map((id) => {
                const r = reportsQuery.data?.items.find((x) => x.id === id);
                const label = r ? reportTypeLabel(r.reportTypeId) : "Report";
                return (
                  <span
                    key={id}
                    className={cn(
                      "flex items-center gap-1 rounded-full border px-2.5 py-1 text-[11.5px] font-medium",
                      id === activeReportId
                        ? "border-[var(--color-primary)] bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                        : "border-[var(--color-border)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)]",
                    )}
                  >
                    <button
                      type="button"
                      onClick={() => patientId && setActiveReport(patientId, id)}
                      className="cursor-pointer"
                    >
                      {label}
                    </button>
                    <button
                      type="button"
                      aria-label={`Close ${label} report tab`}
                      onClick={() => patientId && closeReport(patientId, id)}
                      className="grid size-3.5 place-items-center rounded-full opacity-70 hover:opacity-100"
                    >
                      <X className="size-3" />
                    </button>
                  </span>
                );
              })}
            </div>
          )}

          {/* The only scroll region on the chart's right side. */}
          <div className="min-h-0 flex-1 overflow-y-auto p-3 sm:p-4">
            {patientId && activeReportId ? (
              <ReportEditorPanel patientId={patientId} reportId={activeReportId} />
            ) : (
              <div className="grid h-full min-h-[280px] place-items-center p-8 text-center">
                <div>
                  <FileText className="mx-auto size-8 text-[var(--color-muted-foreground)]" />
                  <p className="mt-2 text-[14px] font-medium">No report open</p>
                  <p className="mt-1 text-[12px] text-[var(--color-muted-foreground)]">
                    Select or add a report from the Patient Reports list.
                  </p>
                </div>
              </div>
            )}
          </div>
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

      {/* Patient Info edit dialog (replaces the old /patients/:id page) */}
      {patientId && (
        <PatientInfoDialog
          patientId={patientId}
          open={infoDialogOpen}
          onClose={() => setInfoDialogOpen(false)}
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

      {/* Incidents dialog — BackChart's open-incidents chooser. Pops up on
          initial chart load when the patient has more than one open incident,
          and opens from the Incident card's view (eye) button / DOIV row. */}
      {patientId && (
        <IncidentsListDialog
          open={incidentsListOpen}
          onClose={() => setIncidentsListOpen(false)}
          incidents={openIncidents}
          onSelect={(incidentId) => {
            setActiveIncident(patientId, incidentId);
            setIncidentsListOpen(false);
          }}
          onOpenReport={(incidentId, reportId) => {
            setActiveIncident(patientId, incidentId);
            openReport(patientId, reportId);
            setIncidentsListOpen(false);
          }}
        />
      )}

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

      {/* Documents dialog (uploads/downloads patient chart documents) */}
      {patientId && (
        <DocumentsListDialog
          patientId={patientId}
          open={documentsOpen}
          onClose={() => setDocumentsOpen(false)}
        />
      )}

      {/* Export Reports dialog (exports the active incident's reports as PDF) */}
      {activeIncidentId && (
        <ExportReportsDialog
          incidentId={activeIncidentId}
          open={exportReportsOpen}
          onClose={() => setExportReportsOpen(false)}
        />
      )}

      {/* Procedures Performed dialog (chart-shortcut context: report-picker phase first) */}
      {patientId && activeIncidentId && (
        <ProceduresPerformedDialog
          patientId={patientId}
          patientName={fullName || undefined}
          incidentId={activeIncidentId}
          reportId={null}
          open={proceduresOpen}
          onClose={() => setProceduresOpen(false)}
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
