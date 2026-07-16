import { formatDate } from "@/lib/list-helpers";

/**
 * Normalized, source-agnostic shape for a printable bill. The SuperBill editor
 * (live rows) and the claim-detail page (billed snapshot) both project into this
 * so the on-screen summary and the printed sheet stay identical.
 *
 * Mirrors legacy BackChart's `BillingViewBill.razor`: patient + insurance header,
 * a procedure table with the DX codes each line is linked to, and a charge total.
 * Payments/remainder are optional — clinic-solution-app has no payments module yet,
 * so callers that lack that data simply omit `totalPayments`.
 */
export type BillLine = {
  code: string;
  description?: string | null;
  charge: number;
  dxCodes: string[];
};

export type InsuranceBlock = {
  type?: string | null;
  provider?: string | null;
  groupNumber?: string | null;
  policyNumber?: string | null;
  subscriber?: string | null;
};

export type BillSummaryData = {
  patient: {
    name: string;
    code?: string | null;
    dob?: string | null;
    phone?: string | null;
    /** Address lines, already assembled and non-empty. */
    address?: string[];
  };
  primaryInsurance?: InsuranceBlock | null;
  secondaryInsurance?: InsuranceBlock | null;
  /** Kept for the print sheet's subtitle date only; encounter is no longer a card
   * (it lives on the SuperBill / claim mini-cards instead). */
  encounter?: {
    reportDate?: string | null;
  } | null;
  lines: BillLine[];
  /** Omit for a charge-only summary; present enables the payments/remainder rows. */
  totalPayments?: number | null;
};

const money = (n: number) =>
  n.toLocaleString(undefined, { style: "currency", currency: "USD" });

export function billTotalCharge(data: BillSummaryData): number {
  return data.lines.reduce((sum, l) => sum + (Number.isFinite(l.charge) ? l.charge : 0), 0);
}

/** A small labeled row used across the on-screen summary cards. */
function Row({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex gap-2 py-0.5 text-[13px]">
      <span className="w-32 shrink-0 text-[var(--color-muted-foreground)]">{label}</span>
      <span className="min-w-0 flex-1 font-medium">{children || "—"}</span>
    </div>
  );
}

function Card({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
      <h3 className="mb-2 text-[11px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
        {title}
      </h3>
      {children}
    </section>
  );
}

/** On-screen bill summary — the same data the Print sheet renders. */
export function BillSummaryView({ data }: { data: BillSummaryData }) {
  const total = billTotalCharge(data);
  const payments = data.totalPayments ?? null;
  const remainder = payments == null ? null : total - payments;

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2">
        <Card title="Patient">
          <Row label="Name">{data.patient.name}</Row>
          {data.patient.address?.map((line, i) => (
            <Row key={i} label={i === 0 ? "Address" : ""}>
              {line}
            </Row>
          ))}
          <Row label="Telephone">{data.patient.phone}</Row>
          <Row label="DOB">{formatDate(data.patient.dob)}</Row>
          <Row label="Code">{data.patient.code}</Row>
        </Card>

        {data.primaryInsurance && (
          <Card title="Primary Insurance">
            <Row label="Insurance Type">{data.primaryInsurance.type}</Row>
            <Row label="Provider">{data.primaryInsurance.provider}</Row>
            <Row label="Group Number">{data.primaryInsurance.groupNumber}</Row>
            <Row label="Policy Number">{data.primaryInsurance.policyNumber}</Row>
            <Row label="Subscriber">{data.primaryInsurance.subscriber}</Row>
          </Card>
        )}

        {data.secondaryInsurance && (
          <Card title="Secondary Insurance">
            <Row label="Insurance Type">{data.secondaryInsurance.type}</Row>
            <Row label="Provider">{data.secondaryInsurance.provider}</Row>
            <Row label="Group Number">{data.secondaryInsurance.groupNumber}</Row>
            <Row label="Policy Number">{data.secondaryInsurance.policyNumber}</Row>
            <Row label="Subscriber">{data.secondaryInsurance.subscriber}</Row>
          </Card>
        )}
      </div>

      <div className="overflow-hidden rounded-xl border border-[var(--color-border)]">
        <table className="w-full text-[13px]">
          <thead className="bg-[var(--color-muted)]/40">
            <tr className="text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
              <th className="px-3 py-2 w-28">Code</th>
              <th className="px-3 py-2">Description</th>
              <th className="px-3 py-2 w-40 text-right">Cost</th>
            </tr>
          </thead>
          <tbody>
            {data.lines.length === 0 ? (
              <tr>
                <td colSpan={3} className="px-3 py-6 text-center text-[var(--color-muted-foreground)]">
                  No procedures on this bill.
                </td>
              </tr>
            ) : (
              data.lines.map((l, i) => (
                <tr key={`${l.code}-${i}`} className="border-t border-[var(--color-border)] align-top">
                  <td className="px-3 py-2 font-medium">{l.code}</td>
                  <td className="px-3 py-2">
                    <div>{l.description ?? "—"}</div>
                    {l.dxCodes.length > 0 && (
                      <div className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-[11px] text-[var(--color-muted-foreground)]">
                        <span className="font-semibold">DX:</span>
                        {l.dxCodes.map((dx) => (
                          <span key={dx}>{dx}</span>
                        ))}
                      </div>
                    )}
                  </td>
                  <td className="px-3 py-2 text-right font-medium">{money(l.charge)}</td>
                </tr>
              ))
            )}
          </tbody>
          <tfoot className="border-t-2 border-[var(--color-border)]">
            <tr>
              <td />
              <td className="px-3 py-2 text-right font-semibold">Total Cost</td>
              <td className="px-3 py-2 text-right font-bold text-[var(--color-primary)]">{money(total)}</td>
            </tr>
            {payments != null && (
              <>
                <tr>
                  <td />
                  <td className="px-3 py-1 text-right font-semibold">Total Payments</td>
                  <td className="px-3 py-1 text-right font-medium">{money(payments)}</td>
                </tr>
                <tr>
                  <td />
                  <td className="px-3 py-1 text-right font-semibold">Remainder</td>
                  <td className="px-3 py-1 text-right font-bold">{money(remainder ?? 0)}</td>
                </tr>
              </>
            )}
          </tfoot>
        </table>
      </div>
    </div>
  );
}

// ── Print / PDF ────────────────────────────────────────────────────────────
// Self-contained: opens a print window with an inline-styled sheet and triggers
// the browser print dialog (Save-as-PDF is the built-in destination). No PDF lib
// dependency, and nothing leaks the app chrome into the printout.

const esc = (s: unknown) =>
  String(s ?? "").replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" })[c]!);

function billHtml(data: BillSummaryData, title: string): string {
  const total = billTotalCharge(data);
  const payments = data.totalPayments ?? null;
  const remainder = payments == null ? null : total - payments;

  const row = (label: string, value: unknown) =>
    `<tr><td class="lbl">${esc(label)}</td><td>${esc(value) || "&mdash;"}</td></tr>`;

  const patientRows = [
    row("Name", data.patient.name),
    ...(data.patient.address ?? []).map((line, i) => row(i === 0 ? "Address" : "", line)),
    row("Telephone", data.patient.phone),
    row("DOB", formatDate(data.patient.dob)),
    row("Code", data.patient.code),
  ].join("");

  const insuranceRows = (ins: InsuranceBlock) =>
    [
      row("Insurance Type", ins.type),
      row("Provider", ins.provider),
      row("Group Number", ins.groupNumber),
      row("Policy Number", ins.policyNumber),
      row("Subscriber", ins.subscriber),
    ].join("");
  const primaryRows = data.primaryInsurance ? insuranceRows(data.primaryInsurance) : "";
  const secondaryRows = data.secondaryInsurance ? insuranceRows(data.secondaryInsurance) : "";

  const lineRows = data.lines.length
    ? data.lines
        .map(
          (l) => `<tr>
            <td class="code">${esc(l.code)}</td>
            <td>${esc(l.description) || "&mdash;"}${
              l.dxCodes.length ? `<div class="dx"><b>DX:</b> ${l.dxCodes.map(esc).join(", ")}</div>` : ""
            }</td>
            <td class="num">${esc(money(l.charge))}</td>
          </tr>`,
        )
        .join("")
    : `<tr><td colspan="3" class="empty">No procedures on this bill.</td></tr>`;

  const footRows = [
    `<tr><td></td><td class="num lbl">Total Cost</td><td class="num total">${esc(money(total))}</td></tr>`,
    payments != null
      ? `<tr><td></td><td class="num lbl">Total Payments</td><td class="num">${esc(money(payments))}</td></tr>` +
        `<tr><td></td><td class="num lbl">Remainder</td><td class="num total">${esc(money(remainder ?? 0))}</td></tr>`
      : "",
  ].join("");

  return `<!doctype html><html><head><meta charset="utf-8"><title>${esc(title)}</title>
    <style>
      * { box-sizing: border-box; }
      body { font-family: -apple-system, Segoe UI, Roboto, Helvetica, Arial, sans-serif; color: #0f172a; margin: 32px; }
      h1 { font-size: 18px; margin: 0 0 4px; }
      .sub { color: #64748b; font-size: 12px; margin-bottom: 20px; }
      .cards { display: grid; grid-template-columns: 1fr 1fr; gap: 16px; margin-bottom: 20px; }
      .card { border: 1px solid #e2e8f0; border-radius: 10px; padding: 12px 14px; }
      .card h2 { font-size: 11px; text-transform: uppercase; letter-spacing: .04em; color: #0d9488; margin: 0 0 6px; }
      .card table { width: 100%; border-collapse: collapse; }
      .card td { padding: 2px 0; font-size: 12.5px; vertical-align: top; }
      .card td.lbl { color: #64748b; width: 120px; }
      table.lines { width: 100%; border-collapse: collapse; border: 1px solid #e2e8f0; border-radius: 10px; overflow: hidden; }
      table.lines thead th { background: #f1f5f9; text-align: left; font-size: 11px; text-transform: uppercase; letter-spacing: .04em; color: #64748b; padding: 8px 12px; }
      table.lines td { padding: 8px 12px; font-size: 12.5px; border-top: 1px solid #e2e8f0; vertical-align: top; }
      table.lines td.code { font-weight: 600; width: 110px; }
      table.lines td.num { text-align: right; white-space: nowrap; width: 150px; }
      table.lines td.empty { text-align: center; color: #94a3b8; padding: 24px; }
      .dx { margin-top: 3px; font-size: 11px; color: #64748b; }
      tfoot td { border-top: 1px solid #e2e8f0; }
      tfoot td.lbl { font-weight: 600; }
      tfoot td.total { font-weight: 700; color: #0d9488; }
      @media print { body { margin: 12px; } }
    </style></head><body onload="window.print()">
    <h1>${esc(title)}</h1>
    <div class="sub">${esc(data.patient.name)}${data.encounter?.reportDate ? " &middot; " + esc(formatDate(data.encounter.reportDate)) : ""}</div>
    <div class="cards">
      <div class="card"><h2>Patient</h2><table>${patientRows}</table></div>
      ${primaryRows ? `<div class="card"><h2>Primary Insurance</h2><table>${primaryRows}</table></div>` : ""}
      ${secondaryRows ? `<div class="card"><h2>Secondary Insurance</h2><table>${secondaryRows}</table></div>` : ""}
    </div>
    <table class="lines">
      <thead><tr><th>Code</th><th>Description</th><th style="text-align:right">Cost</th></tr></thead>
      <tbody>${lineRows}</tbody>
      <tfoot>${footRows}</tfoot>
    </table>
  </body></html>`;
}

/** Open a print window for the bill. Returns false if the browser blocked the popup. */
export function printBillSummary(data: BillSummaryData, title = "Bill"): boolean {
  const win = window.open("", "_blank", "width=820,height=1000");
  if (!win) return false;
  win.document.write(billHtml(data, title));
  win.document.close();
  win.focus();
  return true;
}
