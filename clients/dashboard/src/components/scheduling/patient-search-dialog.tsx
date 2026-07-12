import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Search } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
import { cn } from "@/lib/cn";

function fmtDob(iso: string): string {
  return new Intl.DateTimeFormat("en-US", { year: "numeric", month: "short", day: "numeric" }).format(
    new Date(iso),
  );
}

/**
 * PatientSearchDialog — a modal debounced typeahead over /patient/patients
 * (no PHI in the list response). Mirrors BackChart's "Search for Patient"
 * popup: searching happens here, not inline in the appointment form. Picking
 * a row fires `onSelect` and closes.
 */
export function PatientSearchDialog({
  open,
  onOpenChange,
  onSelect,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSelect: (patient: PatientListItemDto) => void;
}) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");

  // Reset the query each time the dialog closes so the next open starts clean.
  useEffect(() => {
    if (!open) {
      setQuery("");
      setDebounced("");
    }
  }, [open]);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  const resultsQuery = useQuery({
    queryKey: ["scheduling", "patients", "search", debounced],
    queryFn: () => searchPatients({ search: debounced, isActive: true, pageNumber: 1, pageSize: 8 }),
    enabled: debounced.length > 0 && open,
    staleTime: 30_000,
  });
  const results = useMemo(() => resultsQuery.data?.items ?? [], [resultsQuery.data]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="!max-w-md">
        <DialogHeader>
          <DialogTitle>Search for patient</DialogTitle>
          <DialogDescription>Find a patient by name or code.</DialogDescription>
        </DialogHeader>
        <div className="space-y-2 px-6 pb-6 pt-1">
          <div className="relative">
            <Search
              aria-hidden
              className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
            />
            <Input
              type="search"
              placeholder="Search by name or code…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              autoComplete="off"
              spellCheck={false}
              autoFocus
              className="pl-8"
            />
          </div>

          <div className="max-h-64 overflow-auto rounded-md border border-[var(--color-border)]">
            {debounced.length === 0 ? (
              <div className="px-3 py-3 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
                Type to search patients.
              </div>
            ) : resultsQuery.isFetching && results.length === 0 ? (
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
                    aria-selected={false}
                    onClick={() => onSelect(p)}
                    className={cn(
                      "flex w-full items-center gap-2.5 px-3 py-2 text-left transition-colors",
                      "hover:bg-[var(--color-muted)] focus-visible:bg-[var(--color-muted)] focus-visible:outline-none",
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
        </div>
      </DialogContent>
    </Dialog>
  );
}
