import { apiFetch } from "@/lib/api-client";

export type ClaimStatus =
  | "Draft" | "Ready" | "Submitted" | "Paid" | "Denied" | "Voided" | (string & {});

export type ClaimLineDto = {
  procedureCodeId: string;
  code: string;
  description?: string | null;
  charge: number;
  diagnosticIds: string[];
};

export type ClaimListItemDto = {
  id: string;
  superBillId: string;
  reportId: string;
  patientId: string;
  insuranceTypeId?: string | null;
  status: ClaimStatus;
  totalCharge: number;
  lineCount: number;
  createdAtUtc: string;
  submittedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
};

export type ClaimDetailDto = {
  id: string;
  superBillId: string;
  reportId: string;
  patientId: string;
  insuranceTypeId?: string | null;
  status: ClaimStatus;
  totalCharge: number;
  controlNumber?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  submittedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  lines: ClaimLineDto[];
};

export type ClaimsSummaryDto = {
  draft: number;
  ready: number;
  submitted: number;
  paid: number;
  denied: number;
  outstandingCharge: number;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
};

export type ClaimsPageDto = {
  page: PagedResult<ClaimListItemDto>;
  summary: ClaimsSummaryDto;
};

export type ClaimSearchParams = {
  status?: ClaimStatus;
  insuranceTypeId?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
};

export function getClaims(params: ClaimSearchParams = {}) {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.insuranceTypeId) query.set("insuranceTypeId", params.insuranceTypeId);
  if (params.search) query.set("search", params.search);
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<ClaimsPageDto>(`/api/v1/claims${suffix}`);
}

export function getClaim(id: string) {
  return apiFetch<ClaimDetailDto>(`/api/v1/claims/${id}`);
}

const post = (id: string, action: string) =>
  apiFetch<string>(`/api/v1/claims/${id}/${action}`, { method: "POST" });

export const markClaimReady = (id: string) => post(id, "ready");
export const submitClaim = (id: string) => post(id, "submit");
export const markClaimPaid = (id: string) => post(id, "paid");
export const markClaimDenied = (id: string) => post(id, "denied");

export function voidClaim({ id, reason }: { id: string; reason?: string }) {
  return apiFetch<string>(`/api/v1/claims/${id}/void`, {
    method: "POST",
    body: JSON.stringify({ reason: reason ?? null }),
  });
}
