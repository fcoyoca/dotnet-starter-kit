import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PatientNote = {
  id: string;
  patientId: string;
  name: string;
  description?: string | null;
  isMedicalAlert: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchNotesParams = {
  patientId: string;
  medicalAlertsOnly?: boolean;
  includeDeleted?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientNotes(params: SearchNotesParams): Promise<PagedResponse<PatientNote>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.medicalAlertsOnly) query.set("medicalAlertsOnly", "true");
  if (params.includeDeleted) query.set("includeDeleted", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientNote>>(`/api/v1/patient/notes?${query.toString()}`);
}

export type CreateNoteInput = {
  patientId: string;
  name: string;
  description?: string | null;
  isMedicalAlert: boolean;
};

export async function createNote(input: CreateNoteInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/notes", {
    method: "POST",
    body: JSON.stringify({
      patientId: input.patientId,
      name: input.name,
      description: input.description ?? null,
      isMedicalAlert: input.isMedicalAlert,
    }),
  });
}

export type UpdateNoteInput = {
  noteId: string;
  name: string;
  description?: string | null;
  isMedicalAlert: boolean;
};

export async function updateNote(input: UpdateNoteInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/notes/${encodeURIComponent(input.noteId)}`, {
    method: "PUT",
    body: JSON.stringify({
      noteId: input.noteId,
      name: input.name,
      description: input.description ?? null,
      isMedicalAlert: input.isMedicalAlert,
    }),
  });
}

export async function deleteNote(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/notes/${encodeURIComponent(id)}`, { method: "DELETE" });
}
