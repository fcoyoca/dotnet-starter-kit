import { useMemo, useState } from "react";
import { useQuery, useQueries } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getClaims, type ClaimStatus, type ClaimListItemDto } from "@/api/claims";
import { getPatientById } from "@/api/patients";
import { useInsuranceTypeOptions } from "@/api/administration";

const STATUSES: ClaimStatus[] = ["Draft", "Ready", "Submitted", "Paid", "Denied", "Voided"];

export function ClaimStatusPill({ status }: { status: ClaimStatus }) {
  // Per-status semantic colors; the neutral fallback covers any server-added status.
  const cls: Record<string, string> = {
    Draft: "bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-200",
    Ready: "bg-teal-100 text-teal-800 dark:bg-teal-900/50 dark:text-teal-200",
    Submitted: "bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-200",
    Paid: "bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-200",
    Denied: "bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-200",
    Voided: "bg-slate-100 text-slate-500 line-through dark:bg-slate-800 dark:text-slate-400",
  };
  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-[11px] font-semibold ${cls[status] ?? cls.Draft}`}>
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
      queryKey: ["patients", id],
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

  const totalPages = claimsQuery.data?.page.totalPages ?? 1;

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">Claims</h1>
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Kpi label="Draft" value={summary?.draft ?? 0} />
        <Kpi label="Ready" value={summary?.ready ?? 0} />
        <Kpi label="Submitted" value={summary?.submitted ?? 0} />
        <Kpi label="Outstanding" value={`$${(summary?.outstandingCharge ?? 0).toLocaleString()}`} accent />
      </div>

      {/* Status filter */}
      <div className="flex flex-wrap gap-2">
        <FilterChip active={!status} onClick={() => { setStatus(undefined); setPage(1); }}>All</FilterChip>
        {STATUSES.map((s) => (
          <FilterChip key={s} active={status === s} onClick={() => { setStatus(s); setPage(1); }}>{s}</FilterChip>
        ))}
      </div>

      {/* Table */}
      <div className="overflow-x-auto rounded-xl border border-[var(--color-border)] bg-[var(--color-card)]">
        <table className="w-full text-[13px]">
          <thead className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
            <tr>
              <th className="p-3">Patient</th>
              <th className="p-3">Payer</th>
              <th className="p-3 text-center">CPT</th>
              <th className="p-3 text-right">Charge</th>
              <th className="p-3">Status</th>
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id} className="border-t border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]">
                <td className="p-3">
                  <Link className="font-medium text-[var(--color-primary)] hover:underline" to={`/billing/claims/${c.id}`}>
                    {patientName(c.patientId)}
                  </Link>
                </td>
                <td className="p-3">{payerName(c.insuranceTypeId)}</td>
                <td className="p-3 text-center tabular-nums">{c.lineCount}</td>
                <td className="p-3 text-right font-medium tabular-nums">${c.totalCharge.toLocaleString()}</td>
                <td className="p-3"><ClaimStatusPill status={c.status} /></td>
              </tr>
            ))}
            {items.length === 0 && !claimsQuery.isLoading && (
              <tr><td colSpan={5} className="p-8 text-center text-[var(--color-muted-foreground)]">No claims yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-end gap-2 text-[13px]">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            className="rounded-lg border border-[var(--color-border)] px-3 py-1 disabled:opacity-40"
          >
            Prev
          </button>
          <span className="text-[var(--color-muted-foreground)]">
            Page {page} of {totalPages}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            className="rounded-lg border border-[var(--color-border)] px-3 py-1 disabled:opacity-40"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}

function Kpi({ label, value, accent }: { label: string; value: string | number; accent?: boolean }) {
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3">
      <div className={`text-2xl font-bold tabular-nums ${accent ? "text-[var(--color-primary)]" : ""}`}>{value}</div>
      <div className="text-[12px] text-[var(--color-muted-foreground)]">{label}</div>
    </div>
  );
}

function FilterChip({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded-full border px-3 py-1 text-[12px] font-medium transition-colors ${
        active
          ? "border-[var(--color-primary)] bg-[var(--color-primary)]/10 text-[var(--color-primary)]"
          : "border-[var(--color-border)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)]"
      }`}
    >
      {children}
    </button>
  );
}
