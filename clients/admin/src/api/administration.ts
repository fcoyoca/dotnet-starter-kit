import { apiFetch } from "@/lib/api-client";

export type LookupItemDto = {
  id: number;
  name: string;
  isActive: boolean;
  snomedCode?: string | null;
};

export type CreateLookupInput = {
  name: string;
  snomedCode?: string | null;
};

export type UpdateLookupInput = {
  id: number;
  name: string;
  isActive: boolean;
  snomedCode?: string | null;
};

function makeClient(segment: string) {
  const BASE = `/api/v1/administration/${segment}`;
  return {
    list: (isActive?: boolean): Promise<LookupItemDto[]> =>
      apiFetch<LookupItemDto[]>(
        isActive !== undefined ? `${BASE}?isActive=${isActive}` : BASE,
      ),
    getById: (id: number): Promise<LookupItemDto> =>
      apiFetch<LookupItemDto>(`${BASE}/${id}`),
    create: (input: CreateLookupInput): Promise<number> =>
      apiFetch<number>(BASE, { method: "POST", body: JSON.stringify(input) }),
    update: (id: number, input: Omit<UpdateLookupInput, "id">): Promise<void> =>
      apiFetch<void>(`${BASE}/${id}`, {
        method: "PUT",
        body: JSON.stringify({ id, ...input }),
      }),
    remove: (id: number): Promise<void> =>
      apiFetch<void>(`${BASE}/${id}`, { method: "DELETE" }),
  };
}

export const racesApi = makeClient("races");
export const ethnicitiesApi = makeClient("ethnicities");
export const languagesApi = makeClient("languages");
export const smokingStatusesApi = makeClient("smoking-statuses");
export const contactMethodsApi = makeClient("preferred-contact-methods");
export const referralTypesApi = makeClient("referral-types");
