import { useEffect, useMemo, useState, type FormEvent } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarClock, ChevronLeft, ChevronRight, Plus } from "lucide-react";
import { toast } from "sonner";
import {
  cancelAppointment,
  checkInAppointment,
  checkOutAppointment,
  createAppointment,
  deleteAppointment,
  noShowAppointment,
  listAppointments,
  updateAppointment,
  type AppointmentDto,
} from "@/api/scheduling";
import { listClinics, listProviders, type ClinicDto, type ProviderDto } from "@/api/administration";
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
import { EntityPageHeader, Field } from "@/components/list";
import { describe } from "@/lib/list-helpers";

// ─── timezone helpers (Intl-only; no extra deps) ───────────────────────

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
  const map: Record<string, string> = {};
  for (const p of dtf.formatToParts(instant)) map[p.type] = p.value;
  const asUtc = Date.UTC(+map.year, +map.month - 1, +map.day, +map.hour, +map.minute, +map.second);
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

function addDaysYmd(ymd: string, days: number): string {
  const [y, mo, d] = ymd.split("-").map(Number);
  const dt = new Date(Date.UTC(y, mo - 1, d));
  dt.setUTCDate(dt.getUTCDate() + days);
  return dt.toISOString().slice(0, 10);
}

/** Today's calendar date (YYYY-MM-DD) as seen in `timeZone`. */
function todayYmdInTz(timeZone: string): string {
  const map: Record<string, string> = {};
  for (const p of new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date())) {
    map[p.type] = p.value;
  }
  return `${map.year}-${map.month}-${map.day}`;
}

function fmtTime(iso: string, timeZone: string): string {
  return new Intl.DateTimeFormat("en-US", {
    timeZone,
    hour: "numeric",
    minute: "2-digit",
  }).format(new Date(iso));
}

/** Clinic-local HH:mm (24h) for an instant — used to seed <input type="time">. */
function fmtTimeValue(iso: string, timeZone: string): string {
  const map: Record<string, string> = {};
  for (const p of new Intl.DateTimeFormat("en-US", {
    timeZone,
    hourCycle: "h23",
    hour: "2-digit",
    minute: "2-digit",
  }).formatToParts(new Date(iso))) {
    map[p.type] = p.value;
  }
  return `${map.hour}:${map.minute}`;
}

function fmtDayLabel(ymd: string): string {
  const [y, mo, d] = ymd.split("-").map(Number);
  return new Intl.DateTimeFormat("en-US", {
    weekday: "short",
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date(Date.UTC(y, mo - 1, d, 12)));
}

// ─── grid constants ─────────────────────────────────────────────────────

const START_HOUR = 7;
const END_HOUR = 19;
const HOUR_PX = 56;
const PX_PER_MIN = HOUR_PX / 60;
const GRID_HEIGHT = (END_HOUR - START_HOUR) * HOUR_PX;

const STATUS_STYLES: Record<string, string> = {
  Scheduled: "border-l-[var(--brand-500)]",
  CheckedIn: "border-l-[var(--color-saffron-500,#eab308)]",
  CheckedOut: "border-l-[var(--color-emerald-500,#10b981)]",
};

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

  // Default the clinic selection once clinics load.
  useEffect(() => {
    if (!clinicId && clinics.length > 0) setClinicId(clinics[0].id);
  }, [clinicId, clinics]);

  const [date, setDate] = useState<string>("");
  // Seed the date to "today" in the clinic's timezone the first time we know the zone.
  useEffect(() => {
    if (!date && clinic) setDate(todayYmdInTz(timeZone));
  }, [date, clinic, timeZone]);

  const effectiveClinicId = clinic?.id ?? "";

  const { data: providersPage } = useQuery({
    queryKey: ["scheduling.providers", effectiveClinicId],
    queryFn: () =>
      listProviders({ primaryClinicId: effectiveClinicId, isActive: true, pageNumber: 1, pageSize: 200 }),
    enabled: Boolean(effectiveClinicId),
    staleTime: 5 * 60 * 1000,
  });
  const providers: ProviderDto[] = useMemo(() => providersPage?.items ?? [], [providersPage]);

  const dayWindow = useMemo(() => {
    if (!date || !clinic) return null;
    const fromUtc = zonedWallToUtc(date, "00:00", timeZone);
    const toUtc = zonedWallToUtc(addDaysYmd(date, 1), "00:00", timeZone);
    return { fromUtc: fromUtc.toISOString(), toUtc: toUtc.toISOString(), fromMs: fromUtc.getTime() };
  }, [date, clinic, timeZone]);

  const { data: appointments } = useQuery({
    queryKey: ["scheduling.appointments", effectiveClinicId, dayWindow?.fromUtc, dayWindow?.toUtc],
    queryFn: () =>
      listAppointments({ clinicId: effectiveClinicId, fromUtc: dayWindow!.fromUtc, toUtc: dayWindow!.toUtc }),
    enabled: Boolean(effectiveClinicId && dayWindow),
    placeholderData: keepPreviousData,
  });

  const providerName = (id: string) => {
    const p = providers.find((x) => x.id === id);
    return p ? `${p.lastName}, ${p.firstName}` : "Unknown provider";
  };

  // Columns: clinic providers, plus any provider that has an appointment today.
  const columnIds = useMemo(() => {
    const ids = new Set(providers.map((p) => p.id));
    for (const a of appointments ?? []) ids.add(a.providerId);
    return [...ids];
  }, [providers, appointments]);

  const byProvider = useMemo(() => {
    const map = new Map<string, AppointmentDto[]>();
    for (const a of appointments ?? []) {
      const list = map.get(a.providerId) ?? [];
      list.push(a);
      map.set(a.providerId, list);
    }
    return map;
  }, [appointments]);

  const [dialog, setDialog] = useState<
    { mode: "create"; providerId: string } | { mode: "edit"; appointment: AppointmentDto } | null
  >(null);

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["scheduling.appointments"] });

  return (
    <div className="space-y-4">
      <EntityPageHeader
        icon={CalendarClock}
        title="Appointments"
        description="Day-view scheduler across providers, in each clinic's local time."
      >
        <Button
          size="sm"
          disabled={columnIds.length === 0}
          onClick={() => setDialog({ mode: "create", providerId: columnIds[0] ?? "" })}
        >
          <Plus className="size-4" />
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

        <div className="ml-auto flex items-center gap-1">
          <Button variant="outline" size="icon-sm" aria-label="Previous day" onClick={() => setDate((d) => addDaysYmd(d, -1))}>
            <ChevronLeft className="size-4" />
          </Button>
          <Button variant="outline" size="sm" onClick={() => setDate(todayYmdInTz(timeZone))}>
            Today
          </Button>
          <Button variant="outline" size="icon-sm" aria-label="Next day" onClick={() => setDate((d) => addDaysYmd(d, 1))}>
            <ChevronRight className="size-4" />
          </Button>
          <span className="ml-2 min-w-[12rem] text-[13px] font-medium text-[var(--color-foreground)]">
            {date ? fmtDayLabel(date) : "—"}
            <span className="ml-2 text-[12px] font-normal text-[var(--color-muted-foreground)]">{timeZone}</span>
          </span>
        </div>
      </div>

      {/* Grid */}
      {columnIds.length === 0 ? (
        <div className="rounded-lg border border-[var(--color-border)] p-8 text-center text-[13px] text-[var(--color-muted-foreground)]">
          No active providers for this clinic. Add a provider in Administration to start scheduling.
        </div>
      ) : (
        <div className="overflow-x-auto rounded-lg border border-[var(--color-border)]">
          <div className="flex min-w-fit">
            {/* hour gutter */}
            <div className="sticky left-0 z-10 w-14 shrink-0 border-r border-[var(--color-border)] bg-[var(--color-background)]">
              <div className="h-9 border-b border-[var(--color-border)]" />
              <div className="relative" style={{ height: GRID_HEIGHT }}>
                {Array.from({ length: END_HOUR - START_HOUR }, (_, i) => (
                  <div
                    key={i}
                    className="absolute right-1 text-[11px] text-[var(--color-muted-foreground)]"
                    style={{ top: i * HOUR_PX - 6 }}
                  >
                    {((START_HOUR + i + 11) % 12) + 1}
                    {START_HOUR + i < 12 ? "a" : "p"}
                  </div>
                ))}
              </div>
            </div>

            {/* provider columns */}
            {columnIds.map((pid) => (
              <div key={pid} className="w-56 shrink-0 border-r border-[var(--color-border)] last:border-r-0">
                <div className="flex h-9 items-center truncate border-b border-[var(--color-border)] px-2 text-[12px] font-medium text-[var(--color-foreground)]">
                  {providerName(pid)}
                </div>
                <div
                  className="relative"
                  style={{ height: GRID_HEIGHT }}
                  onDoubleClick={() => setDialog({ mode: "create", providerId: pid })}
                >
                  {/* hour lines */}
                  {Array.from({ length: END_HOUR - START_HOUR }, (_, i) => (
                    <div
                      key={i}
                      className="absolute inset-x-0 border-t border-[var(--color-border)]/60"
                      style={{ top: i * HOUR_PX }}
                    />
                  ))}
                  {(byProvider.get(pid) ?? []).map((a) => {
                    const startMin = (new Date(a.startUtc).getTime() - (dayWindow?.fromMs ?? 0)) / 60000;
                    const endMin = (new Date(a.endUtc).getTime() - (dayWindow?.fromMs ?? 0)) / 60000;
                    const top = Math.max(0, (startMin - START_HOUR * 60) * PX_PER_MIN);
                    const height = Math.max(18, (endMin - startMin) * PX_PER_MIN);
                    return (
                      <button
                        key={a.id}
                        type="button"
                        onClick={() => setDialog({ mode: "edit", appointment: a })}
                        className={`absolute inset-x-1 overflow-hidden rounded-md border border-[var(--color-border)] border-l-2 bg-[var(--color-card)] px-1.5 py-1 text-left text-[11px] shadow-xs hover:ring-1 hover:ring-[var(--color-ring)] ${STATUS_STYLES[a.status] ?? ""} ${a.cancelled ? "opacity-50 line-through" : ""}`}
                        style={{ top, height }}
                      >
                        <div className="font-medium text-[var(--color-foreground)]">{fmtTime(a.startUtc, timeZone)}</div>
                        <div className="truncate text-[var(--color-muted-foreground)]">
                          {a.noShow ? "No-show · " : ""}
                          {a.notes || (a.patientId ? "Patient on file" : "(no notes)")}
                        </div>
                      </button>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {dialog && clinic && date && (
        <AppointmentDialog
          state={dialog}
          clinicId={clinic.id}
          date={date}
          timeZone={timeZone}
          providers={providers}
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
  | { mode: "create"; providerId: string }
  | { mode: "edit"; appointment: AppointmentDto };

function AppointmentDialog({
  state,
  clinicId,
  date,
  timeZone,
  providers,
  providerName,
  onClose,
  onChanged,
}: {
  state: DialogState;
  clinicId: string;
  date: string;
  timeZone: string;
  providers: ProviderDto[];
  providerName: (id: string) => string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const editing = state.mode === "edit" ? state.appointment : null;

  const [providerId, setProviderId] = useState(
    editing ? editing.providerId : state.mode === "create" ? state.providerId : "",
  );
  const [startTime, setStartTime] = useState(editing ? fmtTimeValue(editing.startUtc, timeZone) : "09:00");
  const [endTime, setEndTime] = useState(editing ? fmtTimeValue(editing.endUtc, timeZone) : "09:30");
  const [notes, setNotes] = useState(editing?.notes ?? "");

  const afterSuccess = (msg: string) => {
    toast.success(msg);
    onChanged();
    onClose();
  };
  const onError = (verb: string) => (err: unknown) => toast.error(`${verb} failed`, { description: describe(err) });

  const createMutation = useMutation({
    mutationFn: createAppointment,
    onSuccess: () => afterSuccess("Appointment created"),
    onError: onError("Create"),
  });
  const updateMutation = useMutation({
    mutationFn: updateAppointment,
    onSuccess: () => afterSuccess("Appointment updated"),
    onError: onError("Update"),
  });
  const deleteMutation = useMutation({
    mutationFn: deleteAppointment,
    onSuccess: () => afterSuccess("Appointment deleted"),
    onError: onError("Delete"),
  });
  const lifecycleMutation = useMutation({
    mutationFn: ({ id, fn }: { id: string; fn: (id: string) => Promise<void>; label: string }) => fn(id),
    onSuccess: (_, vars) => afterSuccess(vars.label),
    onError: onError("Action"),
  });

  const isPending =
    createMutation.isPending || updateMutation.isPending || deleteMutation.isPending || lifecycleMutation.isPending;
  const valid = Boolean(providerId) && Boolean(startTime) && Boolean(endTime) && endTime > startTime;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!valid) return;
    const startUtc = zonedWallToUtc(date, startTime, timeZone).toISOString();
    const endUtc = zonedWallToUtc(date, endTime, timeZone).toISOString();
    const payload = {
      clinicId,
      providerId,
      patientId: editing?.patientId ?? null,
      appointmentTypeId: editing?.appointmentTypeId ?? null,
      startUtc,
      endUtc,
      notes: notes.trim() || null,
    };
    if (editing) {
      updateMutation.mutate({ id: editing.id, ...payload });
    } else {
      createMutation.mutate(payload);
    }
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
              {editing
                ? `${providerName(editing.providerId)} · ${fmtTime(editing.startUtc, timeZone)}`
                : "Book a slot on the selected day. Times are in the clinic's local zone."}
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-3">
            <Field id="appt-provider" label="Provider">
              <select
                id="appt-provider"
                value={providerId}
                onChange={(e) => setProviderId(e.target.value)}
                className="h-9 w-full rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px]"
              >
                {providers.length === 0 && <option value="">No providers</option>}
                {providers.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.lastName}, {p.firstName}
                  </option>
                ))}
              </select>
            </Field>

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

            {editing && (
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
