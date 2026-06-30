import { apiFetch } from "@/lib/api-client";

// ─── Appointments ──────────────────────────────────────────────────────

export type AppointmentStatus = "Scheduled" | "CheckedIn" | "CheckedOut";

export type AppointmentDto = {
  id: string;
  clinicId: string;
  providerId: string;
  patientId?: string | null;
  appointmentTypeId?: string | null;
  startUtc: string;
  endUtc: string;
  notes?: string | null;
  status: AppointmentStatus;
  cancelled: boolean;
  noShow: boolean;
  isReservation: boolean;
  reservationTitle?: string | null;
  /** Groups materialized occurrences of a recurring reserve-time series. */
  reservationSeriesId?: string | null;
  /** When the patient confirmed; null/absent = unconfirmed. */
  confirmedAtUtc?: string | null;
  /** Replacement appointment id; non-null = this slot was rescheduled away (read-only). */
  rescheduledToAppointmentId?: string | null;
};

export type ListAppointmentsParams = {
  clinicId: string;
  /** ISO-8601 UTC instant (inclusive lower bound of the window). */
  fromUtc: string;
  /** ISO-8601 UTC instant (exclusive upper bound of the window). */
  toUtc: string;
  providerIds?: string[];
};

export function listAppointments(params: ListAppointmentsParams): Promise<AppointmentDto[]> {
  const query = new URLSearchParams();
  query.set("clinicId", params.clinicId);
  query.set("fromUtc", params.fromUtc);
  query.set("toUtc", params.toUtc);
  for (const pid of params.providerIds ?? []) query.append("providerIds", pid);
  return apiFetch<AppointmentDto[]>(`/api/v1/scheduling/appointments?${query.toString()}`);
}

export function getAppointment(id: string): Promise<AppointmentDto> {
  return apiFetch<AppointmentDto>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}`);
}

/** A patient's appointments (newest first), for the report "Select Appointment" picker. */
export function listPatientAppointments(patientId: string): Promise<AppointmentDto[]> {
  return apiFetch<AppointmentDto[]>(
    `/api/v1/scheduling/appointments/by-patient/${encodeURIComponent(patientId)}`,
  );
}

export type AppointmentInput = {
  clinicId: string;
  providerId: string;
  patientId?: string | null;
  appointmentTypeId?: string | null;
  startUtc: string;
  endUtc: string;
  notes?: string | null;
  isReservation?: boolean;
  reservationTitle?: string | null;
};

export function createAppointment(input: AppointmentInput): Promise<string> {
  return apiFetch<string>("/api/v1/scheduling/appointments", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export type UpdateAppointmentInput = AppointmentInput & { id: string };

export async function updateAppointment(input: UpdateAppointmentInput): Promise<void> {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(input.id)}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deleteAppointment(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

async function lifecycle(
  id: string,
  action: "check-in" | "check-out" | "cancel" | "no-show" | "confirm",
): Promise<void> {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}/${action}`, {
    method: "POST",
  });
}

export const checkInAppointment = (id: string): Promise<void> => lifecycle(id, "check-in");
export const checkOutAppointment = (id: string): Promise<void> => lifecycle(id, "check-out");
export const cancelAppointment = (id: string): Promise<void> => lifecycle(id, "cancel");
export const noShowAppointment = (id: string): Promise<void> => lifecycle(id, "no-show");
export const confirmAppointment = (id: string): Promise<void> => lifecycle(id, "confirm");

export type RescheduleAppointmentInput = {
  id: string;
  providerId: string;
  startUtc: string;
  endUtc: string;
};

/** Reschedules an appointment (creates a replacement, links the original). Returns the new appointment id. */
export function rescheduleAppointment(input: RescheduleAppointmentInput): Promise<string> {
  return apiFetch<string>(
    `/api/v1/scheduling/appointments/${encodeURIComponent(input.id)}/reschedule`,
    {
      method: "POST",
      body: JSON.stringify({
        providerId: input.providerId,
        startUtc: input.startUtc,
        endUtc: input.endUtc,
      }),
    },
  );
}

export type ReservationOccurrence = { startUtc: string; endUtc: string };

export type CreateRecurringReservationInput = {
  clinicId: string;
  providerId: string;
  title: string;
  notes?: string | null;
  occurrences: ReservationOccurrence[];
};

/** Creates a recurring reserve-time series (one block per occurrence). Returns the count created. */
export function createRecurringReservation(input: CreateRecurringReservationInput): Promise<number> {
  return apiFetch<number>("/api/v1/scheduling/appointments/reserve-recurring", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

/** Deletes an entire recurring reserve-time series. Returns the count deleted. */
export function deleteReservationSeries(seriesId: string): Promise<number> {
  return apiFetch<number>(
    `/api/v1/scheduling/appointments/series/${encodeURIComponent(seriesId)}`,
    { method: "DELETE" },
  );
}

/** Realtime broadcast payload (group `tenant:{tenantId}`, event `AppointmentChanged`). */
export type AppointmentChangedEvent = {
  clinicId: string;
  providerId: string;
  startUtc: string;
  endUtc: string;
  action:
    | "created"
    | "updated"
    | "deleted"
    | "checked-in"
    | "checked-out"
    | "cancelled"
    | "no-show"
    | "confirmed"
    | "rescheduled";
};

export const SCHEDULING_PERMISSIONS = {
  view: "Permissions.Scheduling.Appointments.View",
  create: "Permissions.Scheduling.Appointments.Create",
  update: "Permissions.Scheduling.Appointments.Update",
  delete: "Permissions.Scheduling.Appointments.Delete",
} as const;
