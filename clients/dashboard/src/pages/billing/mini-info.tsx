/**
 * Compact read-only info cards shared by the billing surfaces (SuperBill dialog +
 * claim-detail). Mirrors legacy BackChart's Patient / Incident / Report cards.
 */

/** Compact labeled row inside a mini-info card. */
export function MiniRow({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex gap-2 py-0.5 text-[12.5px]">
      <span className="w-24 shrink-0 text-[var(--color-muted-foreground)]">{label}</span>
      <span
        className="min-w-0 flex-1 truncate font-medium"
        title={typeof children === "string" ? children : undefined}
      >
        {children || "—"}
      </span>
    </div>
  );
}

export function MiniCard({
  icon,
  title,
  action,
  children,
}: {
  icon: React.ReactNode;
  title: string;
  action?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3">
      <div className="mb-2 flex items-center justify-between gap-2">
        <h3 className="flex items-center gap-1.5 text-[11px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
          {icon}
          {title}
        </h3>
        {action}
      </div>
      {children}
    </section>
  );
}

/** DX-code chip row used inside the Incident card. */
export function MiniDxRow({
  ids,
  label,
}: {
  ids: string[];
  /** id → { code, description } for tooltips; falls back to a truncated id. */
  label: (id: string) => { code: string; description?: string | null };
}) {
  return (
    <div className="flex gap-2 py-0.5 text-[12.5px]">
      <span className="w-24 shrink-0 text-[var(--color-muted-foreground)]">DX Codes</span>
      <span className="flex min-w-0 flex-1 flex-wrap gap-x-2 gap-y-0.5">
        {ids.length === 0 ? (
          <span className="font-medium">—</span>
        ) : (
          ids.map((id) => {
            const l = label(id);
            return (
              <span
                key={id}
                title={l.description ?? undefined}
                className="font-semibold text-[var(--color-foreground)]"
              >
                {l.code}
              </span>
            );
          })
        )}
      </span>
    </div>
  );
}
