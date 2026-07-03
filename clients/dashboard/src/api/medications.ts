import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PatientMedication = {
  id: string;
  patientId: string;
  drugName: string;
  rxAui?: string | null;
  rxCode?: string | null;
  ndc?: string | null;
  prescriber?: string | null;
  startDate: string;
  endDate?: string | null;
  doseValue?: number | null;
  doseUnitId?: number | null;
  dosePeriodValue?: number | null;
  dosePeriodUnit?: string | null;
  instructions?: string | null;
  indication?: string | null;
  isActive: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchMedicationsParams = {
  patientId: string;
  includeInactive?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientMedications(
  params: SearchMedicationsParams,
): Promise<PagedResponse<PatientMedication>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.includeInactive) query.set("includeInactive", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientMedication>>(
    `/api/v1/patient/medications?${query.toString()}`,
  );
}

export type MedicationFields = {
  drugName: string;
  rxAui?: string | null;
  rxCode?: string | null;
  ndc?: string | null;
  prescriber?: string | null;
  startDate: string;
  endDate?: string | null;
  doseValue?: number | null;
  doseUnitId?: number | null;
  dosePeriodValue?: number | null;
  dosePeriodUnit?: string | null;
  instructions?: string | null;
  indication?: string | null;
  isActive: boolean;
};

function medicationBody(fields: MedicationFields): Record<string, unknown> {
  return {
    drugName: fields.drugName,
    rxAui: fields.rxAui ?? null,
    rxCode: fields.rxCode ?? null,
    ndc: fields.ndc ?? null,
    prescriber: fields.prescriber ?? null,
    startDate: fields.startDate,
    endDate: fields.endDate ?? null,
    doseValue: fields.doseValue ?? null,
    doseUnitId: fields.doseUnitId ?? null,
    dosePeriodValue: fields.dosePeriodValue ?? null,
    dosePeriodUnit: fields.dosePeriodUnit ?? null,
    instructions: fields.instructions ?? null,
    indication: fields.indication ?? null,
    isActive: fields.isActive,
  };
}

export type CreateMedicationInput = MedicationFields & { patientId: string };

export async function createMedication(input: CreateMedicationInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/medications", {
    method: "POST",
    body: JSON.stringify({ patientId: input.patientId, ...medicationBody(input) }),
  });
}

export type UpdateMedicationInput = MedicationFields & { medicationId: string };

export async function updateMedication(input: UpdateMedicationInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/medications/${encodeURIComponent(input.medicationId)}`, {
    method: "PUT",
    body: JSON.stringify({ medicationId: input.medicationId, ...medicationBody(input) }),
  });
}

export async function setNoKnownMedications(patientId: string, value: boolean): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/patients/${encodeURIComponent(patientId)}/no-known-medications`,
    { method: "PUT", body: JSON.stringify({ value }) },
  );
}

export type MedicationReconciledDate = {
  id: string;
  patientId: string;
  reconciledOn: string;
  createdByName?: string | null;
  createdAtUtc: string;
};

export function getMedicationReconciledDates(
  patientId: string,
): Promise<MedicationReconciledDate[]> {
  const query = new URLSearchParams({ patientId });
  return apiFetch<MedicationReconciledDate[]>(
    `/api/v1/patient/medication-reconciliations?${query.toString()}`,
  );
}

export async function markMedicationsReconciled(patientId: string): Promise<string> {
  return apiFetch<string>("/api/v1/patient/medication-reconciliations", {
    method: "POST",
    body: JSON.stringify({ patientId }),
  });
}

/** Legacy "Info" button — MedlinePlus Connect lookup by RxNorm code. */
export function medlinePlusUrl(rxCode: string): string {
  return (
    "https://connect.medlineplus.gov/application?mainSearchCriteria.v.cs=2.16.840.1.113883.6.88" +
    `&mainSearchCriteria.v.c=${encodeURIComponent(rxCode)}&informationRecipient.languageCode.c=en`
  );
}
