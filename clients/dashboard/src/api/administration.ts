import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import type { ComboboxOption } from "@/components/list";
import type { PagedResponse } from "@/api/catalog";

type LookupItemDto = {
  id: number;
  name: string;
  isActive: boolean;
  snomedCode?: string | null;
};

function fetchLookup(segment: string): Promise<LookupItemDto[]> {
  return apiFetch<LookupItemDto[]>(`/api/v1/administration/${segment}?isActive=true`);
}

function toOptions(items: LookupItemDto[]): ComboboxOption[] {
  return items.map((item) => ({ value: String(item.id), label: item.name }));
}

export function useRaceOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.races"],
    queryFn: () => fetchLookup("races"),
    staleTime: 10 * 60 * 1000,
  });
  return data ? toOptions(data) : undefined;
}

export function useEthnicityOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.ethnicities"],
    queryFn: () => fetchLookup("ethnicities"),
    staleTime: 10 * 60 * 1000,
  });
  return data ? toOptions(data) : undefined;
}

export function useLanguageOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.languages"],
    queryFn: () => fetchLookup("languages"),
    staleTime: 10 * 60 * 1000,
  });
  return data ? toOptions(data) : undefined;
}

export function useSmokingStatusOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.smokingStatuses"],
    queryFn: () => fetchLookup("smoking-statuses"),
    staleTime: 10 * 60 * 1000,
  });
  return data ? toOptions(data) : undefined;
}

export function useContactMethodOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.contactMethods"],
    queryFn: () => fetchLookup("preferred-contact-methods"),
    staleTime: 10 * 60 * 1000,
  });
  return data ? toOptions(data) : undefined;
}

export function useReferralTypeOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.referralTypes"],
    queryFn: () => fetchLookup("referral-types"),
    staleTime: 10 * 60 * 1000,
  });
  return data ? toOptions(data) : undefined;
}

// ─── Clinics (tenant-scoped CRUD) ──────────────────────────────────────

export type ClinicDto = {
  id: string;
  code: string;
  name: string;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zip: string;
  phone?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListClinicsParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type ClinicInput = {
  code: string;
  name: string;
  address1: string;
  address2?: string | null;
  city: string;
  state: string;
  zip: string;
  phone?: string | null;
};

export type CreateClinicInput = ClinicInput;
export type UpdateClinicInput = ClinicInput & { clinicId: string; isActive: boolean };

export function listClinics(params: ListClinicsParams = {}): Promise<PagedResponse<ClinicDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<ClinicDto>>(`/api/v1/administration/clinics?${query.toString()}`);
}

export function getClinicById(id: string): Promise<ClinicDto> {
  return apiFetch<ClinicDto>(`/api/v1/administration/clinics/${encodeURIComponent(id)}`);
}

export async function createClinic(input: CreateClinicInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/clinics", {
    method: "POST",
    body: JSON.stringify({
      code: input.code,
      name: input.name,
      address1: input.address1,
      address2: input.address2 ?? null,
      city: input.city,
      state: input.state,
      zip: input.zip,
      phone: input.phone ?? null,
    }),
  });
}

export async function updateClinic(input: UpdateClinicInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/clinics/${encodeURIComponent(input.clinicId)}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.clinicId,
      code: input.code,
      name: input.name,
      address1: input.address1,
      address2: input.address2 ?? null,
      city: input.city,
      state: input.state,
      zip: input.zip,
      phone: input.phone ?? null,
      isActive: input.isActive,
    }),
  });
}

export async function deleteClinic(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/clinics/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

/** Active clinics as combobox options — for the Provider primary-clinic picker. */
export function useClinicOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.clinicOptions"],
    queryFn: () => listClinics({ isActive: true, pageSize: 200, sortBy: "name", sortDir: "asc" }),
    staleTime: 5 * 60 * 1000,
  });
  return data ? data.items.map((c) => ({ value: c.id, label: c.name })) : undefined;
}

// ─── Departments (tenant-scoped CRUD) ──────────────────────────────────

export type DepartmentDto = {
  id: string;
  name: string;
  displayOrder: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListDepartmentsParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateDepartmentInput = { name: string; displayOrder: number };
export type UpdateDepartmentInput = CreateDepartmentInput & { departmentId: string; isActive: boolean };

export function listDepartments(params: ListDepartmentsParams = {}): Promise<PagedResponse<DepartmentDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<DepartmentDto>>(`/api/v1/administration/departments?${query.toString()}`);
}

export async function createDepartment(input: CreateDepartmentInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/departments", {
    method: "POST",
    body: JSON.stringify({ name: input.name, displayOrder: input.displayOrder }),
  });
}

export async function updateDepartment(input: UpdateDepartmentInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/departments/${encodeURIComponent(input.departmentId)}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.departmentId,
      name: input.name,
      displayOrder: input.displayOrder,
      isActive: input.isActive,
    }),
  });
}

export async function deleteDepartment(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/departments/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Providers (tenant-scoped CRUD) ────────────────────────────────────

export type ProviderDto = {
  id: string;
  firstName: string;
  lastName: string;
  prefix?: string | null;
  suffix?: string | null;
  specialty?: string | null;
  npi?: string | null;
  kareoExternalId?: string | null;
  primaryClinicId?: string | null;
  primaryClinicName?: string | null;
  userId?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListProvidersParams = {
  search?: string;
  isActive?: boolean | null;
  primaryClinicId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type ProviderInput = {
  firstName: string;
  lastName: string;
  prefix?: string | null;
  suffix?: string | null;
  specialty?: string | null;
  npi?: string | null;
  kareoExternalId?: string | null;
  primaryClinicId?: string | null;
  userId?: string | null;
};

export type CreateProviderInput = ProviderInput;
export type UpdateProviderInput = ProviderInput & { providerId: string; isActive: boolean };

export function listProviders(params: ListProvidersParams = {}): Promise<PagedResponse<ProviderDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.primaryClinicId) query.set("primaryClinicId", params.primaryClinicId);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<ProviderDto>>(`/api/v1/administration/providers?${query.toString()}`);
}

function providerBody(input: ProviderInput): string {
  return JSON.stringify({
    firstName: input.firstName,
    lastName: input.lastName,
    prefix: input.prefix ?? null,
    suffix: input.suffix ?? null,
    specialty: input.specialty ?? null,
    npi: input.npi ?? null,
    kareoExternalId: input.kareoExternalId ?? null,
    primaryClinicId: input.primaryClinicId ?? null,
    userId: input.userId ?? null,
  });
}

export async function createProvider(input: CreateProviderInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/providers", {
    method: "POST",
    body: providerBody(input),
  });
}

export async function updateProvider(input: UpdateProviderInput): Promise<void> {
  const base = JSON.parse(providerBody(input)) as Record<string, unknown>;
  await apiFetch<void>(`/api/v1/administration/providers/${encodeURIComponent(input.providerId)}`, {
    method: "PUT",
    body: JSON.stringify({ ...base, id: input.providerId, isActive: input.isActive }),
  });
}

export async function deleteProvider(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/providers/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Insurance Types (tenant-scoped CRUD) ──────────────────────────────

export type InsuranceTypeDto = {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListInsuranceTypesParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateInsuranceTypeInput = { name: string };
export type UpdateInsuranceTypeInput = { insuranceTypeId: string; name: string; isActive: boolean };

export function listInsuranceTypes(
  params: ListInsuranceTypesParams = {},
): Promise<PagedResponse<InsuranceTypeDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<InsuranceTypeDto>>(
    `/api/v1/administration/insurance-types?${query.toString()}`,
  );
}

export async function createInsuranceType(input: CreateInsuranceTypeInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/insurance-types", {
    method: "POST",
    body: JSON.stringify({ name: input.name }),
  });
}

export async function updateInsuranceType(input: UpdateInsuranceTypeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/insurance-types/${encodeURIComponent(input.insuranceTypeId)}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.insuranceTypeId, name: input.name, isActive: input.isActive }),
  });
}

export async function deleteInsuranceType(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/insurance-types/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

/** Active insurance types as combobox options — for the company's type picker. */
export function useInsuranceTypeOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.insuranceTypeOptions"],
    queryFn: () => listInsuranceTypes({ isActive: true, pageSize: 200, sortBy: "name", sortDir: "asc" }),
    staleTime: 5 * 60 * 1000,
  });
  return data ? data.items.map((t) => ({ value: t.id, label: t.name })) : undefined;
}

// ─── Insurance Companies (tenant-scoped CRUD) ──────────────────────────

export type InsuranceCompanyDto = {
  id: string;
  name: string;
  insuranceTypeId?: string | null;
  insuranceTypeName?: string | null;
  formularyTiers: number;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zip?: string | null;
  phone?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListInsuranceCompaniesParams = {
  search?: string;
  isActive?: boolean | null;
  insuranceTypeId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type InsuranceCompanyInput = {
  name: string;
  insuranceTypeId?: string | null;
  formularyTiers: number;
  address1?: string | null;
  address2?: string | null;
  city?: string | null;
  state?: string | null;
  zip?: string | null;
  phone?: string | null;
};

export type CreateInsuranceCompanyInput = InsuranceCompanyInput;
export type UpdateInsuranceCompanyInput = InsuranceCompanyInput & { companyId: string; isActive: boolean };

export function listInsuranceCompanies(
  params: ListInsuranceCompaniesParams = {},
): Promise<PagedResponse<InsuranceCompanyDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.insuranceTypeId) query.set("insuranceTypeId", params.insuranceTypeId);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<InsuranceCompanyDto>>(
    `/api/v1/administration/insurance-companies?${query.toString()}`,
  );
}

function insuranceCompanyBody(input: InsuranceCompanyInput): Record<string, unknown> {
  return {
    name: input.name,
    insuranceTypeId: input.insuranceTypeId ?? null,
    formularyTiers: input.formularyTiers,
    address1: input.address1 ?? null,
    address2: input.address2 ?? null,
    city: input.city ?? null,
    state: input.state ?? null,
    zip: input.zip ?? null,
    phone: input.phone ?? null,
  };
}

export async function createInsuranceCompany(input: CreateInsuranceCompanyInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/insurance-companies", {
    method: "POST",
    body: JSON.stringify(insuranceCompanyBody(input)),
  });
}

export async function updateInsuranceCompany(input: UpdateInsuranceCompanyInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/insurance-companies/${encodeURIComponent(input.companyId)}`, {
    method: "PUT",
    body: JSON.stringify({ ...insuranceCompanyBody(input), id: input.companyId, isActive: input.isActive }),
  });
}

export async function deleteInsuranceCompany(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/insurance-companies/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}
