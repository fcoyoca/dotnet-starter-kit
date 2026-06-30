import { useEffect, useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { CalendarClock } from "lucide-react";
import { listPatientAppointments, type AppointmentDto } from "@/api/scheduling";
import { useProviderOptions } from "@/api/administration";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

const SCHEDULING_VIEW = "Permissions.Scheduling.Appointments.View";

/** What the chart needs to create the report once an appointment (or manual date) is chosen. */
export type AppointmentSelection = {
  reportDate: string;
  clinicId: string | null;
  providerId: string | null;
  appointmentId: string | null;
};

type Props = {
  patientId: string;
  /** Label of the report type being created, shown for context. */
  reportTypeName: string;
  open: boolean;
  /** True while the report is being created (disables the actions). */
  creating?: boolean;
  onCancel(): void;
  onConfirm(selection: AppointmentSelection): void;
};

function todayDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function formatApptTime(iso: string): string {
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

export function SelectAppointmentDialog({
  patientId,
  reportTypeName,
  open,
  creating = false,
  onCancel,
  onConfirm,
}: Props) {
  const { user } = useAuth();
  const canViewScheduling = user?.permissions?.includes(SCHEDULING_VIEW) ?? false;

  const providerOptions = useProviderOptions();
  const providerLabel = (id: string): string =>
    providerOptions?.find((o) => o.value === id)?.label ?? "—";

  const [manual, setManual] = useState(false);
  const [manualDate, setManualDate] = useState(todayDate());
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Reset to a clean state every time the dialog re-opens.
  useEffect(() => {
    if (open) {
      setManual(false);
      setManualDate(todayDate());
      setSelectedId(null);
    }
  }, [open]);

  const appointmentsQuery = useQuery({
    queryKey: ["patient-appointments", patientId],
    queryFn: () => listPatientAppointments(patientId),
    enabled: open && canViewScheduling,
    placeholderData: keepPreviousData,
  });

  const appointments = useMemo(
    () => appointmentsQuery.data ?? [],
    [appointmentsQuery.data],
  );

  const useAppointment = (appt: AppointmentDto) => {
    onConfirm({
      reportDate: appt.startUtc.slice(0, 10),
      clinicId: appt.clinicId,
      providerId: appt.providerId,
      appointmentId: appt.id,
    });
  };

  const confirmManual = () => {
    onConfirm({ reportDate: manualDate, clinicId: null, providerId: null, appointmentId: null });
  };

  const selected = appointments.find((a) => a.id === selectedId) ?? null;

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onCancel()}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <CalendarClock className="size-4" />
            Select Appointment
          </DialogTitle>
          <p className="text-[13px] text-[var(--color-muted-foreground)]">
            for the new {reportTypeName}
          </p>
        </DialogHeader>

        <DialogBody>
          {manual || !canViewScheduling ? (
            <div className="space-y-2">
              {!canViewScheduling && (
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  You don't have access to the appointment schedule. Enter the report date manually.
                </p>
              )}
              <label htmlFor="appt-manual-date" className="block text-[13px] font-medium">
                Report date
              </label>
              <input
                id="appt-manual-date"
                type="date"
                value={manualDate}
                onChange={(e) => setManualDate(e.target.value)}
                className="h-9 w-full rounded-md border border-[var(--color-border)] bg-[var(--color-background)] px-3 text-[13px]"
              />
            </div>
          ) : appointmentsQuery.isLoading ? (
            <div className="h-24 animate-pulse rounded-lg bg-[var(--color-muted)]" />
          ) : appointments.length === 0 ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              No appointments found for this patient. Use “Manually Enter Date” to set the report date.
            </p>
          ) : (
            <>
              <p className="mb-2 text-[12px] text-[var(--color-muted-foreground)]">
                Double-click an appointment, or select it and press “Use This Appointment”.
              </p>
              <ul className="max-h-64 divide-y divide-[var(--color-border)] overflow-y-auto rounded-lg border border-[var(--color-border)]">
                {appointments.map((a) => (
                  <li key={a.id}>
                    <button
                      type="button"
                      onClick={() => setSelectedId(a.id)}
                      onDoubleClick={() => useAppointment(a)}
                      className={[
                        "flex w-full items-center justify-between gap-3 px-3 py-2.5 text-left transition-colors",
                        a.id === selectedId
                          ? "bg-[var(--color-accent)]"
                          : "hover:bg-[var(--color-accent)]",
                      ].join(" ")}
                    >
                      <div className="min-w-0">
                        <p className="truncate text-[13px] font-medium">{formatApptTime(a.startUtc)}</p>
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {providerLabel(a.providerId)}
                          {a.noShow ? " · No Show" : ""}
                        </p>
                      </div>
                      <span className="shrink-0 text-[12px] text-[var(--color-muted-foreground)]">
                        {a.status}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            </>
          )}
        </DialogBody>

        <DialogFooter>
          {manual || !canViewScheduling ? (
            <>
              {canViewScheduling && (
                <Button variant="outline" disabled={creating} onClick={() => setManual(false)}>
                  Back
                </Button>
              )}
              <Button disabled={creating || !manualDate} onClick={confirmManual}>
                Use This Date
              </Button>
            </>
          ) : (
            <>
              <Button variant="outline" disabled={creating} onClick={() => setManual(true)}>
                Manually Enter Date
              </Button>
              <Button
                disabled={creating || !selected}
                onClick={() => selected && useAppointment(selected)}
              >
                Use This Appointment
              </Button>
            </>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
