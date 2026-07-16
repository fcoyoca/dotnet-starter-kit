import { useMemo, useState } from "react";
import { useQuery, useQueries } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getClaims, type ClaimStatus, type ClaimListItemDto } from "@/api/claims";
import { getPatientById } from "@/api/patients";
import { useInsuranceTypeOptions } from "@/api/administration";

const STATUSES: ClaimStatus[] = ["Draft", "Ready", "Submitted", "Paid", "Denied", "Voided"];

export function ClaimStatusPill({ status }: { status: ClaimStatus }) {
  // Billing accent + per-status colors; frontend-design skill refines the exact palette.
  const cls: Record<string, string> = {
    Draft: "bg-slate-200 text-slate-700",
    Ready: "bg-teal-100 text-teal-800",
    Submitted: "bg-blue-100 text-blue-800",
    Paid: "bg-green-100 text-green-800",
    Denied: "bg-red-100 text-red-800",
    Voided: "bg-slate-100 text-slate-500 line-through",
  };
  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${cls[status] ?? cls.Draft}`}>
      {status}
    </span>
  );
}

export function ClaimsListPage() {
  const [status, setStatus] = useState<ClaimStatus | undefined>(undefined);
  const [page, setPage] = useState(1);

  const claimsQuery = useQuery({
    queryKey: ["claims", { status, page }],
    queryFn: () => getClaims({ status, pageNumber: page, pageSize: 20 }),
  });

  const summary = claimsQuery.data?.summary;
  const items: ClaimListItemDto[] = useMemo(
    () => claimsQuery.data?.page.items ?? [],
    [claimsQuery.data],
  );

  // Payer names: useInsuranceTypeOptions() returns ComboboxOption[] (value=id, label=name) directly.
  const insuranceOptions = useInsuranceTypeOptions();
  const payerName = useMemo(() => {
    const map = new Map((insuranceOptions ?? []).map((o) => [o.value, o.label]));
    return (id?: string | null) => (id ? map.get(id) ?? "—" : "—");
  }, [insuranceOptions]);

  // Patient names: resolve the visible page's ids via getPatientById (TanStack-cached, deduped).
  const patientIds = useMemo(
    () => Array.from(new Set(items.map((c) => c.patientId))),
    [items],
  );
  const patientQueries = useQueries({
    queries: patientIds.map((id) => ({
      queryKey: ["patient", id],
      queryFn: () => getPatientById(id),
      staleTime: 5 * 60_000,
    })),
  });
  const patientName = useMemo(() => {
    const map = new Map<string, string>();
    patientQueries.forEach((q, i) => {
      const p = q.data;
      if (p) {
        map.set(patientIds[i], `${p.demographics.lastName}, ${p.demographics.firstName}`.trim());
      }
    });
    return (id: string) => map.get(id) ?? "—";
  }, [patientQueries, patientIds]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">Claims</h1>
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-4 gap-3">
        <Kpi label="Draft" value={summary?.draft ?? 0} />
        <Kpi label="Ready" value={summary?.ready ?? 0} />
        <Kpi label="Submitted" value={summary?.submitted ?? 0} />
        <Kpi label="Outstanding" value={`$${(summary?.outstandingCharge ?? 0).toLocaleString()}`} accent />
      </div>

      {/* Status filter */}
      <div className="flex gap-2">
        <FilterChip active={!status} onClick={() => { setStatus(undefined); setPage(1); }}>All</FilterChip>
        {STATUSES.map((s) => (
          <FilterChip key={s} active={status === s} onClick={() => { setStatus(s); setPage(1); }}>{s}</FilterChip>
        ))}
      </div>

      {/* Table */}
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full text-sm">
          <thead className="text-left text-xs uppercase text-muted-foreground">
            <tr>
              <th className="p-2">Patient</th><th className="p-2">Payer</th>
              <th className="p-2">CPT</th><th className="p-2">Charge</th><th className="p-2">Status</th>
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id} className="border-t hover:bg-muted/40">
                <td className="p-2">
                  <Link className="font-medium hover:underline" to={`/billing/claims/${c.id}`}>
                    {patientName(c.patientId)}
                  </Link>
                </td>
                <td className="p-2">{payerName(c.insuranceTypeId)}</td>
                <td className="p-2">{c.lineCount}</td>
                <td className="p-2">${c.totalCharge.toLocaleString()}</td>
                <td className="p-2"><ClaimStatusPill status={c.status} /></td>
              </tr>
            ))}
            {items.length === 0 && !claimsQuery.isLoading && (
              <tr><td colSpan={5} className="p-6 text-center text-muted-foreground">No claims yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Kpi({ label, value, accent }: { label: string; value: string | number; accent?: boolean }) {
  return (
    <div className="rounded-lg border p-3">
      <div className={`text-2xl font-bold ${accent ? "text-teal-600" : ""}`}>{value}</div>
      <div className="text-xs text-muted-foreground">{label}</div>
    </div>
  );
}

function FilterChip({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button type="button" onClick={onClick}
      className={`rounded-full border px-3 py-1 text-xs ${active ? "border-teal-500 bg-teal-50 text-teal-800" : ""}`}>
      {children}
    </button>
  );
}
