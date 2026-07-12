import { useEffect, useState } from "react";
import { Search, UserRound, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PatientSearchDialog } from "@/components/scheduling/patient-search-dialog";
import { type PatientListItemDto } from "@/api/patients";

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
 * PatientPicker — a collapsed patient control. A selected patient renders as a
 * read-only chip (name + DOB) with Change/Clear; empty renders a "Search for
 * patient" button. All searching happens in a nested PatientSearchDialog
 * (BackChart parity: read-only name box + a search popup). The parent passes
 * `initialLabel` so the chip shows a name when editing or pre-filling without
 * re-fetching.
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
  const [searchOpen, setSearchOpen] = useState(false);

  useEffect(() => {
    if (value === null) {
      setSelected(null);
      setLabel(null);
    } else if (initialLabel) {
      setLabel(initialLabel);
    }
  }, [value, initialLabel]);

  const pick = (p: PatientListItemDto) => {
    setSelected(p);
    setLabel(patientLabel(p));
    setSearchOpen(false);
    onChange(p.id, p);
  };

  const clear = () => {
    setSelected(null);
    setLabel(null);
    onChange(null, null);
  };

  return (
    <div className="space-y-2">
      {value ? (
        <div className="flex items-center justify-between gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2">
          <div className="flex min-w-0 items-center gap-2.5">
            <span
              aria-hidden
              className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              <UserRound className="h-3.5 w-3.5" />
            </span>
            <div className="min-w-0">
              <div className="truncate text-sm font-medium tracking-tight">{label ?? value}</div>
              {selected?.dateOfBirth && (
                <div className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                  DOB {fmtDob(selected.dateOfBirth)}
                </div>
              )}
            </div>
          </div>
          {!disabled && (
            <div className="flex shrink-0 items-center gap-1">
              <Button type="button" variant="outline" size="sm" onClick={() => setSearchOpen(true)}>
                Change
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={clear}
                aria-label="Clear patient"
              >
                <X className="h-3.5 w-3.5" />
                Clear
              </Button>
            </div>
          )}
        </div>
      ) : (
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="w-full justify-start"
          onClick={() => setSearchOpen(true)}
          disabled={disabled}
        >
          <Search className="h-3.5 w-3.5" />
          Search for patient
        </Button>
      )}

      <PatientSearchDialog open={searchOpen} onOpenChange={setSearchOpen} onSelect={pick} />
    </div>
  );
}
