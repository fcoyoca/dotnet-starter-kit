import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type ProblemStatus = "Active" | "Resolved" | "Inactive";

export type PatientProblem = {
  id: string;
  patientId: string;
  incidentId?: string | null;
  diagnosticId: number;
  diagnosticCode: string;
  diagnosticDescription?: string | null;
  diagnosisDate?: string | null;
  status: ProblemStatus;
  notes?: string | null;
  isMedicalAlert: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchProblemsParams = {
  patientId: string;
  includeInactive?: boolean;
  includeResolved?: boolean;
  includeDeleted?: boolean;
  medicalAlertsOnly?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientProblems(
  params: SearchProblemsParams,
): Promise<PagedResponse<PatientProblem>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.includeInactive) query.set("includeInactive", "true");
  if (params.includeResolved) query.set("includeResolved", "true");
  if (params.includeDeleted) query.set("includeDeleted", "true");
  if (params.medicalAlertsOnly) query.set("medicalAlertsOnly", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientProblem>>(`/api/v1/patient/problems?${query.toString()}`);
}

export function getProblem(id: string): Promise<PatientProblem> {
  return apiFetch<PatientProblem>(`/api/v1/patient/problems/${encodeURIComponent(id)}`);
}

export type CreateProblemInput = {
  patientId: string;
  diagnosticId: number;
  diagnosticCode: string;
  diagnosticDescription?: string | null;
  diagnosisDate?: string | null;
  status: ProblemStatus;
  notes?: string | null;
  isMedicalAlert: boolean;
  incidentId?: string | null;
};

export async function createProblem(input: CreateProblemInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/problems", {
    method: "POST",
    body: JSON.stringify({
      patientId: input.patientId,
      diagnosticId: input.diagnosticId,
      diagnosticCode: input.diagnosticCode,
      diagnosticDescription: input.diagnosticDescription ?? null,
      diagnosisDate: input.diagnosisDate ?? null,
      status: input.status,
      notes: input.notes ?? null,
      isMedicalAlert: input.isMedicalAlert,
      incidentId: input.incidentId ?? null,
    }),
  });
}

export type UpdateProblemInput = {
  problemId: string;
  diagnosticId: number;
  diagnosticCode: string;
  diagnosticDescription?: string | null;
  diagnosisDate?: string | null;
  status: ProblemStatus;
  notes?: string | null;
  isMedicalAlert: boolean;
};

export async function updateProblem(input: UpdateProblemInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/problems/${encodeURIComponent(input.problemId)}`, {
    method: "PUT",
    body: JSON.stringify({
      problemId: input.problemId,
      diagnosticId: input.diagnosticId,
      diagnosticCode: input.diagnosticCode,
      diagnosticDescription: input.diagnosticDescription ?? null,
      diagnosisDate: input.diagnosisDate ?? null,
      status: input.status,
      notes: input.notes ?? null,
      isMedicalAlert: input.isMedicalAlert,
    }),
  });
}

export async function deleteProblem(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/problems/${encodeURIComponent(id)}`, { method: "DELETE" });
}

/** Replace the set of problems associated with a report (legacy "Associated Problems"). */
export async function setReportProblems(reportId: string, problemIds: string[]): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/reports/${encodeURIComponent(reportId)}/problems`, {
    method: "PUT",
    body: JSON.stringify({ reportId, problemIds }),
  });
}
