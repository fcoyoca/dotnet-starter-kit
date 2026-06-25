import { useEffect, useState } from "react";
import { FileSearch } from "lucide-react";
import type { PatientIncidentListItemDto } from "@/api/incidents";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { formatDate } from "@/lib/list-helpers";

type Props = {
  open: boolean;
  onClose(): void;
  incident: PatientIncidentListItemDto | null;
  incidentTypeLabel?: string;
};

/**
 * Search the selected incident's reports for a phrase — mirrors BackChart's
 * "Search patient reports" magnifier on the chart card. Patient reports
 * themselves land in a later sprint, so this presents the search shell scoped
 * to the active incident and surfaces that state until the reports API exists.
 */
export function ReportSearchDialog({ open, onClose, incident, incidentTypeLabel }: Props) {
  const [phrase, setPhrase] = useState("");

  useEffect(() => {
    if (!open) setPhrase("");
  }, [open]);

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <DialogHeader>
          <DialogTitle>Search Patient Reports</DialogTitle>
        </DialogHeader>

        <DialogBody className="space-y-4">
          {incident ? (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Searching reports for the incident dated{" "}
              <span className="font-medium text-[var(--color-foreground)]">
                {formatDate(incident.dateOfLoss)}
              </span>
              {incidentTypeLabel && incidentTypeLabel !== "—" ? ` · ${incidentTypeLabel}` : ""}.
            </p>
          ) : (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">
              Select an incident first to search its reports.
            </p>
          )}

          <Input
            type="text"
            value={phrase}
            onChange={(e) => setPhrase(e.target.value)}
            placeholder="Search report text for a phrase…"
            disabled={!incident}
          />

          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-[var(--color-border)] py-8 text-center">
            <div className="mb-3 grid size-11 place-items-center rounded-2xl bg-[var(--color-muted)]">
              <FileSearch className="size-5 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
            </div>
            <p className="text-[13px] font-medium">Patient reports are coming soon</p>
            <p className="mt-1 max-w-[300px] text-[12px] text-[var(--color-muted-foreground)]">
              Report search will return matching reports for this incident once the patient
              reports module ships in an upcoming sprint.
            </p>
          </div>
        </DialogBody>

        <DialogFooter>
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
