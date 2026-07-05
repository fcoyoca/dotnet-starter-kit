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

// ─── Code Sources (global lookup CRUD) ─────────────────────────────────

export type CodeSourceDto = { id: number; name: string; isActive: boolean };

export function listCodeSources(isActive?: boolean): Promise<CodeSourceDto[]> {
  const qs = isActive === undefined ? "" : `?isActive=${isActive}`;
  return apiFetch<CodeSourceDto[]>(`/api/v1/administration/code-sources${qs}`);
}

export async function createCodeSource(name: string): Promise<number> {
  return apiFetch<number>("/api/v1/administration/code-sources", {
    method: "POST",
    body: JSON.stringify({ name }),
  });
}

export async function updateCodeSource(input: { id: number; name: string; isActive: boolean }): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/code-sources/${input.id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deleteCodeSource(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/code-sources/${id}`, { method: "DELETE" });
}

export function useCodeSourceOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.codeSources"],
    queryFn: () => fetchLookup("code-sources"),
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
  timeZoneId: string;
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
  timeZoneId?: string | null;
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
      timeZoneId: input.timeZoneId ?? null,
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
      timeZoneId: input.timeZoneId ?? null,
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

export function useDepartmentOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.departmentOptions"],
    queryFn: () => listDepartments({ isActive: true, pageSize: 200, sortBy: "name", sortDir: "asc" }),
    staleTime: 10 * 60 * 1000,
  });
  return data ? data.items.map((d) => ({ value: d.id, label: d.name })) : undefined;
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
  signatureImagePath?: string | null;
  signatureImageUrl?: string | null;
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

/** Upload/replace a provider's signature image. Returns the stored image's public URL. */
export async function setProviderSignature(providerId: string, imageBase64: string): Promise<string> {
  return apiFetch<string>(`/api/v1/administration/providers/${encodeURIComponent(providerId)}/signature`, {
    method: "PUT",
    body: JSON.stringify({ providerId, imageBase64 }),
  });
}

/** Remove a provider's signature image (idempotent). */
export async function clearProviderSignature(providerId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/providers/${encodeURIComponent(providerId)}/signature`, {
    method: "DELETE",
  });
}

/** Active providers as combobox options — for the report doctor picker. */
export function useProviderOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.providerOptions"],
    queryFn: () => listProviders({ isActive: true, pageSize: 200, sortBy: "lastName", sortDir: "asc" }),
    staleTime: 5 * 60 * 1000,
  });
  return data
    ? data.items.map((p) => ({
        value: p.id,
        label: [p.prefix, p.firstName, p.lastName, p.suffix].filter(Boolean).join(" "),
      }))
    : undefined;
}

// ─── Insurance Types (tenant-scoped CRUD) ──────────────────────────────

export type InsuranceTypeDto = {
  id: string;
  name: string;
  isActive: boolean;
  procedureCategoryId?: string | null;
  procedureCategoryName?: string | null;
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

export type CreateInsuranceTypeInput = { name: string; procedureCategoryId?: string | null };
export type UpdateInsuranceTypeInput = {
  insuranceTypeId: string;
  name: string;
  isActive: boolean;
  procedureCategoryId?: string | null;
};

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
    body: JSON.stringify({ name: input.name, procedureCategoryId: input.procedureCategoryId ?? null }),
  });
}

export async function updateInsuranceType(input: UpdateInsuranceTypeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/insurance-types/${encodeURIComponent(input.insuranceTypeId)}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.insuranceTypeId,
      name: input.name,
      isActive: input.isActive,
      procedureCategoryId: input.procedureCategoryId ?? null,
    }),
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

// ─── Insurance Type ↔ Procedure Code associations (with price) ─────────

export type InsuranceTypeProcedureDto = {
  id: string;
  insuranceTypeId: string;
  procedureCodeId: string;
  procedureCode: string;
  procedureName?: string | null;
  procedureCategoryName?: string | null;
  price: number;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type InsuranceTypeProcedureItem = { procedureCodeId: string; price: number };

export function listInsuranceTypeProcedures(insuranceTypeId: string): Promise<InsuranceTypeProcedureDto[]> {
  return apiFetch<InsuranceTypeProcedureDto[]>(
    `/api/v1/administration/insurance-types/${encodeURIComponent(insuranceTypeId)}/procedures`,
  );
}

/** Replace the full set of procedure-code price associations for an insurance type (legacy batch save). */
export async function setInsuranceTypeProcedures(
  insuranceTypeId: string,
  items: InsuranceTypeProcedureItem[],
): Promise<void> {
  await apiFetch<void>(
    `/api/v1/administration/insurance-types/${encodeURIComponent(insuranceTypeId)}/procedures`,
    {
      method: "PUT",
      body: JSON.stringify({ insuranceTypeId, items }),
    },
  );
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

// ─── Diagnostic Categories (tenant-scoped CRUD) ────────────────────────

export type DiagnosticCategoryDto = {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListDiagnosticCategoriesParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateDiagnosticCategoryInput = { name: string };
export type UpdateDiagnosticCategoryInput = { categoryId: string; name: string; isActive: boolean };

export function listDiagnosticCategories(
  params: ListDiagnosticCategoriesParams = {},
): Promise<PagedResponse<DiagnosticCategoryDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<DiagnosticCategoryDto>>(
    `/api/v1/administration/diagnostic-categories?${query.toString()}`,
  );
}

export async function createDiagnosticCategory(input: CreateDiagnosticCategoryInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/diagnostic-categories", {
    method: "POST",
    body: JSON.stringify({ name: input.name }),
  });
}

export async function updateDiagnosticCategory(input: UpdateDiagnosticCategoryInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/diagnostic-categories/${encodeURIComponent(input.categoryId)}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.categoryId, name: input.name, isActive: input.isActive }),
  });
}

export async function deleteDiagnosticCategory(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/diagnostic-categories/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Incident Types (tenant-scoped CRUD) ───────────────────────────────

export type IncidentTypeDto = {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListIncidentTypesParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateIncidentTypeInput = { name: string };
export type UpdateIncidentTypeInput = { typeId: string; name: string; isActive: boolean };

export function listIncidentTypes(
  params: ListIncidentTypesParams = {},
): Promise<PagedResponse<IncidentTypeDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<IncidentTypeDto>>(
    `/api/v1/administration/incident-types?${query.toString()}`,
  );
}

export async function createIncidentType(input: CreateIncidentTypeInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/incident-types", {
    method: "POST",
    body: JSON.stringify({ name: input.name }),
  });
}

export async function updateIncidentType(input: UpdateIncidentTypeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/incident-types/${encodeURIComponent(input.typeId)}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.typeId, name: input.name, isActive: input.isActive }),
  });
}

export async function deleteIncidentType(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/incident-types/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export function useIncidentTypeOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.incidentTypeOptions"],
    queryFn: () => listIncidentTypes({ isActive: true, pageSize: 200, sortBy: "name", sortDir: "asc" }),
    staleTime: 10 * 60 * 1000,
  });
  return data ? data.items.map((t) => ({ value: t.id, label: t.name })) : undefined;
}

// ─── Patient Document Types (tenant-scoped CRUD) ───────────────────────

export type PatientDocumentTypeDto = {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListPatientDocumentTypesParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreatePatientDocumentTypeInput = { name: string };
export type UpdatePatientDocumentTypeInput = { typeId: string; name: string; isActive: boolean };

export function listPatientDocumentTypes(
  params: ListPatientDocumentTypesParams = {},
): Promise<PagedResponse<PatientDocumentTypeDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<PatientDocumentTypeDto>>(
    `/api/v1/administration/patient-document-types?${query.toString()}`,
  );
}

export async function createPatientDocumentType(input: CreatePatientDocumentTypeInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/patient-document-types", {
    method: "POST",
    body: JSON.stringify({ name: input.name }),
  });
}

export async function updatePatientDocumentType(input: UpdatePatientDocumentTypeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/patient-document-types/${encodeURIComponent(input.typeId)}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.typeId, name: input.name, isActive: input.isActive }),
  });
}

export async function deletePatientDocumentType(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/patient-document-types/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Report templates (tenant-scoped catalog; drives the Macros admin) ──

export type ReportTypeDto = { id: number; name: string; displayOrder: number; isActive: boolean };
export type ReportFieldDto = {
  id: number;
  reportTypeId: number;
  name: string;
  category?: string | null;
  displayOrder: number;
  isActive: boolean;
};

export function listReportTypes(isActive?: boolean): Promise<ReportTypeDto[]> {
  const q = isActive === undefined ? "" : `?isActive=${isActive}`;
  return apiFetch<ReportTypeDto[]>(`/api/v1/administration/report-types${q}`);
}

export function listReportFields(reportTypeId: number, includeInactive = false): Promise<ReportFieldDto[]> {
  const q = new URLSearchParams({ reportTypeId: String(reportTypeId) });
  if (includeInactive) q.set("includeInactive", "true");
  return apiFetch<ReportFieldDto[]>(`/api/v1/administration/report-fields?${q.toString()}`);
}

export type CreateReportTypeInput = { name: string; displayOrder?: number };
export type UpdateReportTypeInput = { id: number; name: string; displayOrder: number; isActive: boolean };

export function createReportType(input: CreateReportTypeInput): Promise<number> {
  return apiFetch<number>("/api/v1/administration/report-types", {
    method: "POST",
    body: JSON.stringify({ name: input.name, displayOrder: input.displayOrder ?? 0 }),
  });
}

export async function updateReportType(input: UpdateReportTypeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/report-types/${input.id}`, {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

export async function deleteReportType(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/report-types/${id}`, { method: "DELETE" });
}

export type CreateReportFieldInput = {
  reportTypeId: number;
  name: string;
  category?: string | null;
  displayOrder?: number;
};
export type UpdateReportFieldInput = {
  id: number;
  name: string;
  category?: string | null;
  displayOrder: number;
  isActive: boolean;
};

export function createReportField(input: CreateReportFieldInput): Promise<number> {
  return apiFetch<number>("/api/v1/administration/report-fields", {
    method: "POST",
    body: JSON.stringify({
      reportTypeId: input.reportTypeId,
      name: input.name,
      category: input.category ?? null,
      displayOrder: input.displayOrder ?? 0,
    }),
  });
}

export async function updateReportField(input: UpdateReportFieldInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/report-fields/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({ ...input, category: input.category ?? null }),
  });
}

export async function deleteReportField(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/report-fields/${id}`, { method: "DELETE" });
}

// ─── Macros (tenant-scoped CRUD, scoped to a report field) ─────────────

export type MacroDto = {
  id: string;
  name: string;
  text?: string | null;
  reportFieldId?: number | null;
  reportFieldName?: string | null;
  reportCategory?: string | null;
  useableByUserId?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListMacrosParams = {
  search?: string;
  isActive?: boolean | null;
  /** Filter to a specific report field. */
  reportFieldId?: number | null;
  /** Filter across every report field sharing this name (case-insensitive). */
  reportFieldName?: string | null;
  /** Filter to the "All (General)" (unassigned) bucket. */
  general?: boolean;
  /** Filter to macros owned by a specific tenant user. */
  useableByUserId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CreateMacroInput = {
  name: string;
  text?: string | null;
  reportFieldId?: number | null;
  useableByUserId?: string | null;
};
export type UpdateMacroInput = {
  macroId: string;
  name: string;
  text?: string | null;
  reportFieldId?: number | null;
  useableByUserId?: string | null;
  isActive: boolean;
};

export function listMacros(params: ListMacrosParams = {}): Promise<PagedResponse<MacroDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.general) query.set("general", "true");
  else if (params.reportFieldId != null) query.set("reportFieldId", String(params.reportFieldId));
  else if (params.reportFieldName) query.set("reportFieldName", params.reportFieldName);
  if (params.useableByUserId) query.set("useableByUserId", params.useableByUserId);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<MacroDto>>(
    `/api/v1/administration/macros?${query.toString()}`,
  );
}

export async function createMacro(input: CreateMacroInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/macros", {
    method: "POST",
    body: JSON.stringify({
      name: input.name,
      text: input.text ?? null,
      reportFieldId: input.reportFieldId ?? null,
      useableByUserId: input.useableByUserId ?? null,
    }),
  });
}

export async function updateMacro(input: UpdateMacroInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/macros/${encodeURIComponent(input.macroId)}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.macroId,
      name: input.name,
      text: input.text ?? null,
      reportFieldId: input.reportFieldId ?? null,
      useableByUserId: input.useableByUserId ?? null,
      isActive: input.isActive,
    }),
  });
}

export async function deleteMacro(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/macros/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Appointment Types (tenant-scoped CRUD; Schedule) ──────────────────

export type AppointmentTypeDto = {
  id: string;
  name: string;
  color?: string | null;
  defaultDurationMinutes: number;
  displayOrder: number;
  isActive: boolean;
};

export function listAppointmentTypes(isActive?: boolean): Promise<AppointmentTypeDto[]> {
  const q = isActive === undefined ? "" : `?isActive=${isActive}`;
  return apiFetch<AppointmentTypeDto[]>(`/api/v1/administration/appointment-types${q}`);
}

export type CreateAppointmentTypeInput = {
  name: string;
  defaultDurationMinutes: number;
  color?: string | null;
  displayOrder?: number;
};
export type UpdateAppointmentTypeInput = {
  id: string;
  name: string;
  defaultDurationMinutes: number;
  color?: string | null;
  displayOrder: number;
  isActive: boolean;
};

export function createAppointmentType(input: CreateAppointmentTypeInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/appointment-types", {
    method: "POST",
    body: JSON.stringify({
      name: input.name,
      defaultDurationMinutes: input.defaultDurationMinutes,
      color: input.color ?? null,
      displayOrder: input.displayOrder ?? 0,
    }),
  });
}

export async function updateAppointmentType(input: UpdateAppointmentTypeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/appointment-types/${encodeURIComponent(input.id)}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.id,
      name: input.name,
      defaultDurationMinutes: input.defaultDurationMinutes,
      color: input.color ?? null,
      displayOrder: input.displayOrder,
      isActive: input.isActive,
    }),
  });
}

export async function deleteAppointmentType(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/appointment-types/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Schedule Config (per-clinic schedule units; Schedule) ─────────────

/** Times are "HH:mm" (the API accepts and returns ISO time; we keep the minute precision the form uses). */
export type ScheduleConfigDto = {
  clinicId: string;
  startTime: string;
  endTime: string;
  intervalMinutes: number;
};

export type UpsertScheduleConfigInput = {
  clinicId: string;
  startTime: string;
  endTime: string;
  intervalMinutes: number;
};

export function getScheduleConfig(clinicId: string): Promise<ScheduleConfigDto> {
  return apiFetch<ScheduleConfigDto>(
    `/api/v1/administration/schedule-config/${encodeURIComponent(clinicId)}`,
  );
}

export async function upsertScheduleConfig(input: UpsertScheduleConfigInput): Promise<ScheduleConfigDto> {
  return apiFetch<ScheduleConfigDto>(
    `/api/v1/administration/schedule-config/${encodeURIComponent(input.clinicId)}`,
    {
      method: "PUT",
      body: JSON.stringify({
        clinicId: input.clinicId,
        startTime: input.startTime,
        endTime: input.endTime,
        intervalMinutes: input.intervalMinutes,
      }),
    },
  );
}

// ─── Email Settings (tenant-scoped singleton) ──────────────────────────

export type EmailSettingsDto = {
  useCustomSmtp: boolean;
  host?: string | null;
  port?: number | null;
  useSsl: boolean;
  username?: string | null;
  hasPassword: boolean;
  fromAddress?: string | null;
  fromName?: string | null;
  replyTo?: string | null;
  footerHtml?: string | null;
  passwordResetSubject?: string | null;
  passwordResetBody?: string | null;
  passwordResetFooter?: string | null;
  updatedAtUtc?: string | null;
};

export type UpdateEmailSettingsInput = {
  useCustomSmtp: boolean;
  host?: string | null;
  port?: number | null;
  useSsl: boolean;
  username?: string | null;
  /** Pass null to keep the stored password unchanged; a value replaces it. */
  password?: string | null;
  fromAddress?: string | null;
  fromName?: string | null;
  replyTo?: string | null;
  footerHtml?: string | null;
  passwordResetSubject?: string | null;
  passwordResetBody?: string | null;
  passwordResetFooter?: string | null;
};

export function getEmailSettings(): Promise<EmailSettingsDto> {
  return apiFetch<EmailSettingsDto>("/api/v1/administration/email-settings");
}

export async function updateEmailSettings(input: UpdateEmailSettingsInput): Promise<void> {
  await apiFetch<void>("/api/v1/administration/email-settings", {
    method: "PUT",
    body: JSON.stringify(input),
  });
}

// ─── Custom Diagnostics (tenant-scoped CRUD) ───────────────────────────

export type CustomDiagnosticDto = {
  id: string;
  code: string;
  description?: string | null;
  longDescription?: string | null;
  isChiropractic: boolean;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListCustomDiagnosticsParams = {
  search?: string;
  isActive?: boolean | null;
  isChiropractic?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type CustomDiagnosticInput = {
  code: string;
  description?: string | null;
  longDescription?: string | null;
  isChiropractic: boolean;
};

export type CreateCustomDiagnosticInput = CustomDiagnosticInput;
export type UpdateCustomDiagnosticInput = CustomDiagnosticInput & { diagnosticId: string; isActive: boolean };

export function listCustomDiagnostics(
  params: ListCustomDiagnosticsParams = {},
): Promise<PagedResponse<CustomDiagnosticDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.isChiropractic !== undefined && params.isChiropractic !== null)
    query.set("isChiropractic", String(params.isChiropractic));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<CustomDiagnosticDto>>(
    `/api/v1/administration/custom-diagnostics?${query.toString()}`,
  );
}

function customDiagnosticBody(input: CustomDiagnosticInput): Record<string, unknown> {
  return {
    code: input.code,
    description: input.description ?? null,
    longDescription: input.longDescription ?? null,
    isChiropractic: input.isChiropractic,
  };
}

export async function createCustomDiagnostic(input: CreateCustomDiagnosticInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/custom-diagnostics", {
    method: "POST",
    body: JSON.stringify(customDiagnosticBody(input)),
  });
}

export async function updateCustomDiagnostic(input: UpdateCustomDiagnosticInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/custom-diagnostics/${encodeURIComponent(input.diagnosticId)}`, {
    method: "PUT",
    body: JSON.stringify({ ...customDiagnosticBody(input), id: input.diagnosticId, isActive: input.isActive }),
  });
}

export async function deleteCustomDiagnostic(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/custom-diagnostics/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Diagnostics (global ICD catalog — "Diagnostic Details") ───────────

export type DiagnosticDto = {
  id: number;
  code: string;
  description?: string | null;
  longDescription?: string | null;
  codeSourceId: number;
  codeSourceName?: string | null;
  isChiropractic: boolean;
  isBillable?: boolean | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListDiagnosticsParams = {
  search?: string;
  codeSourceId?: number | null;
  isActive?: boolean | null;
  isChiropractic?: boolean | null;
  isBillable?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type DiagnosticInput = {
  code: string;
  description?: string | null;
  longDescription?: string | null;
  codeSourceId: number;
  isChiropractic: boolean;
  isBillable?: boolean | null;
};

export type CreateDiagnosticInput = DiagnosticInput;
export type UpdateDiagnosticInput = DiagnosticInput & { diagnosticId: number; isActive: boolean };

export function listDiagnostics(params: ListDiagnosticsParams = {}): Promise<PagedResponse<DiagnosticDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.codeSourceId != null) query.set("codeSourceId", String(params.codeSourceId));
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.isChiropractic !== undefined && params.isChiropractic !== null)
    query.set("isChiropractic", String(params.isChiropractic));
  if (params.isBillable !== undefined && params.isBillable !== null)
    query.set("isBillable", String(params.isBillable));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<DiagnosticDto>>(`/api/v1/administration/diagnostics?${query.toString()}`);
}

function diagnosticBody(input: DiagnosticInput): Record<string, unknown> {
  return {
    code: input.code,
    description: input.description ?? null,
    longDescription: input.longDescription ?? null,
    codeSourceId: input.codeSourceId,
    isChiropractic: input.isChiropractic,
    isBillable: input.isBillable ?? null,
  };
}

export async function createDiagnostic(input: CreateDiagnosticInput): Promise<number> {
  return apiFetch<number>("/api/v1/administration/diagnostics", {
    method: "POST",
    body: JSON.stringify(diagnosticBody(input)),
  });
}

export async function updateDiagnostic(input: UpdateDiagnosticInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/diagnostics/${input.diagnosticId}`, {
    method: "PUT",
    body: JSON.stringify({ ...diagnosticBody(input), id: input.diagnosticId, isActive: input.isActive }),
  });
}

export async function deleteDiagnostic(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/diagnostics/${id}`, { method: "DELETE" });
}

// ─── Procedure Categories (tenant-scoped CRUD) ─────────────────────────

export type ProcedureCategoryDto = {
  id: string;
  name: string;
  description?: string | null;
  isImaging: boolean;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListProcedureCategoriesParams = {
  search?: string;
  isActive?: boolean | null;
  isImaging?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type ProcedureCategoryInput = {
  name: string;
  description?: string | null;
  isImaging: boolean;
};

export type CreateProcedureCategoryInput = ProcedureCategoryInput;
export type UpdateProcedureCategoryInput = ProcedureCategoryInput & { categoryId: string; isActive: boolean };

export function listProcedureCategories(
  params: ListProcedureCategoriesParams = {},
): Promise<PagedResponse<ProcedureCategoryDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.isImaging !== undefined && params.isImaging !== null)
    query.set("isImaging", String(params.isImaging));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<ProcedureCategoryDto>>(
    `/api/v1/administration/procedure-categories?${query.toString()}`,
  );
}

function procedureCategoryBody(input: ProcedureCategoryInput): Record<string, unknown> {
  return {
    name: input.name,
    description: input.description ?? null,
    isImaging: input.isImaging,
  };
}

export async function createProcedureCategory(input: CreateProcedureCategoryInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/procedure-categories", {
    method: "POST",
    body: JSON.stringify(procedureCategoryBody(input)),
  });
}

export async function updateProcedureCategory(input: UpdateProcedureCategoryInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/procedure-categories/${encodeURIComponent(input.categoryId)}`, {
    method: "PUT",
    body: JSON.stringify({ ...procedureCategoryBody(input), id: input.categoryId, isActive: input.isActive }),
  });
}

export async function deleteProcedureCategory(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/procedure-categories/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

/** Active procedure categories as combobox options — for the code's category picker. */
export function useProcedureCategoryOptions(): ComboboxOption[] | undefined {
  const { data } = useQuery({
    queryKey: ["administration.procedureCategoryOptions"],
    queryFn: () => listProcedureCategories({ isActive: true, pageSize: 200, sortBy: "name", sortDir: "asc" }),
    staleTime: 5 * 60 * 1000,
  });
  return data ? data.items.map((c) => ({ value: c.id, label: c.name })) : undefined;
}

// ─── Procedure Codes (tenant-scoped CRUD) ──────────────────────────────

export type ProcedureCodeDto = {
  id: string;
  code: string;
  name?: string | null;
  description?: string | null;
  procedureCategoryId: string;
  procedureCategoryName?: string | null;
  codeSourceId?: number | null;
  codeSourceName?: string | null;
  macroText?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListProcedureCodesParams = {
  search?: string;
  isActive?: boolean | null;
  procedureCategoryId?: string | null;
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDir?: "asc" | "desc";
};

export type ProcedureCodeInput = {
  code: string;
  procedureCategoryId: string;
  name?: string | null;
  description?: string | null;
  codeSourceId?: number | null;
  macroText?: string | null;
};

export type CreateProcedureCodeInput = ProcedureCodeInput;
export type UpdateProcedureCodeInput = ProcedureCodeInput & { codeId: string; isActive: boolean };

export function listProcedureCodes(
  params: ListProcedureCodesParams = {},
): Promise<PagedResponse<ProcedureCodeDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  if (params.procedureCategoryId) query.set("procedureCategoryId", params.procedureCategoryId);
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDir) query.set("sortDir", params.sortDir);
  return apiFetch<PagedResponse<ProcedureCodeDto>>(
    `/api/v1/administration/procedure-codes?${query.toString()}`,
  );
}

function procedureCodeBody(input: ProcedureCodeInput): Record<string, unknown> {
  return {
    code: input.code,
    procedureCategoryId: input.procedureCategoryId,
    name: input.name ?? null,
    description: input.description ?? null,
    codeSourceId: input.codeSourceId ?? null,
    macroText: input.macroText ?? null,
  };
}

export async function createProcedureCode(input: CreateProcedureCodeInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/procedure-codes", {
    method: "POST",
    body: JSON.stringify(procedureCodeBody(input)),
  });
}

export async function updateProcedureCode(input: UpdateProcedureCodeInput): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/procedure-codes/${encodeURIComponent(input.codeId)}`, {
    method: "PUT",
    body: JSON.stringify({ ...procedureCodeBody(input), id: input.codeId, isActive: input.isActive }),
  });
}

export async function deleteProcedureCode(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/procedure-codes/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

// ─── Drugs (global catalog — allergy/medication pickers) ───────────────

export type DrugDto = {
  id: number;
  name: string;
  rxAui?: string | null;
  rxCui?: string | null;
  tty?: string | null;
  sab?: string | null;
  code?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListDrugsParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
};

export function listDrugs(params: ListDrugsParams = {}): Promise<PagedResponse<DrugDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  return apiFetch<PagedResponse<DrugDto>>(`/api/v1/administration/drugs?${query.toString()}`);
}

// ─── Allergy Reactions (global SNOMED reaction lookup) ──────────────────

export type AllergyReactionDto = {
  id: number;
  term: string;
  snomedCode?: string | null;
  isActive: boolean;
};

export function listAllergyReactions(params: { isActive?: boolean } = {}): Promise<AllergyReactionDto[]> {
  const query = new URLSearchParams();
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  return apiFetch<AllergyReactionDto[]>(
    `/api/v1/administration/allergy-reactions?${query.toString()}`,
  );
}

// ─── Medication Dose Units (global lookup) ──────────────────────────────

export type MedicationDoseUnitDto = {
  id: number;
  name: string;
  isActive: boolean;
};

export function listMedicationDoseUnits(
  params: { isActive?: boolean } = {},
): Promise<MedicationDoseUnitDto[]> {
  const query = new URLSearchParams();
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  return apiFetch<MedicationDoseUnitDto[]>(
    `/api/v1/administration/medication-dose-units?${query.toString()}`,
  );
}

// ─── Drugs CRUD + RxNav import ──────────────────────────────────────────

export type DrugInput = {
  name: string;
  rxAui?: string | null;
  rxCui?: string | null;
  tty?: string | null;
  sab?: string | null;
  code?: string | null;
};

function drugBody(input: DrugInput): Record<string, unknown> {
  return {
    name: input.name,
    rxAui: input.rxAui ?? null,
    rxCui: input.rxCui ?? null,
    tty: input.tty ?? null,
    sab: input.sab ?? null,
    code: input.code ?? null,
  };
}

export async function createDrug(input: DrugInput): Promise<number> {
  return apiFetch<number>("/api/v1/administration/drugs", {
    method: "POST",
    body: JSON.stringify(drugBody(input)),
  });
}

export async function updateDrug(input: DrugInput & { id: number; isActive: boolean }): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/drugs/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.id, ...drugBody(input), isActive: input.isActive }),
  });
}

export async function deleteDrug(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/drugs/${id}`, { method: "DELETE" });
}

export type RxNavDrug = { rxCui: string; name: string; tty?: string | null };

export function searchRxNav(term: string): Promise<RxNavDrug[]> {
  const query = new URLSearchParams({ term });
  return apiFetch<RxNavDrug[]>(`/api/v1/administration/drugs/rxnav?${query.toString()}`);
}

export async function importDrugs(items: RxNavDrug[]): Promise<number> {
  return apiFetch<number>("/api/v1/administration/drugs/import", {
    method: "POST",
    body: JSON.stringify({ items }),
  });
}

// ─── Allergy Reactions CRUD ──────────────────────────────────────────────

export async function createAllergyReaction(input: { term: string; snomedCode?: string | null }): Promise<number> {
  return apiFetch<number>("/api/v1/administration/allergy-reactions", {
    method: "POST",
    body: JSON.stringify({ term: input.term, snomedCode: input.snomedCode ?? null }),
  });
}

export async function updateAllergyReaction(input: {
  id: number;
  term: string;
  snomedCode?: string | null;
  isActive: boolean;
}): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/allergy-reactions/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.id,
      term: input.term,
      snomedCode: input.snomedCode ?? null,
      isActive: input.isActive,
    }),
  });
}

export async function deleteAllergyReaction(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/allergy-reactions/${id}`, { method: "DELETE" });
}

// ─── Medication Dose Units CRUD ──────────────────────────────────────────

export async function createMedicationDoseUnit(input: { name: string }): Promise<number> {
  return apiFetch<number>("/api/v1/administration/medication-dose-units", {
    method: "POST",
    body: JSON.stringify({ name: input.name }),
  });
}

export async function updateMedicationDoseUnit(input: {
  id: number;
  name: string;
  isActive: boolean;
}): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/medication-dose-units/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.id, name: input.name, isActive: input.isActive }),
  });
}

export async function deleteMedicationDoseUnit(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/medication-dose-units/${id}`, { method: "DELETE" });
}
