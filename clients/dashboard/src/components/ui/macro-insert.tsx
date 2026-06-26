import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Sparkles } from "lucide-react";
import { listMacros } from "@/api/administration";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

/**
 * MacroInsert — a small popover, scoped to a report field, that lists the
 * tenant's macros for that field and inserts the chosen macro's text into the
 * focused editor field. Mirrors BackChart's per-field macro picker. The macros
 * come from the Administration catalog (`listMacros({ reportFieldId })`).
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

  const macrosQuery = useQuery({
    queryKey: ["administration.macros", "field", reportFieldId],
    queryFn: () => listMacros({ reportFieldId, isActive: true, pageSize: 100 }),
    // Only fetch once the popover is opened — avoids N queries on a long report.
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const macros = (macrosQuery.data?.items ?? []).filter((m) => (m.text ?? "").trim().length > 0);

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
        {macrosQuery.isLoading ? (
          <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">Loading…</p>
        ) : macros.length === 0 ? (
          <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
            No macros for this field.
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
                  <span className="text-[13px] font-medium">{m.name}</span>
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
