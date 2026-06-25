import { useEffect, useMemo, useState, type FormEvent } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Calendar, dateFnsLocalizer, type View } from "react-big-calendar";
import { format } from "date-fns/format";
import { parse } from "date-fns/parse";
import { startOfWeek } from "date-fns/startOfWeek";
import { startOfMonth } from "date-fns/startOfMonth";
import { endOfMonth } from "date-fns/endOfMonth";
import { endOfWeek } from "date-fns/endOfWeek";
import { getDay } from "date-fns/getDay";
import { enUS } from "date-fns/locale/en-US";
import { CalendarClock } from "lucide-react";
import { toast } from "sonner";
import "react-big-calendar/lib/css/react-big-calendar.css";
import {
  cancelAppointment,
  checkInAppointment,
  checkOutAppointment,
  createAppointment,
  deleteAppointment,
  noShowAppointment,
  listAppointments,
  updateAppointment,
  type AppointmentChangedEvent,
  type AppointmentDto,
} from "@/api/scheduling";
import {
  getScheduleConfig,
  listAppointmentTypes,
  listClinics,
  listProviders,
  type AppointmentTypeDto,
  type ClinicDto,
  type ProviderDto,
} from "@/api/administration";
import { useRealtimeEvent } from "@/realtime/realtime-context";
import { PatientPicker, patientLabel } from "@/components/scheduling/patient-picker";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { EntityPageHeader, Field } from "@/components/list";
import { cn } from "@/lib/cn";
import { describe } from "@/lib/list-helpers";

const localizer = dateFnsLocalizer({
  format,
  parse,
  startOfWeek,
  getDay,
  locales: { "en-US": enUS },
});

// ─── timezone helpers (Intl-only) ───────────────────────────────────────

function pad(n: number): string {
  return String(n).padStart(2, "0");
}

/** Offset (localWall − UTC) in ms for `instant` in `timeZone`. */
function tzOffsetMs(instant: Date, timeZone: string): number {
  const dtf = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
  const m: Record<string, string> = {};
  for (const p of dtf.formatToParts(instant)) m[p.type] = p.value;
  const asUtc = Date.UTC(+m.year, +m.month - 1, +m.day, +m.hour, +m.minute, +m.second);
  return asUtc - instant.getTime();
}

/** UTC instant for a wall-clock time (`ymd` = YYYY-MM-DD, `hhmm` = HH:mm) in `timeZone`. */
function zonedWallToUtc(ymd: string, hhmm: string, timeZone: string): Date {
  const [y, mo, d] = ymd.split("-").map(Number);
  const [h, mi] = hhmm.split(":").map(Number);
  const guess = Date.UTC(y, mo - 1, d, h, mi, 0);
  const offset = tzOffsetMs(new Date(guess), timeZone);
  return new Date(guess - offset);
}

/** A UTC instant → a browser-local Date whose fields equal the clinic-local wall time (for RBC rendering). */
function utcToClinicWallDate(iso: string, timeZone: string): Date {
  const m: Record<string, string> = {};
  for (const p of new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  }).formatToParts(new Date(iso))) {
    m[p.type] = p.value;
  }
  return new Date(+m.year, +m.month - 1, +m.day, +m.hour, +m.minute);
}

function localYmd(d: Date): string {
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
}

/** Clinic-local calendar date (YYYY-MM-DD) of an instant. */
function ymdInTz(iso: string, timeZone: string): string {
  const m: Record<string, string> = {};
  for (const p of new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date(iso))) {
    m[p.type] = p.value;
  }
  return `${m.year}-${m.month}-${m.day}`;
}

/** Clinic-local HH:mm (24h) of an instant — seeds <input type="time">. */
function timeValueInTz(iso: string, timeZone: string): string {
  const m: Record<string, string> = {};
  for (const p of new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    hour: "2-digit",
    minute: "2-digit",
  }).formatToParts(new Date(iso))) {
    m[p.type] = p.value;
  }
  return `${m.hour}:${m.minute}`;
}

function addMinutesToHhmm(hhmm: string, minutes: number): string {
  const [h, mi] = hhmm.split(":").map(Number);
  const total = (h * 60 + mi + minutes + 1440) % 1440;
  return `${pad(Math.floor(total / 60))}:${pad(total % 60)}`;
}

// ─── colors ─────────────────────────────────────────────────────────────

function eventColor(a: AppointmentDto, typeColor: string | null | undefined): string {
  if (a.isReservation) return "#8E8A7F"; // taupe
  if (a.cancelled) return "#ffa41b"; // orange
  if (a.noShow) return "#dd2c00"; // red
  if (a.status === "CheckedIn") return "#21bf73"; // green
  if (a.status === "CheckedOut") return "#929aab"; // gray
  return typeColor || "#7045af"; // type color or default purple
}

type CalEvent = {
  id: string;
  title: string;
  start: Date;
  end: Date;
  resourceId: string;
  appt: AppointmentDto;
};

// ─── page ───────────────────────────────────────────────────────────────

export function AppointmentsPage() {
  const queryClient = useQueryClient();

  const { data: clinicsPage } = useQuery({
    queryKey: ["scheduling.clinics"],
    queryFn: () => listClinics({ isActive: true, pageNumber: 1, pageSize: 100, sortBy: "name", sortDir: "asc" }),
    staleTime: 5 * 60 * 1000,
  });
  const clinics: ClinicDto[] = useMemo(() => clinicsPage?.items ?? [], [clinicsPage]);

  const [clinicId, setClinicId] = useState<string>("");
  const clinic = clinics.find((c) => c.id === clinicId) ?? clinics[0];
  const timeZone = clinic?.timeZoneId ?? "UTC";
  const effectiveClinicId = clinic?.id ?? "";

  useEffect(() => {
    if (!clinicId && clinics.length > 0) setClinicId(clinics[0].id);
  }, [clinicId, clinics]);

  const [view, setView] = useState<View>("day");
  const [date, setDate] = useState<Date>(() => new Date());

  const { data: providersPage } = useQuery({
    queryKey: ["scheduling.providers", effectiveClinicId],
    queryFn: () =>
      listProviders({ primaryClinicId: effectiveClinicId, isActive: true, pageNumber: 1, pageSize: 200 }),
    enabled: Boolean(effectiveClinicId),
    staleTime: 5 * 60 * 1000,
  });
  const providers: ProviderDto[] = useMemo(() => providersPage?.items ?? [], [providersPage]);

  const { data: types } = useQuery({
    queryKey: ["scheduling.appointmentTypes"],
    queryFn: () => listAppointmentTypes(true),
    staleTime: 5 * 60 * 1000,
  });
  const typesById = useMemo(() => {
    const map = new Map<string, AppointmentTypeDto>();
    for (const t of types ?? []) map.set(t.id, t);
    return map;
  }, [types]);

  const { data: config } = useQuery({
    queryKey: ["scheduling.config", effectiveClinicId],
    queryFn: () => getScheduleConfig(effectiveClinicId),
    enabled: Boolean(effectiveClinicId),
    staleTime: 5 * 60 * 1000,
    retry: false,
  });

  // Provider filter — empty set means "all".
  const [selectedProviders, setSelectedProviders] = useState<Set<string>>(new Set());
  const activeProviderIds = useMemo(
    () => (selectedProviders.size === 0 ? providers.map((p) => p.id) : [...selectedProviders]),
    [selectedProviders, providers],
  );

  // Visible window (clinic-local) → UTC range for the query.
  const window = useMemo(() => {
    if (!clinic) return null;
    let fromYmd: string;
    let toYmd: string;
    if (view === "month") {
      fromYmd = localYmd(startOfWeek(startOfMonth(date)));
      toYmd = localYmd(endOfWeek(endOfMonth(date)));
    } else {
      fromYmd = localYmd(date);
      toYmd = fromYmd;
    }
    const fromUtc = zonedWallToUtc(fromYmd, "00:00", timeZone).toISOString();
    // exclusive upper bound = day after the last visible day
    const [y, mo, d] = toYmd.split("-").map(Number);
    const next = new Date(Date.UTC(y, mo - 1, d));
    next.setUTCDate(next.getUTCDate() + 1);
    const toUtc = zonedWallToUtc(next.toISOString().slice(0, 10), "00:00", timeZone).toISOString();
    return { fromUtc, toUtc };
  }, [clinic, view, date, timeZone]);

  const { data: appointments } = useQuery({
    queryKey: ["scheduling.appointments", effectiveClinicId, window?.fromUtc, window?.toUtc],
    queryFn: () => listAppointments({ clinicId: effectiveClinicId, fromUtc: window!.fromUtc, toUtc: window!.toUtc }),
    enabled: Boolean(effectiveClinicId && window),
    placeholderData: keepPreviousData,
  });

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["scheduling.appointments"] });

  useRealtimeEvent<AppointmentChangedEvent>(
    "AppointmentChanged",
    (payload) => {
      if (payload.clinicId === effectiveClinicId) invalidate();
    },
    [effectiveClinicId],
  );

  const providerName = (id: string) => {
    const p = providers.find((x) => x.id === id);
    return p ? `${p.lastName}, ${p.firstName}` : "Unknown";
  };

  const events: CalEvent[] = useMemo(() => {
    const active = new Set(activeProviderIds);
    return (appointments ?? [])
      .filter((a) => active.has(a.providerId))
      .map((a) => {
        const type = a.appointmentTypeId ? typesById.get(a.appointmentTypeId) : undefined;
        const title = a.isReservation
          ? a.reservationTitle || "Reserved"
          : a.notes || type?.name || "Appointment";
        return {
          id: a.id,
          title,
          start: utcToClinicWallDate(a.startUtc, timeZone),
          end: utcToClinicWallDate(a.endUtc, timeZone),
          resourceId: a.providerId,
          appt: a,
        };
      });
  }, [appointments, activeProviderIds, typesById, timeZone]);

  const resources = useMemo(
    () => providers.filter((p) => activeProviderIds.includes(p.id)).map((p) => ({ id: p.id, title: providerName(p.id) })),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [providers, activeProviderIds],
  );

  // Business hours from ScheduleConfig (fallback 7a–7p).
  const { min, max } = useMemo(() => {
    const start = config?.startTime?.slice(0, 5) ?? "07:00";
    const end = config?.endTime?.slice(0, 5) ?? "19:00";
    const [sh, sm] = start.split(":").map(Number);
    const [eh, em] = end.split(":").map(Number);
    return { min: new Date(1970, 0, 1, sh, sm), max: new Date(1970, 0, 1, eh, em) };
  }, [config]);
  const step = config?.intervalMinutes ?? 30;

  const [dialog, setDialog] = useState<
    | { mode: "create"; providerId: string; ymd: string; startTime: string; endTime: string }
    | { mode: "edit"; appointment: AppointmentDto }
    | null
  >(null);

  const toggleProvider = (id: string) =>
    setSelectedProviders((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <div className="space-y-4">
      <EntityPageHeader
        icon={CalendarClock}
        title="Appointments"
        description="Schedule patients and reserve time across providers, in each clinic's local time."
      >
        <Button
          size="sm"
          disabled={providers.length === 0}
          onClick={() =>
            setDialog({
              mode: "create",
              providerId: activeProviderIds[0] ?? providers[0]?.id ?? "",
              ymd: localYmd(date),
              startTime: "09:00",
              endTime: "09:30",
            })
          }
        >
          New appointment
        </Button>
      </EntityPageHeader>

      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-2">
        <select
          aria-label="Clinic"
          value={effectiveClinicId}
          onChange={(e) => setClinicId(e.target.value)}
          className="h-9 rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px]"
        >
          {clinics.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        <span className="text-[12px] text-[var(--color-muted-foreground)]">{timeZone}</span>

        {providers.length > 0 && (
          <div className="ml-auto flex flex-wrap items-center gap-1">
            {providers.map((p) => {
              const on = selectedProviders.size === 0 || selectedProviders.has(p.id);
              return (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => toggleProvider(p.id)}
                  className={cn(
                    "rounded-full border px-2.5 py-1 text-[12px] transition-colors",
                    on
                      ? "border-[var(--color-primary)] bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                      : "border-[var(--color-border)] text-[var(--color-muted-foreground)]",
                  )}
                >
                  {p.lastName}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Calendar */}
      {providers.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] p-8 text-center text-[13px] text-[var(--color-muted-foreground)]">
          No active providers for this clinic. Add a provider in Administration to start scheduling.
        </div>
      ) : (
        <div className="rounded-lg border border-[var(--color-border)] bg-[var(--color-card)] p-2" style={{ height: 680 }}>
          <Calendar<CalEvent, { id: string; title: string }>
            localizer={localizer}
            events={events}
            startAccessor="start"
            endAccessor="end"
            views={["day", "month"]}
            view={view}
            onView={setView}
            date={date}
            onNavigate={setDate}
            step={step}
            timeslots={1}
            min={min}
            max={max}
            selectable
            popup
            resources={view === "day" ? resources : undefined}
            resourceIdAccessor="id"
            resourceTitleAccessor="title"
            eventPropGetter={(event) => ({
              style: {
                backgroundColor: eventColor(
                  event.appt,
                  event.appt.appointmentTypeId ? typesById.get(event.appt.appointmentTypeId)?.color : undefined,
                ),
                border: "none",
              },
            })}
            onSelectSlot={(slot) => {
              const start = slot.start as Date;
              const end = slot.end as Date;
              setDialog({
                mode: "create",
                providerId: (slot as { resourceId?: string }).resourceId ?? activeProviderIds[0] ?? "",
                ymd: localYmd(start),
                startTime: `${pad(start.getHours())}:${pad(start.getMinutes())}`,
                endTime: `${pad(end.getHours())}:${pad(end.getMinutes())}`,
              });
            }}
            onSelectEvent={(event) => setDialog({ mode: "edit", appointment: event.appt })}
          />
        </div>
      )}

      {dialog && clinic && (
        <AppointmentDialog
          state={dialog}
          clinicId={clinic.id}
          timeZone={timeZone}
          providers={providers}
          types={types ?? []}
          providerName={providerName}
          onClose={() => setDialog(null)}
          onChanged={invalidate}
        />
      )}
    </div>
  );
}

// ─── create / edit dialog ───────────────────────────────────────────────

type DialogState =
  | { mode: "create"; providerId: string; ymd: string; startTime: string; endTime: string }
  | { mode: "edit"; appointment: AppointmentDto };

function AppointmentDialog({
  state,
  clinicId,
  timeZone,
  providers,
  types,
  providerName,
  onClose,
  onChanged,
}: {
  state: DialogState;
  clinicId: string;
  timeZone: string;
  providers: ProviderDto[];
  types: AppointmentTypeDto[];
  providerName: (id: string) => string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const editing = state.mode === "edit" ? state.appointment : null;
  const creating = state.mode === "create" ? state : null;
  const ymd = editing ? ymdInTz(editing.startUtc, timeZone) : creating!.ymd;

  const [providerId, setProviderId] = useState(editing ? editing.providerId : creating!.providerId);
  const [isReservation, setIsReservation] = useState(editing?.isReservation ?? false);
  const [reservationTitle, setReservationTitle] = useState(editing?.reservationTitle ?? "");
  const [patientId, setPatientId] = useState<string | null>(editing?.patientId ?? null);
  const [appointmentTypeId, setAppointmentTypeId] = useState<string>(editing?.appointmentTypeId ?? "");
  const [startTime, setStartTime] = useState(
    editing ? timeValueInTz(editing.startUtc, timeZone) : creating!.startTime,
  );
  const [endTime, setEndTime] = useState(editing ? timeValueInTz(editing.endUtc, timeZone) : creating!.endTime);
  const [notes, setNotes] = useState(editing?.notes ?? "");

  const afterSuccess = (msg: string) => {
    toast.success(msg);
    onChanged();
    onClose();
  };
  const onError = (verb: string) => (err: unknown) => toast.error(`${verb} failed`, { description: describe(err) });

  const createMutation = useMutation({
    mutationFn: createAppointment,
    onSuccess: () => afterSuccess(isReservation ? "Reservation created" : "Appointment created"),
    onError: onError("Create"),
  });
  const updateMutation = useMutation({
    mutationFn: updateAppointment,
    onSuccess: () => afterSuccess("Saved"),
    onError: onError("Update"),
  });
  const deleteMutation = useMutation({
    mutationFn: deleteAppointment,
    onSuccess: () => afterSuccess("Deleted"),
    onError: onError("Delete"),
  });
  const lifecycleMutation = useMutation({
    mutationFn: ({ id, fn }: { id: string; fn: (id: string) => Promise<void>; label: string }) => fn(id),
    onSuccess: (_, vars) => afterSuccess(vars.label),
    onError: onError("Action"),
  });

  const isPending =
    createMutation.isPending || updateMutation.isPending || deleteMutation.isPending || lifecycleMutation.isPending;
  const valid =
    Boolean(providerId) &&
    Boolean(startTime) &&
    Boolean(endTime) &&
    endTime > startTime &&
    (!isReservation || reservationTitle.trim().length > 0);

  const onPickType = (id: string) => {
    setAppointmentTypeId(id);
    const t = types.find((x) => x.id === id);
    if (t && t.defaultDurationMinutes > 0) {
      setEndTime(addMinutesToHhmm(startTime, t.defaultDurationMinutes));
    }
  };

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    const startUtc = zonedWallToUtc(ymd, startTime, timeZone).toISOString();
    const endUtc = zonedWallToUtc(ymd, endTime, timeZone).toISOString();
    const payload = {
      clinicId,
      providerId,
      patientId: isReservation ? null : patientId,
      appointmentTypeId: isReservation || !appointmentTypeId ? null : appointmentTypeId,
      startUtc,
      endUtc,
      notes: notes.trim() || null,
      isReservation,
      reservationTitle: isReservation ? reservationTitle.trim() : null,
    };
    if (editing) updateMutation.mutate({ id: editing.id, ...payload });
    else createMutation.mutate(payload);
  };

  const runLifecycle = (fn: (id: string) => Promise<void>, label: string) => {
    if (editing) lifecycleMutation.mutate({ id: editing.id, fn, label });
  };

  return (
    <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{editing ? "Edit appointment" : "New appointment"}</DialogTitle>
            <DialogDescription>
              {providerName(providerId)} · times are in the clinic's local zone.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-3">
            <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
              <div>
                <p className="text-[13px] font-medium text-[var(--color-foreground)]">Reserve time</p>
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  Block the slot without a patient (e.g. lunch, admin).
                </p>
              </div>
              <Switch checked={isReservation} onCheckedChange={setIsReservation} aria-label="Reserve time" />
            </div>

            <Field id="appt-provider" label="Provider">
              <select
                id="appt-provider"
                value={providerId}
                onChange={(e) => setProviderId(e.target.value)}
                className="h-9 w-full rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px]"
              >
                {providers.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.lastName}, {p.firstName}
                  </option>
                ))}
              </select>
            </Field>

            {isReservation ? (
              <Field id="appt-title" label="Title">
                <Input
                  id="appt-title"
                  value={reservationTitle}
                  onChange={(e) => setReservationTitle(e.target.value)}
                  placeholder="Lunch, meeting, admin…"
                  maxLength={200}
                />
              </Field>
            ) : (
              <>
                <Field id="appt-patient" label="Patient" hint="Optional — leave empty for a walk-in / hold.">
                  <PatientPicker
                    value={patientId}
                    initialLabel={editing?.patientId ? "Selected patient" : null}
                    onChange={(id, p) => {
                      setPatientId(id);
                      if (p && !notes) setNotes(patientLabel(p));
                    }}
                  />
                </Field>

                <Field id="appt-type" label="Appointment type">
                  <select
                    id="appt-type"
                    value={appointmentTypeId}
                    onChange={(e) => onPickType(e.target.value)}
                    className="h-9 w-full rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px]"
                  >
                    <option value="">— none —</option>
                    {types.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.name} ({t.defaultDurationMinutes}m)
                      </option>
                    ))}
                  </select>
                </Field>
              </>
            )}

            <div className="grid grid-cols-2 gap-3">
              <Field id="appt-start" label="Start">
                <Input id="appt-start" type="time" value={startTime} onChange={(e) => setStartTime(e.target.value)} />
              </Field>
              <Field id="appt-end" label="End">
                <Input id="appt-end" type="time" value={endTime} onChange={(e) => setEndTime(e.target.value)} />
              </Field>
            </div>

            <Field id="appt-notes" label="Notes">
              <Input
                id="appt-notes"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="Reason for visit"
                maxLength={4000}
              />
            </Field>

            {editing && !editing.isReservation && (
              <div className="flex flex-wrap gap-1.5 border-t border-[var(--color-border)] pt-3">
                <Button type="button" variant="outline" size="sm" onClick={() => runLifecycle(checkInAppointment, "Checked in")}>
                  Check in
                </Button>
                <Button type="button" variant="outline" size="sm" onClick={() => runLifecycle(checkOutAppointment, "Checked out")}>
                  Check out
                </Button>
                <Button type="button" variant="outline" size="sm" onClick={() => runLifecycle(cancelAppointment, "Cancelled")}>
                  Cancel
                </Button>
                <Button type="button" variant="outline" size="sm" onClick={() => runLifecycle(noShowAppointment, "Marked no-show")}>
                  No-show
                </Button>
              </div>
            )}
          </DialogBody>

          <DialogFooter className="justify-between">
            {editing ? (
              <Button
                type="button"
                variant="destructive"
                size="sm"
                disabled={isPending}
                onClick={() => deleteMutation.mutate(editing.id)}
              >
                Delete
              </Button>
            ) : (
              <span />
            )}
            <div className="flex gap-2">
              <DialogClose asChild>
                <Button type="button" variant="outline" size="sm">
                  Close
                </Button>
              </DialogClose>
              <Button type="submit" size="sm" disabled={!valid || isPending}>
                {editing ? "Save" : "Create"}
              </Button>
            </div>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
