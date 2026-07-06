import { apiFetch, ApiRequestError } from "@/lib/api-client";
import { env } from "@/env";
import { tokenStore } from "@/auth/token-store";
import type { PagedResponse } from "@/api/catalog";

export type PatientDocument = {
  id: string;
  patientId: string;
  documentTypeId?: string | null;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  notes?: string | null;
  uploadedByName?: string | null;
  uploadedAtUtc: string;
  updatedAtUtc?: string | null;
};

/** Legacy BackChart's allowed upload extensions — mirrors the server validator. */
export const ALLOWED_DOCUMENT_EXTENSIONS = [
  ".pdf", ".doc", ".docx", ".png", ".jpg", ".gif", ".wma", ".jpeg", ".pptx", ".ppt", ".dcm", ".xml",
] as const;

export const MAX_DOCUMENT_SIZE_BYTES = 10 * 1024 * 1024;

export type SearchPatientDocumentsParams = {
  patientId: string;
  documentTypeId?: string | null;
  includeDeleted?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientDocuments(
  params: SearchPatientDocumentsParams,
): Promise<PagedResponse<PatientDocument>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.documentTypeId) query.set("documentTypeId", params.documentTypeId);
  if (params.includeDeleted) query.set("includeDeleted", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientDocument>>(
    `/api/v1/patient/documents?${query.toString()}`,
  );
}

export type UploadPatientDocumentInput = {
  patientId: string;
  documentTypeId?: string | null;
  fileName: string;
  contentBase64: string;
  contentType?: string | null;
  notes?: string | null;
};

export async function uploadPatientDocument(input: UploadPatientDocumentInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/documents", {
    method: "POST",
    body: JSON.stringify({
      patientId: input.patientId,
      documentTypeId: input.documentTypeId ?? null,
      fileName: input.fileName,
      contentBase64: input.contentBase64,
      contentType: input.contentType ?? null,
      notes: input.notes ?? null,
    }),
  });
}

export type UpdatePatientDocumentInput = {
  documentId: string;
  documentTypeId?: string | null;
  notes?: string | null;
};

export async function updatePatientDocument(input: UpdatePatientDocumentInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/documents/${encodeURIComponent(input.documentId)}`, {
    method: "PUT",
    body: JSON.stringify({
      documentId: input.documentId,
      documentTypeId: input.documentTypeId ?? null,
      notes: input.notes ?? null,
    }),
  });
}

export async function deletePatientDocument(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/documents/${encodeURIComponent(id)}`, {
    method: "DELETE",
  });
}

/** Reads a picked file as base64 (without the data: prefix) for the JSON upload transport. */
export function fileToBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result ?? "");
      const comma = result.indexOf(",");
      resolve(comma >= 0 ? result.slice(comma + 1) : result);
    };
    reader.onerror = () => reject(reader.error ?? new Error("Could not read file."));
    reader.readAsDataURL(file);
  });
}

/**
 * Stream a document download and save it under its original file name. apiFetch
 * only returns parsed JSON, so we fetch the blob directly while replicating the
 * same auth + tenant headers (mirrors `downloadInvoicePdf` / `exportReportsPdf`).
 */
export async function downloadPatientDocument(id: string, fileName: string): Promise<void> {
  const accessToken = tokenStore.getAccessToken();
  if (!accessToken) {
    throw new ApiRequestError(401, "Not signed in");
  }

  const headers = new Headers({ Authorization: `Bearer ${accessToken}` });
  const tenant = tokenStore.getTenant() ?? env.defaultTenant;
  if (tenant) headers.set("tenant", tenant);

  const response = await fetch(
    `${env.apiBase}/api/v1/patient/documents/${encodeURIComponent(id)}/download`,
    { headers },
  );

  if (!response.ok) {
    throw new ApiRequestError(response.status, `Failed to download document (${response.status})`);
  }

  const blob = await response.blob();
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
}
