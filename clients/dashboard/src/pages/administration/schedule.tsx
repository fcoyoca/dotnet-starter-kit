import { useEffect, useMemo, useRef, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarClock, Clock, Palette, Pencil, Plus, Tag, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createAppointmentType,
  deleteAppointmentType,
  getScheduleConfig,
  listAppointmentTypes,
  listClinics,
  updateAppointmentType,
  upsertScheduleConfig,
  type AppointmentTypeDto,
  type ClinicDto,
} from "@/api/administration";
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
import {
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
  Field,
} from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { cn } from "@/lib/cn";

const SCHEDULE_KEY = ["administration", "schedule-config"] as const;
const TYPES_KEY = ["administration", "appointment-types"] as const;

const INTERVAL_OPTIONS = [5, 10, 15, 20, 30, 60] as const;

const selectClass = cn(
  "h-9 rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px] shadow-xs",
  "focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]",
);

/** API returns ISO time ("HH:mm:ss"); <input type="time"> wants "HH:mm". */
function toTimeInput(value: string): string {
  return value.slice(0, 5);
}

export function SchedulePage() {
  const [clinicId, setClinicId] = useState<string>("");

  const clinicsQuery = useQuery({
    queryKey: ["administration", "clinicOptions", "schedule"],
    queryFn: () => listClinics({ isActive: true, pageSize: 200, sortBy: "name", sortDir: "asc" }),
    staleTime: 5 * 60 * 1000,
  });
  const clinics = useMemo(() => clinicsQuery.data?.items ?? [], [clinicsQuery.data]);

  // Default to the first clinic once loaded.
  useEffect(() => {
    if (!clinicId && clinics.length > 0) setClinicId(clinics[0].id);
  }, [clinics, clinicId]);

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={CalendarClock}
        title="Schedule"
        description="Configure the scheduler day per clinic and manage the appointment types your staff can book."
      />

      <section className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
        <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">Schedule units</h2>
            <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
              The start and end of the visible scheduler day and the slot interval, set per clinic.
            </p>
          </div>
          <label className="flex flex-col gap-1 text-[12px] text-[var(--color-muted-foreground)]">
            Clinic
            <select
              value={clinicId}
              onChange={(e) => setClinicId(e.target.value)}
              className={cn(selectClass, "min-w-56")}
              aria-label="Clinic"
              disabled={clinicsQuery.isLoading || clinics.length === 0}
            >
              {clinics.length === 0 ? (
                <option value="">No active clinics</option>
              ) : (
                clinics.map((c: ClinicDto) => (
                  <option key={c.id} value={c.id}>
                    {c.name}
                  </option>
                ))
              )}
            </select>
          </label>
        </div>

        {clinicId ? (
          <ScheduleUnitsForm clinicId={clinicId} />
        ) : (
          <p className="text-[13px] text-[var(--color-muted-foreground)]">
            {clinicsQuery.isLoading ? "Loading clinics…" : "Add a clinic first to configure its schedule."}
          </p>
        )}
      </section>

      <AppointmentTypesSection />
    </div>
  );
}

function ScheduleUnitsForm({ clinicId }: { clinicId: string }) {
  const queryClient = useQueryClient();
  const query = useQuery({
    queryKey: [...SCHEDULE_KEY, clinicId],
    queryFn: () => getScheduleConfig(clinicId),
    enabled: !!clinicId,
  });
  const cfg = query.data;

  const [form, setForm] = useState<{ start: string; end: string; interval: number } | null>(null);
  const seededClinic = useRef<string | null>(null);

  // Seed once per clinic so a background refetch can't clobber in-progress edits.
  useEffect(() => {
    if (cfg && seededClinic.current !== cfg.clinicId) {
      setForm({
        start: toTimeInput(cfg.startTime),
        end: toTimeInput(cfg.endTime),
        interval: cfg.intervalMinutes,
      });
      seededClinic.current = cfg.clinicId;
    }
  }, [cfg]);

  const save = useMutation({
    mutationFn: () => {
      if (!form) throw new Error("not ready");
      return upsertScheduleConfig({
        clinicId,
        startTime: form.start,
        endTime: form.end,
        intervalMinutes: form.interval,
      });
    },
    onSuccess: () => {
      toast.success("Schedule units saved");
      queryClient.invalidateQueries({ queryKey: [...SCHEDULE_KEY, clinicId] });
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!form) return;
    if (form.end <= form.start) {
      toast.error("End time must be after start time");
      return;
    }
    save.mutate();
  };

  if (query.isLoading || !form) {
    return <p className="text-[13px] text-[var(--color-muted-foreground)]">Loading…</p>;
  }

  return (
    <form onSubmit={onSubmit} className="grid gap-5 sm:grid-cols-3">
      <Field id="sched-start" label="Day starts" required>
        <Input
          id="sched-start"
          type="time"
          value={form.start}
          onChange={(e) => setForm((f) => (f ? { ...f, start: e.target.value } : f))}
          required
        />
      </Field>
      <Field id="sched-end" label="Day ends" required>
        <Input
          id="sched-end"
          type="time"
          value={form.end}
          onChange={(e) => setForm((f) => (f ? { ...f, end: e.target.value } : f))}
          required
        />
      </Field>
      <Field id="sched-interval" label="Slot interval" hint="Minutes per scheduler row.">
        <select
          id="sched-interval"
          value={form.interval}
          onChange={(e) => setForm((f) => (f ? { ...f, interval: Number(e.target.value) } : f))}
          className={cn(selectClass, "w-full")}
        >
          {INTERVAL_OPTIONS.map((m) => (
            <option key={m} value={m}>
              {m} minutes
            </option>
          ))}
        </select>
      </Field>
      <div className="flex items-center justify-end sm:col-span-3">
        <Button type="submit" disabled={save.isPending}>
          {save.isPending ? "Saving…" : "Save schedule units"}
        </Button>
      </div>
    </form>
  );
}

type TypeEditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; type: AppointmentTypeDto }
  | { mode: "delete"; type: AppointmentTypeDto };

function AppointmentTypesSection() {
  const [showInactive, setShowInactive] = useState(false);
  const [editor, setEditor] = useState<TypeEditorState>({ mode: "closed" });

  const query = useQuery({
    queryKey: [...TYPES_KEY, { showInactive }],
    queryFn: () => listAppointmentTypes(showInactive ? undefined : true),
    staleTime: 60 * 1000,
  });
  const items = query.data ?? [];

  return (
    <section className="rounded-xl border border-[var(--color-border)] p-4 sm:p-5">
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-[14px] font-semibold text-[var(--color-foreground)]">Appointment types</h2>
          <p className="mt-0.5 text-[12px] text-[var(--color-muted-foreground)]">
            The bookable visit types, their default length, and calendar colour.
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <label
            htmlFor="show-inactive-types"
            className="flex cursor-pointer items-center gap-2 text-[12px] text-[var(--color-muted-foreground)]"
          >
            <Switch
              id="show-inactive-types"
              checked={showInactive}
              onCheckedChange={setShowInactive}
              aria-label="Show inactive appointment types"
            />
            Show inactive
          </label>
          <Button
            onClick={() => setEditor({ mode: "create" })}
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
          >
            <Plus className="size-4" />
            New type
          </Button>
        </div>
      </div>

      {query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns="grid-cols-[1fr_120px_90px_64px]" />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={Tag}
          title="No appointment types yet"
          body="Add a visit type so staff can book appointments of that kind."
          action={
            <Button onClick={() => setEditor({ mode: "create" })} className="h-9 rounded-lg px-4 text-[13px]">
              <Plus className="mr-1.5 size-4" />
              Add type
            </Button>
          }
        />
      ) : (
        <EntityListCard>
          <EntityListHeader className="grid-cols-[1fr_120px_90px_64px]">
            <span>Type</span>
            <span>Duration</span>
            <span>Status</span>
            <span />
          </EntityListHeader>
          {items.map((t, i) => (
            <EntityListRow key={t.id} className="grid-cols-[1fr_120px_90px_64px]" isLast={i === items.length - 1}>
              <div className="flex min-w-0 items-center gap-3">
                <ColorSwatch color={t.color} />
                <span className="truncate text-[14px] font-medium text-[var(--color-foreground)]">{t.name}</span>
              </div>
              <div className="flex items-center gap-1.5 text-[12px] text-[var(--color-muted-foreground)]">
                <Clock className="size-3.5 shrink-0" />
                {t.defaultDurationMinutes} min
              </div>
              <div className="flex items-center">
                <EntityStatusBadge tone={t.isActive ? "success" : "default"}>
                  {t.isActive ? "Active" : "Inactive"}
                </EntityStatusBadge>
              </div>
              <div className="flex items-center justify-end gap-1">
                <button
                  type="button"
                  aria-label={`Edit ${t.name}`}
                  onClick={() => setEditor({ mode: "edit", type: t })}
                  className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-foreground)] group-hover:opacity-100"
                >
                  <Pencil className="size-3.5" />
                </button>
                <button
                  type="button"
                  aria-label={`Delete ${t.name}`}
                  onClick={() => setEditor({ mode: "delete", type: t })}
                  className="grid size-7 cursor-pointer place-items-center rounded-md text-[var(--color-muted-foreground)] opacity-0 transition-all hover:bg-[var(--color-muted)] hover:text-[var(--color-destructive)] group-hover:opacity-100"
                >
                  <Trash2 className="size-3.5" />
                </button>
              </div>
            </EntityListRow>
          ))}
        </EntityListCard>
      )}

      {query.isError && (
        <div
          role="alert"
          className="mt-3 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <AppointmentTypeEditorDialog state={editor} onClose={() => setEditor({ mode: "closed" })} />
    </section>
  );
}

function ColorSwatch({ color }: { color?: string | null }) {
  return (
    <span
      aria-hidden
      className="size-6 shrink-0 rounded-md border border-[var(--color-border)]"
      style={{ backgroundColor: color || "transparent" }}
    />
  );
}

function AppointmentTypeEditorDialog({ state, onClose }: { state: TypeEditorState; onClose: () => void }) {
  const isEdit = state.mode === "edit";
  const isOpen = state.mode === "create" || isEdit;
  const type = state.mode === "edit" ? state.type : undefined;
  const queryClient = useQueryClient();
  const invalidate = () => queryClient.invalidateQueries({ queryKey: TYPES_KEY });

  const initial = useMemo(
    () => ({
      name: type?.name ?? "",
      color: type?.color ?? "#3366cc",
      duration: type?.defaultDurationMinutes ?? 15,
      displayOrder: type?.displayOrder ?? 0,
      isActive: type?.isActive ?? true,
    }),
    [type],
  );
  const [form, setForm] = useState(initial);
  useEffect(() => {
    if (isOpen) setForm(initial);
  }, [isOpen, initial]);

  const save = useMutation({
    mutationFn: async () => {
      const name = form.name.trim();
      const color = form.color.trim() || null;
      if (isEdit && type) {
        await updateAppointmentType({
          id: type.id,
          name,
          color,
          defaultDurationMinutes: form.duration,
          displayOrder: form.displayOrder,
          isActive: form.isActive,
        });
      } else {
        await createAppointmentType({
          name,
          color,
          defaultDurationMinutes: form.duration,
          displayOrder: form.displayOrder,
        });
      }
    },
    onSuccess: () => {
      toast.success(isEdit ? "Appointment type updated" : "Appointment type created");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Save failed", { description: describe(err) }),
  });

  const del = useMutation({
    mutationFn: () => deleteAppointmentType((state as { type: AppointmentTypeDto }).type.id),
    onSuccess: () => {
      toast.success("Appointment type deleted");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Delete failed", { description: describe(err) }),
  });

  if (state.mode === "delete") {
    return (
      <Dialog open onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="text-[var(--color-destructive)]">Delete appointment type</DialogTitle>
            <DialogDescription>
              This removes{" "}
              <span className="font-medium text-[var(--color-foreground)]">{state.type.name}</span>. Historic
              appointments of this type are kept.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={del.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button variant="destructive" onClick={() => del.mutate()} disabled={del.isPending}>
              {del.isPending ? "Deleting…" : "Delete type"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    );
  }

  const trimmed = form.name.trim();
  return (
    <Dialog open={isOpen} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-md">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            if (trimmed) save.mutate();
          }}
        >
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit appointment type" : "Add appointment type"}</DialogTitle>
          </DialogHeader>
          <DialogBody className="space-y-5">
            <Field id="at-name" label="Name" required>
              <Input
                id="at-name"
                value={form.name}
                onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                placeholder="Wellness"
                autoFocus
                required
                maxLength={128}
              />
            </Field>
            <div className="grid gap-5 sm:grid-cols-2">
              <Field id="at-duration" label="Default duration" hint="Minutes.">
                <Input
                  id="at-duration"
                  type="number"
                  min={1}
                  max={1440}
                  value={form.duration}
                  onChange={(e) => setForm((f) => ({ ...f, duration: Number(e.target.value) || 1 }))}
                />
              </Field>
              <Field id="at-order" label="Display order" hint="Lower sorts first.">
                <Input
                  id="at-order"
                  type="number"
                  min={0}
                  value={form.displayOrder}
                  onChange={(e) => setForm((f) => ({ ...f, displayOrder: Number(e.target.value) || 0 }))}
                />
              </Field>
            </div>
            <Field id="at-color" label="Calendar colour">
              <div className="flex items-center gap-3">
                <input
                  id="at-color"
                  type="color"
                  value={/^#[0-9a-fA-F]{6}$/.test(form.color) ? form.color : "#3366cc"}
                  onChange={(e) => setForm((f) => ({ ...f, color: e.target.value }))}
                  aria-label="Calendar colour"
                  className="h-9 w-12 cursor-pointer rounded-lg border border-[var(--color-input)] bg-transparent p-1"
                />
                <Input
                  value={form.color}
                  onChange={(e) => setForm((f) => ({ ...f, color: e.target.value }))}
                  placeholder="#3366cc"
                  maxLength={32}
                  aria-label="Colour hex"
                />
                <Palette className="size-4 shrink-0 text-[var(--color-muted-foreground)]" />
              </div>
            </Field>
            {isEdit && (
              <div className="flex items-center justify-between rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <div>
                  <p className="text-[13px] font-medium text-[var(--color-foreground)]">Active</p>
                  <p className="text-[12px] text-[var(--color-muted-foreground)]">
                    Inactive types are hidden when booking.
                  </p>
                </div>
                <Switch
                  checked={form.isActive}
                  onCheckedChange={(v) => setForm((f) => ({ ...f, isActive: v }))}
                  aria-label="Appointment type active"
                />
              </div>
            )}
          </DialogBody>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={save.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={save.isPending || !trimmed}>
              {save.isPending ? "Saving…" : isEdit ? "Save changes" : "Add type"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
