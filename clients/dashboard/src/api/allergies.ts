import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PatientAllergy = {
  id: string;
  patientId: string;
  drugName: string;
  rxAui?: string | null;
  reaction?: string | null;
  comments?: string | null;
  dateNoted: string;
  isActive: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchAllergiesParams = {
  patientId: string;
  includeInactive?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientAllergies(
  params: SearchAllergiesParams,
): Promise<PagedResponse<PatientAllergy>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.includeInactive) query.set("includeInactive", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientAllergy>>(`/api/v1/patient/allergies?${query.toString()}`);
}

export type CreateAllergyInput = {
  patientId: string;
  drugName: string;
  rxAui?: string | null;
  reaction?: string | null;
  comments?: string | null;
  dateNoted: string;
  isActive: boolean;
};

export async function createAllergy(input: CreateAllergyInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/allergies", {
    method: "POST",
    body: JSON.stringify({
      patientId: input.patientId,
      drugName: input.drugName,
      rxAui: input.rxAui ?? null,
      reaction: input.reaction ?? null,
      comments: input.comments ?? null,
      dateNoted: input.dateNoted,
      isActive: input.isActive,
    }),
  });
}

export type UpdateAllergyInput = CreateAllergyInput & { allergyId: string };

export async function updateAllergy(input: UpdateAllergyInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/allergies/${encodeURIComponent(input.allergyId)}`, {
    method: "PUT",
    body: JSON.stringify({
      allergyId: input.allergyId,
      drugName: input.drugName,
      rxAui: input.rxAui ?? null,
      reaction: input.reaction ?? null,
      comments: input.comments ?? null,
      dateNoted: input.dateNoted,
      isActive: input.isActive,
    }),
  });
}

export async function setNoKnownAllergies(patientId: string, value: boolean): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/patients/${encodeURIComponent(patientId)}/no-known-allergies`,
    { method: "PUT", body: JSON.stringify({ value }) },
  );
}
