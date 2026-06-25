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

export type AppointmentInput = {
  clinicId: string;
  providerId: string;
  patientId?: string | null;
  appointmentTypeId?: string | null;
  startUtc: string;
  endUtc: string;
  notes?: string | null;
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

async function lifecycle(id: string, action: "check-in" | "check-out" | "cancel" | "no-show"): Promise<void> {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}/${action}`, {
    method: "POST",
  });
}

export const checkInAppointment = (id: string): Promise<void> => lifecycle(id, "check-in");
export const checkOutAppointment = (id: string): Promise<void> => lifecycle(id, "check-out");
export const cancelAppointment = (id: string): Promise<void> => lifecycle(id, "cancel");
export const noShowAppointment = (id: string): Promise<void> => lifecycle(id, "no-show");

/** Realtime broadcast payload (group `tenant:{tenantId}`, event `AppointmentChanged`). */
export type AppointmentChangedEvent = {
  clinicId: string;
  providerId: string;
  startUtc: string;
  endUtc: string;
  action: "created" | "updated" | "deleted" | "checked-in" | "checked-out" | "cancelled" | "no-show";
};

export const SCHEDULING_PERMISSIONS = {
  view: "Permissions.Scheduling.Appointments.View",
  create: "Permissions.Scheduling.Appointments.Create",
  update: "Permissions.Scheduling.Appointments.Update",
  delete: "Permissions.Scheduling.Appointments.Delete",
} as const;
