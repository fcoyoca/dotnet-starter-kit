import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type AccidentType = "None" | "Auto" | "WorkersComp" | "Slip" | "Other";
export type IncidentPatientStatus = "Active" | "Inactive" | "Discharged" | "Transferred";

export type PatientIncidentListItemDto = {
  id: string;
  patientId: string;
  incidentTypeId?: string | null;
  departmentId?: string | null;
  dateOfInitialVisit?: string | null;
  dateOfLoss: string;
  isClosed: boolean;
  isTransfer: boolean;
  isAccident: boolean;
  accidentType?: AccidentType | null;
  accidentState?: string | null;
  patientStatus: IncidentPatientStatus;
  diagnosticIds: string[];
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type PatientIncidentDetailDto = PatientIncidentListItemDto & {
  comments?: string | null;
  summaryOfCare?: string | null;
  adherenceToPlan?: number | null;
};

export type SearchIncidentsParams = {
  patientId: string;
  isClosed?: boolean | null;
  includeDeleted?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientIncidents(
  params: SearchIncidentsParams,
): Promise<PagedResponse<PatientIncidentListItemDto>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.isClosed !== undefined && params.isClosed !== null)
    query.set("isClosed", String(params.isClosed));
  if (params.includeDeleted) query.set("includeDeleted", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));
  return apiFetch<PagedResponse<PatientIncidentListItemDto>>(
    `/api/v1/patient/incidents?${query.toString()}`,
  );
}

export function getPatientIncident(id: string): Promise<PatientIncidentDetailDto> {
  return apiFetch<PatientIncidentDetailDto>(`/api/v1/patient/incidents/${encodeURIComponent(id)}`);
}

export type CreateIncidentInput = {
  patientId: string;
  incidentTypeId?: string | null;
  departmentId?: string | null;
  dateOfInitialVisit?: string | null;
  dateOfLoss: string;
  isTransfer: boolean;
  isAccident: boolean;
  accidentType?: AccidentType | null;
  accidentState?: string | null;
  comments?: string | null;
  diagnosticIds?: string[] | null;
};

export async function createIncident(input: CreateIncidentInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/incidents", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export type UpdateIncidentInput = {
  incidentId: string;
  incidentTypeId?: string | null;
  departmentId?: string | null;
  dateOfInitialVisit: string;
  dateOfLoss: string;
  isTransfer: boolean;
  isAccident: boolean;
  accidentType?: AccidentType | null;
  accidentState?: string | null;
  comments?: string | null;
  summaryOfCare?: string | null;
  adherenceToPlan?: number | null;
  patientStatus: IncidentPatientStatus;
  isClosed: boolean;
  diagnosticIds?: string[] | null;
};

export async function updateIncident(input: UpdateIncidentInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/incidents/${encodeURIComponent(input.incidentId)}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function setIncidentDiagnostics(input: { incidentId: string; diagnosticIds: string[] }): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/incidents/${encodeURIComponent(input.incidentId)}/diagnostics`,
    { method: "PUT", body: JSON.stringify({ incidentId: input.incidentId, diagnosticIds: input.diagnosticIds }) },
  );
}

export async function closeIncident(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/incidents/${encodeURIComponent(id)}/close`, {
    method: "PUT",
    body: "{}",
  });
}

export async function deleteIncident(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/incidents/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}
