import { useMemo, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, FileText, Receipt, Stethoscope, User } from "lucide-react";
import {
  getClaim, markClaimReady, submitClaim, markClaimPaid, markClaimDenied, voidClaim,
  type ClaimStatus,
} from "@/api/claims";
import { getPatientById } from "@/api/patients";
import { getReport } from "@/api/reports";
import { getPatientIncident } from "@/api/incidents";
import {
  listCustomDiagnostics,
  listReportTypes,
  useDepartmentOptions,
  useIncidentTypeOptions,
  useInsuranceTypeOptions,
  useProviderOptions,
} from "@/api/administration";
import { formatDate } from "@/lib/list-helpers";
import { Button } from "@/components/ui/button";
import { ClaimStatusPill } from "@/pages/billing/claims-list";
import { ViewBillDialog } from "@/pages/billing/view-bill-dialog";
import { MiniCard, MiniDxRow, MiniRow } from "@/pages/billing/mini-info";
import type { BillSummaryData } from "@/pages/billing/bill-summary";

export function ClaimDetailPage() {
  const { claimId = "" } = useParams();
  const qc = useQueryClient();
  const [viewBillOpen, setViewBillOpen] = useState(false);
  const claimQuery = useQuery({ queryKey: ["claim", claimId], queryFn: () => getClaim(claimId) });
  const claim = claimQuery.data;

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["claim", claimId] });
    qc.invalidateQueries({ queryKey: ["claims"] });
  };
  const ready = useMutation({ mutationFn: markClaimReady, onSuccess: invalidate });
  const submit = useMutation({ mutationFn: submitClaim, onSuccess: invalidate });
  const paid = useMutation({ mutationFn: markClaimPaid, onSuccess: invalidate });
  const denied = useMutation({ mutationFn: markClaimDenied, onSuccess: invalidate });
  const doVoid = useMutation({ mutationFn: voidClaim, onSuccess: invalidate });

  // Enrich the claim snapshot for the header + mini-cards + View Bill sheet.
  const patientQuery = useQuery({
    queryKey: ["patients", claim?.patientId],
    queryFn: () => getPatientById(claim!.patientId),
    enabled: !!claim?.patientId,
  });
  const reportQuery = useQuery({
    queryKey: ["report", claim?.reportId],
    queryFn: () => getReport(claim!.reportId),
    enabled: !!claim?.reportId,
  });
  const incidentQuery = useQuery({
    queryKey: ["incident", reportQuery.data?.incidentId],
    queryFn: () => getPatientIncident(reportQuery.data!.incidentId),
    enabled: !!reportQuery.data?.incidentId,
  });
  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
  });

  // DX codes: resolve the union of the claim lines' pointers and the incident's dx set.
  const dxIds = useMemo(
    () =>
      Array.from(
        new Set([
          ...(claim?.lines ?? []).flatMap((l) => l.diagnosticIds),
          ...(incidentQuery.data?.diagnosticIds ?? []),
        ]),
      ),
    [claim?.lines, incidentQuery.data?.diagnosticIds],
  );
  const dxQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", [...dxIds].sort().join(",")],
    queryFn: () => listCustomDiagnostics({ ids: dxIds, pageSize: 200 }),
    enabled: dxIds.length > 0,
  });
  const dxInfo = useMemo(() => {
    const m = new Map<string, { code: string; description: string | null }>();
    for (const d of dxQuery.data?.items ?? []) m.set(d.id, { code: d.code, description: d.description ?? null });
    return m;
  }, [dxQuery.data]);
  const dxCode = (id: string) => dxInfo.get(id)?.code ?? id.slice(0, 8);

  const insuranceOptions = useInsuranceTypeOptions();
  const incidentTypeOptions = useIncidentTypeOptions();
  const departmentOptions = useDepartmentOptions();
  const providerOptions = useProviderOptions();

  const payerName = insuranceOptions?.find((o) => o.value === claim?.insuranceTypeId)?.label ?? null;
  const reportTypeName =
    reportTypesQuery.data?.find((t) => t.id === reportQuery.data?.reportTypeId)?.name ?? null;
  const incidentTypeName =
    incidentTypeOptions?.find((o) => o.value === incidentQuery.data?.incidentTypeId)?.label ?? null;
  const departmentName =
    departmentOptions?.find((o) => o.value === incidentQuery.data?.departmentId)?.label ?? null;
  const providerName =
    providerOptions?.find((o) => o.value === reportQuery.data?.providerId)?.label ?? null;

  const patientName = useMemo(() => {
    const d = patientQuery.data?.demographics;
    return d ? `${d.lastName}, ${d.firstName}`.trim().replace(/^,|,$/g, "") : "—";
  }, [patientQuery.data]);

  const billData = useMemo<BillSummaryData>(() => {
    const d = patientQuery.data?.demographics;
    return {
      patient: {
        name: patientName,
        code: patientQuery.data?.patientCode,
        dob: d?.dateOfBirth,
        phone: patientQuery.data?.contact.phone,
      },
      insurance: claim?.insuranceTypeId ? { type: payerName } : null,
      encounter: reportQuery.data
        ? {
            reportDate: reportQuery.data.reportDate,
            reportType: reportTypeName,
            provider: providerName,
            dateOfLoss: incidentQuery.data?.dateOfLoss,
            dateOfInitialVisit: incidentQuery.data?.dateOfInitialVisit,
          }
        : null,
      lines: (claim?.lines ?? []).map((l) => ({
        code: l.code,
        description: l.description,
        charge: l.charge,
        dxCodes: l.diagnosticIds.map(dxCode),
      })),
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [patientQuery.data, reportQuery.data, incidentQuery.data, reportTypeName, providerName, claim, payerName, patientName, dxInfo]);

  if (!claim) return <div className="p-6 text-[var(--color-muted-foreground)]">Loading…</div>;

  const s: ClaimStatus = claim.status;
  const busy =
    ready.isPending || submit.isPending || paid.isPending || denied.isPending || doVoid.isPending;

  return (
    <div className="flex flex-col gap-4 pb-24">
      {/* Header */}
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="space-y-1">
          <Link
            to="/billing/claims"
            className="inline-flex items-center gap-1 text-[12px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
          >
            <ArrowLeft className="size-3.5" />
            Claims
          </Link>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-semibold">Claim</h1>
            <ClaimStatusPill status={s} />
          </div>
          <div className="text-[13px] text-[var(--color-muted-foreground)]">
            {patientName}
            {claim.controlNumber ? ` · ${claim.controlNumber}` : ""}
            {" · "}
            <Link className="text-[var(--color-primary)] hover:underline" to={`/patient-charts/${claim.patientId}`}>
              open chart
            </Link>
          </div>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={() => setViewBillOpen(true)}>
          <Receipt className="size-4" />
          View Bill
        </Button>
      </div>

      {/* Mini-info cards: Patient · Incident · Report (read-only snapshot) */}
      <div className="grid gap-3 md:grid-cols-3">
        <MiniCard icon={<User className="size-3.5" />} title="Patient">
          <MiniRow label="Name">{patientName}</MiniRow>
          <MiniRow label="DOB">{formatDate(patientQuery.data?.demographics.dateOfBirth)}</MiniRow>
          <MiniRow label="Code">{patientQuery.data?.patientCode}</MiniRow>
          <MiniRow label="Insurance">{payerName}</MiniRow>
        </MiniCard>

        <MiniCard icon={<Stethoscope className="size-3.5" />} title="Incident">
          <MiniRow label="ACC">{incidentTypeName}</MiniRow>
          <MiniRow label="DOIV">{formatDate(incidentQuery.data?.dateOfInitialVisit)}</MiniRow>
          <MiniRow label="DOL">{formatDate(incidentQuery.data?.dateOfLoss)}</MiniRow>
          <MiniDxRow ids={dxIds} label={(id) => dxInfo.get(id) ?? { code: id.slice(0, 8) + "…" }} />
        </MiniCard>

        <MiniCard
          icon={<FileText className="size-3.5" />}
          title="Report"
          action={
            reportQuery.data ? (
              <ClaimStatusPill status={reportQuery.data.workflowStatus} />
            ) : undefined
          }
        >
          <MiniRow label="Date">{formatDate(reportQuery.data?.reportDate)}</MiniRow>
          <MiniRow label="Type">{reportTypeName}</MiniRow>
          <MiniRow label="Department">{departmentName}</MiniRow>
          <MiniRow label="Provider">{providerName}</MiniRow>
        </MiniCard>
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        {/* Left: procedures + diagnoses */}
        <div className="space-y-4 lg:col-span-2">
          <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
            <h2 className="mb-3 text-[11px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
              Procedures (snapshot)
            </h2>
            <table className="w-full text-[13px]">
              <tbody>
                {claim.lines.map((l) => (
                  <tr key={l.procedureCodeId} className="border-b border-[var(--color-border)] last:border-0 align-top">
                    <td className="py-2">
                      <div className="font-medium">
                        {l.code} · {l.description ?? ""}
                      </div>
                      {l.diagnosticIds.length > 0 && (
                        <div className="mt-0.5 flex flex-wrap gap-x-3 text-[11px] text-[var(--color-muted-foreground)]">
                          <span className="font-semibold">DX:</span>
                          {l.diagnosticIds.map((id) => (
                            <span key={id}>{dxCode(id)}</span>
                          ))}
                        </div>
                      )}
                    </td>
                    <td className="py-2 text-right font-medium">${l.charge.toLocaleString()}</td>
                  </tr>
                ))}
                <tr>
                  <td className="py-2 font-bold">Total</td>
                  <td className="py-2 text-right font-bold text-[var(--color-primary)]">
                    ${claim.totalCharge.toLocaleString()}
                  </td>
                </tr>
              </tbody>
            </table>
          </section>
        </div>

        {/* Right: payer + activity rail */}
        <div className="space-y-4">
          <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
            <h2 className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">Payer</h2>
            <MiniRow label="Insurance">{payerName}</MiniRow>
          </section>

          <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[12px] text-[var(--color-muted-foreground)]">
            <h2 className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">Activity</h2>
            <div>Created {new Date(claim.createdAtUtc).toLocaleString()}</div>
            {claim.submittedAtUtc && <div>Submitted {new Date(claim.submittedAtUtc).toLocaleString()}</div>}
            {claim.resolvedAtUtc && <div>Resolved {new Date(claim.resolvedAtUtc).toLocaleString()}</div>}
          </section>
        </div>
      </div>

      {/* Sticky action bar — buttons enabled per legal transition */}
      <div className="fixed inset-x-0 bottom-0 flex justify-end gap-2 border-t border-[var(--color-border)] bg-[var(--color-background)]/95 p-3 backdrop-blur">
        {s !== "Paid" && s !== "Denied" && s !== "Voided" && (
          <Button type="button" variant="outline" size="sm" disabled={busy} onClick={() => doVoid.mutate({ id: claim.id })}>
            Void
          </Button>
        )}
        {s === "Draft" && (
          <Button type="button" size="sm" disabled={busy} onClick={() => ready.mutate(claim.id)}>
            Mark Ready →
          </Button>
        )}
        {s === "Ready" && (
          <Button type="button" size="sm" disabled={busy} onClick={() => submit.mutate(claim.id)}>
            Submit →
          </Button>
        )}
        {s === "Submitted" && (
          <>
            <Button type="button" variant="outline" size="sm" disabled={busy} onClick={() => denied.mutate(claim.id)}>
              Mark Denied
            </Button>
            <Button
              type="button"
              size="sm"
              disabled={busy}
              onClick={() => paid.mutate(claim.id)}
              className="bg-green-600 text-white hover:bg-green-700"
            >
              Mark Paid
            </Button>
          </>
        )}
      </div>

      <ViewBillDialog open={viewBillOpen} onClose={() => setViewBillOpen(false)} data={billData} title="Claim Bill" />
    </div>
  );
}
