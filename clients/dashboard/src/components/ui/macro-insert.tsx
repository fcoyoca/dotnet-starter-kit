import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Sparkles } from "lucide-react";
import { listMacros, type MacroDto } from "@/api/administration";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

/**
 * MacroInsert — a small popover, scoped to a report field, that lists the
 * tenant's macros and inserts the chosen macro's text into the focused editor
 * field. Mirrors BackChart's per-field macro picker. Macros come from the
 * Administration catalog, which keeps them in two buckets: those assigned to a
 * specific report field, and the "All (General)" bucket (unassigned). We surface
 * BOTH — field-specific first, then general — so the picker is populated even
 * when a field has no macros of its own.
 *
 * The insert is delivered through `onInsert(text)` so the parent decides how to
 * splice it (append, replace selection, etc.) — see the report editor (R14).
 */
export function MacroInsert({
  reportFieldId,
  onInsert,
  disabled,
  className,
}: {
  reportFieldId: number;
  onInsert: (text: string) => void;
  disabled?: boolean;
  className?: string;
}) {
  const [open, setOpen] = useState(false);

  // Only fetch once the popover is opened — avoids N queries on a long report.
  const fieldMacrosQuery = useQuery({
    queryKey: ["administration.macros", "field", reportFieldId],
    queryFn: () => listMacros({ reportFieldId, isActive: true, pageSize: 100 }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  // The "All (General)" bucket (reportFieldId = null) — shared across fields.
  const generalMacrosQuery = useQuery({
    queryKey: ["administration.macros", "general"],
    queryFn: () => listMacros({ general: true, isActive: true, pageSize: 100 }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const isLoading = fieldMacrosQuery.isLoading || generalMacrosQuery.isLoading;

  // Field-specific macros first, then general — deduped, only those with text.
  const macros = useMemo(() => {
    const withText = (m: MacroDto) => (m.text ?? "").trim().length > 0;
    const field = (fieldMacrosQuery.data?.items ?? []).filter(withText);
    const general = (generalMacrosQuery.data?.items ?? []).filter(withText);
    const seen = new Set(field.map((m) => m.id));
    return [...field, ...general.filter((m) => !seen.has(m.id))];
  }, [fieldMacrosQuery.data, generalMacrosQuery.data]);

  return (
    <DropdownMenu open={open} onOpenChange={(o) => !disabled && setOpen(o)}>
      <DropdownMenuTrigger asChild disabled={disabled}>
        <button
          type="button"
          title="Insert macro"
          aria-label="Insert macro"
          disabled={disabled}
          className={cn(
            "inline-flex h-7 items-center gap-1 rounded-md border border-[var(--color-border)] px-2",
            "text-[11px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]",
            "transition-colors hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "disabled:pointer-events-none disabled:opacity-40",
            className,
          )}
        >
          <Sparkles className="size-3.5" />
          Macro
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="max-h-[min(340px,55vh)] w-72 overflow-y-auto">
        <DropdownMenuLabel>Macros</DropdownMenuLabel>
        {isLoading ? (
          <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">Loading…</p>
        ) : macros.length === 0 ? (
          <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
            No macros available. Add them under Administration → Macros.
          </p>
        ) : (
          <ul role="none" className="py-1">
            {macros.map((m) => (
              <li key={m.id} role="none">
                <button
                  type="button"
                  onClick={() => {
                    onInsert(m.text ?? "");
                    setOpen(false);
                  }}
                  className="flex w-full flex-col gap-0.5 px-3 py-2 text-left hover:bg-[var(--color-accent)]"
                >
                  <span className="flex items-center justify-between gap-2">
                    <span className="text-[13px] font-medium">{m.name}</span>
                    {m.reportFieldId == null && (
                      <span className="shrink-0 rounded-full bg-[var(--color-muted)] px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
                        General
                      </span>
                    )}
                  </span>
                  <span className="line-clamp-2 text-[11.5px] text-[var(--color-muted-foreground)]">
                    {m.text}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
