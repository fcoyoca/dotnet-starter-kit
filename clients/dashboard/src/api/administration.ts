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
