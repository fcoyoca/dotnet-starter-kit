import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";
import type { ComboboxOption } from "@/components/list";

/**
 * Coordination-of-benefits order. Replaces legacy BackChart's free-form 0–9 "Priority Number",
 * which allowed two policies to share a priority. A patient holds at most one *active* policy
 * per priority — the API rejects a collision with a 4xx.
 */
export type InsurancePriority = "Primary" | "Secondary" | "Tertiary" | "Quaternary";

/** The policy holder's relationship to the patient (legacy `pinRelationshipToInsured`). */
export type SubscriberRelationship = "Self" | "Spouse" | "Child" | "Other";

export const INSURANCE_PRIORITY_OPTIONS: ComboboxOption[] = [
  { value: "Primary", label: "Primary" },
  { value: "Secondary", label: "Secondary" },
  { value: "Tertiary", label: "Tertiary" },
  { value: "Quaternary", label: "Quaternary" },
];

export const SUBSCRIBER_RELATIONSHIP_OPTIONS: ComboboxOption[] = [
  { value: "Self", label: "Self" },
  { value: "Spouse", label: "Spouse" },
  { value: "Child", label: "Child" },
  { value: "Other", label: "Other" },
];

export type PatientInsurancePolicy = {
  id: string;
  patientId: string;
  insuranceCompanyId: string;
  insuranceCompanyName?: string | null;
  insuranceTypeId?: string | null;
  insuranceTypeName?: string | null;
  priority: InsurancePriority;
  policyNumber?: string | null;
  groupNumber?: string | null;
  memberId?: string | null;
  coPay?: number | null;
  deductible?: number | null;
  effectiveDate?: string | null;
  expirationDate?: string | null;
  subscriberRelationship: SubscriberRelationship;
  subscriberFirstName?: string | null;
  subscriberLastName?: string | null;
  subscriberDateOfBirth?: string | null;
  subscriberGender?: string | null;
  /** Masked (`***-**-1234`) — the API never returns the SSN in plaintext. */
  subscriberSsnMasked?: string | null;
  subscriberEmployerName?: string | null;
  subscriberAddress1?: string | null;
  subscriberAddress2?: string | null;
  subscriberCity?: string | null;
  subscriberState?: string | null;
  subscriberZipCode?: string | null;
  notes?: string | null;
  isActive: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

const PRIORITY_RANK: Record<InsurancePriority, number> = {
  Primary: 0,
  Secondary: 1,
  Tertiary: 2,
  Quaternary: 3,
};

/**
 * The insurance type charting/billing flows should default to: taken from the patient's
 * highest-priority *active* policy that names an insurance type. Mirrors legacy BackChart,
 * whose superbill picker reads the patient's primary insurance type
 * (`InsurancesTop2.First().PinInsuranceTypeID`) rather than a per-incident field.
 *
 * Priority is ranked explicitly rather than trusting list order. Returns null when no active
 * policy carries an insurance type — callers fall back to their own default.
 */
export function pickPrimaryInsuranceType(
  policies: PatientInsurancePolicy[],
): { id: string; name: string | null } | null {
  const withType = policies.filter((p) => p.isActive && p.insuranceTypeId);
  if (withType.length === 0) return null;
  const primary = withType.reduce((best, p) =>
    PRIORITY_RANK[p.priority] < PRIORITY_RANK[best.priority] ? p : best,
  );
  return { id: primary.insuranceTypeId!, name: primary.insuranceTypeName ?? null };
}

export type SearchInsurancePoliciesParams = {
  patientId: string;
  includeInactive?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientInsurancePolicies(
  params: SearchInsurancePoliciesParams,
): Promise<PagedResponse<PatientInsurancePolicy>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.includeInactive) query.set("includeInactive", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientInsurancePolicy>>(
    `/api/v1/patient/insurance-policies?${query.toString()}`,
  );
}

export type InsurancePolicyInput = {
  insuranceCompanyId: string;
  insuranceTypeId?: string | null;
  priority: InsurancePriority;
  policyNumber?: string | null;
  groupNumber?: string | null;
  memberId?: string | null;
  coPay?: number | null;
  deductible?: number | null;
  effectiveDate?: string | null;
  expirationDate?: string | null;
  subscriberRelationship: SubscriberRelationship;
  subscriberFirstName?: string | null;
  subscriberLastName?: string | null;
  subscriberDateOfBirth?: string | null;
  subscriberGender?: string | null;
  /** Send only when the user types a new one — blank means "leave the stored SSN unchanged". */
  subscriberSsn?: string | null;
  subscriberEmployerName?: string | null;
  subscriberAddress1?: string | null;
  subscriberAddress2?: string | null;
  subscriberCity?: string | null;
  subscriberState?: string | null;
  subscriberZipCode?: string | null;
  notes?: string | null;
  isActive: boolean;
};

export type CreateInsurancePolicyInput = InsurancePolicyInput & { patientId: string };
export type UpdateInsurancePolicyInput = InsurancePolicyInput & { policyId: string };

function policyBody(input: InsurancePolicyInput): Record<string, unknown> {
  return {
    insuranceCompanyId: input.insuranceCompanyId,
    insuranceTypeId: input.insuranceTypeId ?? null,
    priority: input.priority,
    policyNumber: input.policyNumber ?? null,
    groupNumber: input.groupNumber ?? null,
    memberId: input.memberId ?? null,
    coPay: input.coPay ?? null,
    deductible: input.deductible ?? null,
    effectiveDate: input.effectiveDate ?? null,
    expirationDate: input.expirationDate ?? null,
    subscriberRelationship: input.subscriberRelationship,
    subscriberFirstName: input.subscriberFirstName ?? null,
    subscriberLastName: input.subscriberLastName ?? null,
    subscriberDateOfBirth: input.subscriberDateOfBirth ?? null,
    subscriberGender: input.subscriberGender ?? null,
    subscriberSsn: input.subscriberSsn ?? null,
    subscriberEmployerName: input.subscriberEmployerName ?? null,
    subscriberAddress1: input.subscriberAddress1 ?? null,
    subscriberAddress2: input.subscriberAddress2 ?? null,
    subscriberCity: input.subscriberCity ?? null,
    subscriberState: input.subscriberState ?? null,
    subscriberZipCode: input.subscriberZipCode ?? null,
    notes: input.notes ?? null,
    isActive: input.isActive,
  };
}

export async function createInsurancePolicy(input: CreateInsurancePolicyInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/insurance-policies", {
    method: "POST",
    body: JSON.stringify({ ...policyBody(input), patientId: input.patientId }),
  });
}

export async function updateInsurancePolicy(input: UpdateInsurancePolicyInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/insurance-policies/${encodeURIComponent(input.policyId)}`, {
    method: "PUT",
    body: JSON.stringify({ ...policyBody(input), policyId: input.policyId }),
  });
}

export async function deleteInsurancePolicy(policyId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/insurance-policies/${encodeURIComponent(policyId)}`, {
    method: "DELETE",
  });
}
