import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Search, UserRound, X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
import { cn } from "@/lib/cn";

/** Display label for a patient row: "Last, First · CODE". */
export function patientLabel(p: { firstName: string; lastName: string; patientCode: string }): string {
  return `${p.lastName}, ${p.firstName} · ${p.patientCode}`;
}

function fmtDob(iso: string): string {
  return new Intl.DateTimeFormat("en-US", { year: "numeric", month: "short", day: "numeric" }).format(
    new Date(iso),
  );
}

/**
 * PatientPicker — debounced typeahead over /patient/patients (no PHI in the list response).
 * Mirrors the UserPicker pattern: selected patient renders as a clearable chip; typing fires a
 * 250ms-debounced search; results render in a dropdown. The parent passes `initialLabel` so the
 * chip can show a name when editing an existing appointment without re-fetching.
 */
export function PatientPicker({
  value,
  initialLabel,
  onChange,
  disabled,
}: {
  value: string | null;
  initialLabel?: string | null;
  onChange: (patientId: string | null, patient: PatientListItemDto | null) => void;
  disabled?: boolean;
}) {
  const [selected, setSelected] = useState<PatientListItemDto | null>(null);
  const [label, setLabel] = useState<string | null>(initialLabel ?? null);
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");
  const [open, setOpen] = useState(false);
  const wrapperRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (value === null) {
      setSelected(null);
      setLabel(null);
    } else if (initialLabel) {
      setLabel(initialLabel);
    }
  }, [value, initialLabel]);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  useEffect(() => {
    if (!open) return undefined;
    const onDown = (e: MouseEvent) => {
      if (!wrapperRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", onDown);
    return () => document.removeEventListener("mousedown", onDown);
  }, [open]);

  const resultsQuery = useQuery({
    queryKey: ["scheduling", "patients", "search", debounced],
    queryFn: () => searchPatients({ search: debounced, isActive: true, pageNumber: 1, pageSize: 8 }),
    enabled: debounced.length > 0 && open,
    staleTime: 30_000,
  });

  const results = useMemo(() => resultsQuery.data?.items ?? [], [resultsQuery.data]);

  const pick = (p: PatientListItemDto) => {
    setSelected(p);
    setLabel(patientLabel(p));
    setQuery("");
    setOpen(false);
    onChange(p.id, p);
  };

  const clear = () => {
    setSelected(null);
    setLabel(null);
    setQuery("");
    setOpen(false);
    onChange(null, null);
  };

  return (
    <div ref={wrapperRef} className="space-y-2">
      {value && (
        <div className="flex items-center justify-between gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2">
          <div className="flex min-w-0 items-center gap-2.5">
            <span
              aria-hidden
              className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              <UserRound className="h-3.5 w-3.5" />
            </span>
            <div className="min-w-0">
              <div className="truncate text-sm font-medium tracking-tight">
                {label ?? value}
              </div>
              {selected?.dateOfBirth && (
                <div className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                  DOB {fmtDob(selected.dateOfBirth)}
                </div>
              )}
            </div>
          </div>
          {!disabled && (
            <Button type="button" variant="ghost" size="sm" onClick={clear} aria-label="Clear patient" className="shrink-0">
              <X className="h-3.5 w-3.5" />
              Clear
            </Button>
          )}
        </div>
      )}

      <div className="relative">
        <Search
          aria-hidden
          className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
        />
        <Input
          type="search"
          placeholder={value ? "Search to change patient…" : "Search by name or code…"}
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              setOpen(false);
              e.currentTarget.blur();
            }
          }}
          autoComplete="off"
          spellCheck={false}
          disabled={disabled}
          className="pl-8"
        />

        {open && debounced.length > 0 && (
          <div className="absolute left-0 right-0 top-full z-30 mt-1 max-h-64 overflow-auto rounded-md border border-[var(--color-border)] bg-[var(--color-card)] shadow-[var(--shadow-lift)]">
            {resultsQuery.isFetching && results.length === 0 ? (
              <div className="flex items-center gap-2 px-3 py-2.5 text-[12.5px] text-[var(--color-muted-foreground)]">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Searching for "{debounced}"…
              </div>
            ) : results.length === 0 ? (
              <div className="px-3 py-3 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
                No patients match "{debounced}".
              </div>
            ) : (
              <div role="listbox" aria-label="Patient search results">
                {results.map((p) => (
                  <button
                    key={p.id}
                    type="button"
                    role="option"
                    aria-selected={p.id === value}
                    onClick={() => pick(p)}
                    className={cn(
                      "flex w-full items-center gap-2.5 px-3 py-2 text-left transition-colors",
                      "hover:bg-[var(--color-muted)] focus-visible:bg-[var(--color-muted)] focus-visible:outline-none",
                      p.id === value && "bg-[var(--color-primary-soft)]",
                    )}
                  >
                    <span
                      aria-hidden
                      className="grid h-6 w-6 shrink-0 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
                    >
                      {(p.lastName?.[0] ?? "?").toUpperCase()}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-[13px] font-medium tracking-tight">
                        {p.lastName}, {p.firstName}
                      </div>
                      <div className="truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
                        {p.patientCode} · DOB {fmtDob(p.dateOfBirth)}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
