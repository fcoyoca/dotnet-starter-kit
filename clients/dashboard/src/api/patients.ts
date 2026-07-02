import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

// ─── List / search ─────────────────────────────────────────────────────

export type PatientListItemDto = {
  id: string;
  patientCode: string;
  firstName: string;
  lastName: string;
  middleInitial?: string | null;
  dateOfBirth: string;
  gender: string;
  email?: string | null;
  phone?: string | null;
  isActive: boolean;
  lastVisitDate?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type SearchPatientsParams = {
  search?: string;
  ssnHash?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
  providerId?: string;
  clinicId?: string;
};

export function searchPatients(
  params: SearchPatientsParams = {},
): Promise<PagedResponse<PatientListItemDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.ssnHash) query.set("ssnHash", params.ssnHash);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  if (params.providerId) query.set("providerId", params.providerId);
  if (params.clinicId) query.set("clinicId", params.clinicId);
  return apiFetch<PagedResponse<PatientListItemDto>>(
    `/api/v1/patient/patients?${query.toString()}`,
  );
}

// ─── Detail ────────────────────────────────────────────────────────────

export type PatientDemographicsDto = {
  firstName: string;
  lastName: string;
  middleInitial?: string | null;
  dateOfBirth: string;
  gender: string;
  maritalStatus?: string | null;
  isMinor: boolean;
  raceId?: number | null;
  ethnicityId?: number | null;
  languageId?: number | null;
  smokingStatusId?: number | null;
  smokingStartDate?: string | null;
  smokingEndDate?: string | null;
  medicalAlertNotes?: string | null;
};

export type PatientContactDto = {
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  phone?: string | null;
  phoneExtension?: string | null;
  cellPhone?: string | null;
  email?: string | null;
  preferredContactMethodId?: number | null;
};

/** SSN is masked server-side (e.g. `***-**-1234`) — plaintext is never returned. */
export type PatientPhiDto = {
  ssnMasked?: string | null;
};

export type PatientEmploymentDto = {
  occupation?: string | null;
  employerName?: string | null;
  employerAddress1?: string | null;
  employerAddress2?: string | null;
  employerCity?: string | null;
  employerState?: string | null;
  employerZipCode?: string | null;
  employerPhone?: string | null;
  employerPhoneExtension?: string | null;
};

export type PatientGuardianDto = {
  firstName?: string | null;
  lastName?: string | null;
  middleInitial?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  maritalStatus?: string | null;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  phone?: string | null;
  cellPhone?: string | null;
  employerName?: string | null;
  employerAddress1?: string | null;
  employerAddress2?: string | null;
  employerCity?: string | null;
  employerState?: string | null;
  employerZipCode?: string | null;
};

export type PatientNextOfKinDto = {
  firstName?: string | null;
  lastName?: string | null;
  phone?: string | null;
  relation?: string | null;
  relationRoleCode?: string | null;
};

export type PatientInsuranceDto = {
  insuredFullName?: string | null;
  insuredDateOfBirth?: string | null;
  insuredEmployerName?: string | null;
  referralTypeId?: number | null;
};

export type PatientDetailDto = {
  id: string;
  patientCode: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  demographics: PatientDemographicsDto;
  contact: PatientContactDto;
  // C# `PHI` property name — System.Text.Json's camelCase policy lowercases an
  // all-caps run entirely, so it serializes as "phi", not "pHI".
  phi: PatientPhiDto;
  employment?: PatientEmploymentDto | null;
  guardian?: PatientGuardianDto | null;
  nextOfKin?: PatientNextOfKinDto | null;
  insurance?: PatientInsuranceDto | null;
  hasNoKnownProblems: boolean;
  hasNoKnownMedications: boolean;
  hasNoKnownAllergies: boolean;
  receivesEmailReminders: boolean;
  lastVisitDate?: string | null;
  nextVisitDate?: string | null;
};

export function getPatientById(id: string): Promise<PatientDetailDto> {
  return apiFetch<PatientDetailDto>(`/api/v1/patient/patients/${encodeURIComponent(id)}`);
}

// ─── Create / Update ───────────────────────────────────────────────────
//
// `UpdatePatientCommand` on the backend is a single full-replace PUT — there
// is no per-section PATCH. `PatientFields` enumerates every flat field the
// command accepts; `patient-mappers.ts` merges a `PatientDetailDto` plus a
// section's edits into a full `UpdatePatientInput` before every save.

export type PatientFields = {
  patientCode: string;
  isActive: boolean;
  // Demographics
  firstName: string;
  lastName: string;
  middleInitial?: string | null;
  dateOfBirth: string;
  gender: string;
  maritalStatus?: string | null;
  isMinor: boolean;
  raceId?: number | null;
  ethnicityId?: number | null;
  languageId?: number | null;
  smokingStatusId?: number | null;
  smokingStartDate?: string | null;
  smokingEndDate?: string | null;
  medicalAlertNotes?: string | null;
  // Contact
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zipCode?: string | null;
  phone?: string | null;
  phoneExtension?: string | null;
  cellPhone?: string | null;
  email?: string | null;
  preferredContactMethodId?: number | null;
  // PHI (plaintext on the wire — encrypted server-side; null = don't change on update)
  ssn?: string | null;
  guardianSsn?: string | null;
  // Employment
  occupation?: string | null;
  employerName?: string | null;
  employerAddress1?: string | null;
  employerAddress2?: string | null;
  employerCity?: string | null;
  employerState?: string | null;
  employerZipCode?: string | null;
  employerPhone?: string | null;
  employerPhoneExtension?: string | null;
  // Guardian (required when isMinor = true)
  guardianFirstName?: string | null;
  guardianLastName?: string | null;
  guardianMiddleInitial?: string | null;
  guardianDateOfBirth?: string | null;
  guardianGender?: string | null;
  guardianMaritalStatus?: string | null;
  guardianAddress1?: string | null;
  guardianAddress2?: string | null;
  guardianCity?: string | null;
  guardianState?: string | null;
  guardianZipCode?: string | null;
  guardianPhone?: string | null;
  guardianCellPhone?: string | null;
  guardianEmployerName?: string | null;
  guardianEmployerAddress1?: string | null;
  guardianEmployerAddress2?: string | null;
  guardianEmployerCity?: string | null;
  guardianEmployerState?: string | null;
  guardianEmployerZipCode?: string | null;
  // Next of kin
  nextOfKinFirstName?: string | null;
  nextOfKinLastName?: string | null;
  nextOfKinPhone?: string | null;
  nextOfKinRelation?: string | null;
  nextOfKinRelationRoleCode?: string | null;
  // Insurance
  insuredFullName?: string | null;
  insuredDateOfBirth?: string | null;
  insuredEmployerName?: string | null;
  referralTypeId?: number | null;
  // Flags
  hasNoKnownProblems: boolean;
  hasNoKnownMedications: boolean;
  hasNoKnownAllergies: boolean;
  receivesEmailReminders: boolean;
  lastVisitDate?: string | null;
  nextVisitDate?: string | null;
};

export type CreatePatientInput = Omit<PatientFields, "patientCode"> & { patientCode?: string };
export type UpdatePatientInput = PatientFields & { patientId: string };

export async function createPatient(input: CreatePatientInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/patients", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export async function getNextPatientCodePreview(): Promise<string> {
  const result = await apiFetch<{ preview: string }>(
    "/api/v1/patient/patients/next-code-preview",
  );
  return result.preview;
}

export async function updatePatient(input: UpdatePatientInput): Promise<string> {
  return apiFetch<string>(`/api/v1/patient/patients/${encodeURIComponent(input.patientId)}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deletePatient(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/patients/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export async function restorePatient(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/patients/${encodeURIComponent(id)}/restore`, {
    method: "PUT",
  });
}
