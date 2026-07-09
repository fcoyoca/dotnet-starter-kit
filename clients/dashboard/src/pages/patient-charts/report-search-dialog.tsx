import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileSearch, FileText } from "lucide-react";
import type { PatientIncidentListItemDto } from "@/api/incidents";
import { searchPatientReports } from "@/api/reports";
import { listReportTypes } from "@/api/administration";
import { usePatientWorkspace } from "@/state/patient-workspace-context";
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
import { Button } from "@/components/ui/button";
import { formatDate } from "@/lib/list-helpers";

type Props = {
  open: boolean;
  onClose(): void;
  incident: PatientIncidentListItemDto | null;
  incidentTypeLabel?: string;
};

/**
 * Search the selected incident's reports for a phrase — mirrors BackChart's
 * "Search patient reports" magnifier on the chart card. Phrase search runs
 * server-side across report field values + addendum text (min 2 chars);
 * selecting a match opens the report editor.
 */
export function ReportSearchDialog({ open, onClose, incident, incidentTypeLabel }: Props) {
  const { openReport: openReportInWorkspace } = usePatientWorkspace();
  const [phrase, setPhrase] = useState("");
  const [debounced, setDebounced] = useState("");

  useEffect(() => {
    if (!open) {
      setPhrase("");
      setDebounced("");
    }
  }, [open]);

  // Debounce the phrase so each keystroke doesn't fire a query.
  useEffect(() => {
    const t = setTimeout(() => setDebounced(phrase.trim()), 300);
    return () => clearTimeout(t);
  }, [phrase]);

  const canSearch = open && !!incident && debounced.length >= 2;

  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
    enabled: open,
  });

  const resultsQuery = useQuery({
    queryKey: ["report-search", incident?.id, debounced],
    queryFn: () =>
      searchPatientReports({ incidentId: incident!.id, search: debounced, pageSize: 50 }),
    enabled: canSearch,
  });

  const reportTypeLabel = (id: number): string =>
    reportTypesQuery.data?.find((t) => t.id === id)?.name ?? "Report";

  const items = resultsQuery.data?.items ?? [];

  const openReport = (reportId: string) => {
    if (!incident) return;
    onClose();
    openReportInWorkspace(incident.patientId, reportId);
  };

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

          {!canSearch ? (
            <div className="flex flex-col items-center justify-center rounded-lg border border-dashed border-[var(--color-border)] py-8 text-center">
              <div className="mb-3 grid size-11 place-items-center rounded-2xl bg-[var(--color-muted)]">
                <FileSearch className="size-5 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" />
              </div>
              <p className="text-[13px] font-medium">Type at least 2 characters</p>
              <p className="mt-1 max-w-[300px] text-[12px] text-[var(--color-muted-foreground)]">
                Phrase search scans this incident's report field values and addendums.
              </p>
            </div>
          ) : resultsQuery.isLoading ? (
            <div className="skeleton h-24 rounded-lg" />
          ) : items.length === 0 ? (
            <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
              No reports match “{debounced}”.
            </p>
          ) : (
            <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
              {items.map((r) => (
                <li key={r.id}>
                  <button
                    type="button"
                    onClick={() => openReport(r.id)}
                    className="flex w-full items-center gap-3 px-3 py-2.5 text-left hover:bg-[var(--color-accent)]"
                  >
                    <FileText className="size-4 shrink-0 text-[var(--color-muted-foreground)]" />
                    <span className="min-w-0 flex-1">
                      <span className="block truncate text-[13px] font-medium">
                        {reportTypeLabel(r.reportTypeId)}
                      </span>
                      <span className="block text-[12px] text-[var(--color-muted-foreground)]">
                        {formatDate(r.reportDate)} · {r.workflowStatus}
                      </span>
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
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
