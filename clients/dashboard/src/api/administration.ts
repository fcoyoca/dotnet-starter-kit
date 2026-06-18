import { useQuery } from "@tanstack/react-query";
import { apiFetch } from "@/lib/api-client";
import type { ComboboxOption } from "@/components/list";

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
