import { useEffect, useMemo, useState } from "react";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Search, UserRound } from "lucide-react";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { describe, formatDate } from "@/lib/list-helpers";

const PAGE_SIZE = 20;

function fullName(p: PatientListItemDto): string {
  return [p.firstName, p.middleInitial, p.lastName].filter(Boolean).join(" ");
}

type Props = {
  open: boolean;
  onClose(): void;
  /** Called with the picked patient; the chart page opens it as a workspace tab. */
  onSelect(patient: PatientListItemDto): void;
};

/**
 * Patient picker for the chart page — mirrors BackChart's "Search for Patient"
 * dialog on PatientChartIndex, which lets the user pull additional patients into
 * the chart workspace without leaving the chart they're already in. Selecting a
 * result opens that patient as another chart tab.
 */
export function PatientSearchDialog({ open, onClose, onSelect }: Props) {
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [pageNumber, setPageNumber] = useState(1);

  useEffect(() => {
    if (!open) {
      setSearch("");
      setDebounced("");
      setPageNumber(1);
    }
  }, [open]);

  useEffect(() => {
    const t = setTimeout(() => {
      setDebounced(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debounced,
      sortBy: "lastName",
      sortDir: "asc" as const,
    }),
    [pageNumber, debounced],
  );

  // Search-first, like the chart list page: nothing is fetched until the user types.
  const canSearch = open && debounced.length > 0;

  const query = useQuery({
    queryKey: ["patient-charts", "search-dialog", queryParams],
    queryFn: () => searchPatients(queryParams),
    enabled: canSearch,
    placeholderData: keepPreviousData,
  });

  const items = canSearch ? query.data?.items ?? [] : [];

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-xl">
        <DialogHeader>
          <DialogTitle>Search for Patient</DialogTitle>
          <DialogDescription>
            Pick a patient to open their chart in a new tab alongside the ones already open.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="space-y-4">
          <Input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search by name or patient code…"
            aria-label="Search patients"
            autoFocus
          />

          {!canSearch ? (
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-[var(--color-border)] py-8 text-center">
              <div className="mb-3 grid size-11 place-items-center rounded-2xl bg-[var(--color-muted)]">
                <Search className="size-5 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
              </div>
              <p className="text-[13px] font-medium">Search for a patient</p>
              <p className="mt-1 max-w-[320px] text-[12px] text-[var(--color-muted-foreground)]">
                Type a name or patient code to find the chart you want to open.
              </p>
            </div>
          ) : query.isLoading && items.length === 0 ? (
            <div className="skeleton h-24 rounded-lg" />
          ) : items.length === 0 ? (
            <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
              No patients match “{debounced}”.
            </p>
          ) : (
            <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
              {items.map((p) => (
                <li key={p.id}>
                  <button
                    type="button"
                    aria-label={`Open chart for ${fullName(p)}`}
                    onClick={() => onSelect(p)}
                    className="flex w-full items-center gap-3 px-3 py-2.5 text-left hover:bg-[var(--color-accent)]"
                  >
                    <UserRound className="size-4 shrink-0 text-[var(--color-muted-foreground)]" />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-[13px] font-medium">{fullName(p)}</span>
                      <span className="block text-[12px] text-[var(--color-muted-foreground)]">
                        {p.patientCode} · DOB {formatDate(p.dateOfBirth)}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}

          {query.isError && (
            <div
              role="alert"
              className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
            >
              {describe(query.error)}
            </div>
          )}
        </DialogBody>

        <DialogFooter className="sm:justify-between">
          {canSearch && (query.data?.totalPages ?? 1) > 1 ? (
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!query.data?.hasPrevious}
                onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
              >
                Previous
              </Button>
              <span className="text-[12px] text-[var(--color-muted-foreground)]">
                Page {query.data?.pageNumber ?? 1} of {query.data?.totalPages ?? 1}
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!query.data?.hasNext}
                onClick={() => setPageNumber((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          ) : (
            <span />
          )}
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Close
            </Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
