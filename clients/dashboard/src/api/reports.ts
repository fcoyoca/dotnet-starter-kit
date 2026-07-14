import { apiFetch, ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";
import type { PagedResponse } from "@/api/catalog";

export type ReportWorkflowStatus = "Draft" | "Signed" | "ReviewRequested" | "Reviewed";

export type ReportVitals = {
  heightInches?: number | null;
  weightLbs?: number | null;
  bmi?: number | null;
  systolic?: number | null;
  diastolic?: number | null;
  pulse?: number | null;
  temperatureF?: number | null;
};

export type ReportFieldValue = { reportFieldId: number; text: string };

export type ReportAddendum = {
  id: string;
  text: string;
  createdByUserId: string;
  createdByName?: string | null;
  createdAtUtc: string;
};

export type PatientReportListItem = {
  id: string;
  incidentId: string;
  patientId: string;
  reportTypeId: number;
  reportDate: string;
  version: number;
  providerId?: string | null;
  clinicId?: string | null;
  isNoShow: boolean;
  workflowStatus: ReportWorkflowStatus;
  isSigned: boolean;
  signedByName?: string | null;
  signedOnUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type PatientReportDetail = {
  id: string;
  incidentId: string;
  patientId: string;
  reportTypeId: number;
  reportDate: string;
  version: number;
  providerId?: string | null;
  clinicId?: string | null;
  appointmentId?: string | null;
  isNoShow: boolean;
  vitals: ReportVitals;
  workflowStatus: ReportWorkflowStatus;
  isSigned: boolean;
  signedByUserId?: string | null;
  signedByName?: string | null;
  signedOnUtc?: string | null;
  signatureImagePath?: string | null;
  signatureImageUrl?: string | null;
  reviewRequestedByUserId?: string | null;
  reviewRequestedOnUtc?: string | null;
  reviewerProviderId?: string | null;
  reviewSignedByUserId?: string | null;
  reviewSignedByName?: string | null;
  reviewSignedOnUtc?: string | null;
  reviewSignatureImagePath?: string | null;
  reviewSignatureImageUrl?: string | null;
  fieldValues: ReportFieldValue[];
  addendums: ReportAddendum[];
  associatedProblemIds: string[];
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type SearchReportsParams = {
  incidentId?: string | null;
  patientId?: string | null;
  search?: string | null;
  includeDeleted?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientReports(
  params: SearchReportsParams = {},
): Promise<PagedResponse<PatientReportListItem>> {
  const query = new URLSearchParams();
  if (params.incidentId) query.set("incidentId", params.incidentId);
  if (params.patientId) query.set("patientId", params.patientId);
  if (params.search) query.set("search", params.search);
  if (params.includeDeleted) query.set("includeDeleted", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 50));
  return apiFetch<PagedResponse<PatientReportListItem>>(
    `/api/v1/patient/reports?${query.toString()}`,
  );
}

export function getReport(id: string): Promise<PatientReportDetail> {
  return apiFetch<PatientReportDetail>(`/api/v1/patient/reports/${encodeURIComponent(id)}`);
}

export type CreateReportInput = {
  incidentId: string;
  patientId: string;
  reportTypeId: number;
  reportDate: string;
  providerId?: string | null;
  clinicId?: string | null;
  appointmentId?: string | null;
  isNoShow: boolean;
};

export async function createReport(input: CreateReportInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/reports", {
    method: "POST",
    body: JSON.stringify({
      incidentId: input.incidentId,
      patientId: input.patientId,
      reportTypeId: input.reportTypeId,
      reportDate: input.reportDate,
      providerId: input.providerId ?? null,
      clinicId: input.clinicId ?? null,
      appointmentId: input.appointmentId ?? null,
      isNoShow: input.isNoShow,
    }),
  });
}

export type UpdateReportInput = {
  reportId: string;
  reportDate: string;
  providerId?: string | null;
  clinicId?: string | null;
  isNoShow: boolean;
  vitals: ReportVitals;
  fieldValues: ReportFieldValue[];
};

export async function updateReport(input: UpdateReportInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/reports/${encodeURIComponent(input.reportId)}`, {
    method: "PUT",
    body: JSON.stringify({
      reportId: input.reportId,
      reportDate: input.reportDate,
      providerId: input.providerId ?? null,
      clinicId: input.clinicId ?? null,
      isNoShow: input.isNoShow,
      vitals: {
        heightInches: input.vitals.heightInches ?? null,
        weightLbs: input.vitals.weightLbs ?? null,
        bmi: input.vitals.bmi ?? null,
        systolic: input.vitals.systolic ?? null,
        diastolic: input.vitals.diastolic ?? null,
        pulse: input.vitals.pulse ?? null,
        temperatureF: input.vitals.temperatureF ?? null,
      },
      fieldValues: input.fieldValues.map((f) => ({
        reportFieldId: f.reportFieldId,
        text: f.text,
      })),
    }),
  });
}

/** Sign a report. No image param — the report snapshots the provider's saved signature. */
export async function signReport(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/reports/${encodeURIComponent(id)}/sign`, {
    method: "PUT",
    body: "{}",
  });
}

export async function addAddendum(input: { reportId: string; text: string }): Promise<string> {
  return apiFetch<string>(
    `/api/v1/patient/reports/${encodeURIComponent(input.reportId)}/addendums`,
    {
      method: "POST",
      body: JSON.stringify({ reportId: input.reportId, text: input.text }),
    },
  );
}

export async function requestReview(input: {
  reportId: string;
  reviewerProviderId: string;
}): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/reports/${encodeURIComponent(input.reportId)}/request-review`,
    {
      method: "PUT",
      body: JSON.stringify({ reportId: input.reportId, reviewerProviderId: input.reviewerProviderId }),
    },
  );
}

/** Review-sign a report. No image param — the report snapshots the reviewer provider's saved signature. */
export async function reviewSign(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/reports/${encodeURIComponent(id)}/review-sign`, {
    method: "PUT",
    body: "{}",
  });
}

export async function deleteReport(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/reports/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

export type ExportedReportPdf = {
  fileName: string;
  /** Lowercase hex SHA-256 of the downloaded bytes (integrity check, legacy "Secure Download" parity). */
  sha256: string;
};

function parseContentDispositionFileName(header: string | null): string | null {
  if (!header) return null;
  const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(header);
  if (utf8?.[1]) return decodeURIComponent(utf8[1]);
  const plain = /filename="?([^";]+)"?/i.exec(header);
  return plain?.[1] ?? null;
}

async function sha256Hex(blob: Blob): Promise<string> {
  const digest = await crypto.subtle.digest("SHA-256", await blob.arrayBuffer());
  return Array.from(new Uint8Array(digest))
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}

/**
 * Export one or more reports as a single (merged) PDF and trigger a browser
 * download. apiFetch only returns parsed JSON, so we fetch the blob directly
 * here while replicating the same auth + tenant headers apiFetch sets
 * (mirrors `downloadInvoicePdf` in billing.ts).
 *
 * `password`, when given, is BackChart's padlock export: the server returns an
 * AES-256 encrypted PDF that opens only with that password. It is sent for this
 * one request and never stored — delivering it to the recipient is on the user.
 */
export async function exportReportsPdf(
  reportIds: string[],
  password?: string,
): Promise<ExportedReportPdf> {
  const accessToken = tokenStore.getAccessToken();
  if (!accessToken) {
    throw new ApiRequestError(401, "Not signed in");
  }

  const headers = new Headers({
    Authorization: `Bearer ${accessToken}`,
    "Content-Type": "application/json",
  });
  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  if (tenant) headers.set("tenant", tenant);

  const response = await fetch(`${env.apiBase}/api/v1/patient/reports/export-pdf`, {
    method: "POST",
    headers,
    body: JSON.stringify({ reportIds, password: password ?? null }),
  });

  if (!response.ok) {
    throw new ApiRequestError(response.status, `Failed to export reports (${response.status})`);
  }

  const blob = await response.blob();
  const fileName =
    parseContentDispositionFileName(response.headers.get("content-disposition")) ??
    (reportIds.length === 1
      ? `report_${reportIds[0].replaceAll("-", "")}.pdf`
      : "reports.pdf");
  // Prefer the server-computed checksum; hash the received bytes ourselves if the
  // header isn't exposed to this origin.
  const sha256 = response.headers.get("x-content-sha256") ?? (await sha256Hex(blob));

  const objectUrl = window.URL.createObjectURL(blob);
  try {
    const anchor = document.createElement("a");
    anchor.href = objectUrl;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
  } finally {
    window.URL.revokeObjectURL(objectUrl);
  }

  return { fileName, sha256 };
}
