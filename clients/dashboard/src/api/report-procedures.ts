import { apiFetch } from "@/lib/api-client";

export type SuperBillProcedureDto = {
  id: string;
  procedureCodeId: string;
  code: string;
  description?: string | null;
  charge: number;
  displayOrder: number;
  diagnosticIds: string[];
};

export type SuperBillDto = {
  /** null until the report's first Procedures Performed save. */
  id?: string | null;
  reportId: string;
  isBilled: boolean;
  billedDateUtc?: string | null;
  procedures: SuperBillProcedureDto[];
  /** The insurance type this bill was priced under, snapshotted at save. Null for legacy/self-pay. */
  insuranceTypeId?: string | null;
};

export type ReportProcedureInput = {
  procedureCodeId: string;
  code: string;
  description?: string | null;
  charge: number;
  diagnosticIds: string[];
};

export type SetReportProceduresInput = {
  reportId: string;
  procedures: ReportProcedureInput[];
  /** The insurance type the bill was priced under; snapshotted server-side. */
  insuranceTypeId?: string | null;
};

export function getReportProcedures(reportId: string): Promise<SuperBillDto> {
  return apiFetch<SuperBillDto>(
    `/api/v1/patient/reports/${encodeURIComponent(reportId)}/procedures`,
  );
}

export async function setReportProcedures(input: SetReportProceduresInput): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/reports/${encodeURIComponent(input.reportId)}/procedures`,
    {
      method: "PUT",
      body: JSON.stringify({
        reportId: input.reportId,
        procedures: input.procedures,
        insuranceTypeId: input.insuranceTypeId ?? null,
      }),
    },
  );
}
