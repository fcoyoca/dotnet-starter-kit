import { useMemo, useState } from "react";
import { useQueries, useQuery } from "@tanstack/react-query";
import { ChevronDown, ChevronRight, FileText } from "lucide-react";
import type { PatientIncidentListItemDto } from "@/api/incidents";
import {
  listCustomDiagnostics,
  listReportTypes,
  useDepartmentOptions,
  useIncidentTypeOptions,
} from "@/api/administration";
import { searchPatientReports } from "@/api/reports";
import { REPORT_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
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
  /** Open incidents for the patient (BackChart lists open incidents only). */
  incidents: PatientIncidentListItemDto[];
  /** "Select" on an incident — loads it as the chart's active incident. */
  onSelect(incidentId: string): void;
  /** Clicking a report row — loads its incident and opens the report. */
  onOpenReport?(incidentId: string, reportId: string): void;
};

function resolveLabel(
  id: string | null | undefined,
  options: { value: string; label: string }[] | undefined,
): string {
  if (!id || !options) return "—";
  return options.find((o) => o.value === id)?.label ?? "—";
}

/**
 * The "Incidents" dialog — BackChart-FE parity with the open-incidents
 * chooser that pops up when a patient chart loads (IncidentsReportsList):
 * one card per open incident showing DOIV, accident info, department,
 * type, DX codes, and a collapsible "Reports (n)" list, with a Select
 * button that loads that incident into the chart.
 */
export function IncidentsListDialog({ open, onClose, incidents, onSelect, onOpenReport }: Props) {
  const { user } = useAuth();
  const canViewReports = user?.permissions?.includes(REPORT_PERMISSIONS.view) ?? false;

  const [expandedId, setExpandedId] = useState<string | null>(null);

  const departmentOptions = useDepartmentOptions();
  const incidentTypeOptions = useIncidentTypeOptions();

  // Resolve every incident's DX ids in one lookup (codes + descriptions).
  const allDxIds = useMemo(
    () => [...new Set(incidents.flatMap((x) => x.diagnosticIds))].sort(),
    [incidents],
  );
  const dxQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", allDxIds.join(",")],
    queryFn: () => listCustomDiagnostics({ ids: allDxIds, pageSize: 200 }),
    enabled: open && allDxIds.length > 0,
  });
  const dxById = useMemo(() => {
    const map = new Map<string, { code: string; description: string | null }>();
    for (const d of dxQuery.data?.items ?? []) {
      map.set(d.id, { code: d.code, description: d.description ?? null });
    }
    return map;
  }, [dxQuery.data]);

  const reportTypesQuery = useQuery({
    queryKey: ["report-types"],
    queryFn: () => listReportTypes(true),
    staleTime: 10 * 60 * 1000,
    enabled: open && canViewReports,
  });
  const reportTypeLabel = (id: number): string =>
    reportTypesQuery.data?.find((t) => t.id === id)?.name ?? "Report";

  // One reports query per open incident, so each card can show "Reports (n)"
  // up front like BackChart. Keys match the chart page's ["reports", incidentId].
  const reportQueries = useQueries({
    queries: incidents.map((incident) => ({
      queryKey: ["reports", incident.id],
      queryFn: () => searchPatientReports({ incidentId: incident.id, pageSize: 100 }),
      enabled: open && canViewReports,
    })),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <DialogHeader>
          <DialogTitle>Incidents</DialogTitle>
        </DialogHeader>

        <DialogBody className="space-y-3">
          <p className="text-[13px]">
            There are{" "}
            <span className="font-semibold text-[var(--color-destructive)]">
              {incidents.length}
            </span>{" "}
            incidents open for this patient. Please select which incident you would like to load.
          </p>

          {incidents.map((incident, i) => {
            const reports = reportQueries[i]?.data?.items ?? [];
            const expanded = expandedId === incident.id;
            return (
              <div
                key={incident.id}
                className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-3 text-[13px]"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <p className="font-semibold">
                    DOIV: {formatDate(incident.dateOfInitialVisit)}
                  </p>
                  <div className="flex items-center gap-3">
                    {incident.isAccident && (
                      <span>
                        <span className="font-semibold">Accident Related</span>
                        {incident.accidentType && incident.accidentType !== "None"
                          ? ` — ${incident.accidentType}`
                          : ""}
                      </span>
                    )}
                    <Button
                      size="sm"
                      className="h-8 rounded-lg px-4 text-[13px] font-semibold"
                      onClick={() => onSelect(incident.id)}
                    >
                      Select
                    </Button>
                  </div>
                </div>

                <div className="mt-2 space-y-1">
                  <p>
                    <span className="font-semibold">Department:</span>{" "}
                    {resolveLabel(incident.departmentId, departmentOptions)}
                  </p>
                  <p>
                    <span className="font-semibold">Type:</span>{" "}
                    {resolveLabel(incident.incidentTypeId, incidentTypeOptions)}
                  </p>
                  <div>
                    <span className="font-semibold">Dx Codes:</span>
                    {incident.diagnosticIds.length === 0 ? (
                      <span className="ml-1 text-[var(--color-muted-foreground)]">None</span>
                    ) : (
                      <ul className="ml-4 mt-0.5 space-y-0.5">
                        {incident.diagnosticIds.map((id) => {
                          const dx = dxById.get(id);
                          return (
                            <li key={id}>
                              {dx
                                ? `${dx.code}${dx.description ? ` - ${dx.description}` : ""}`
                                : "…"}
                            </li>
                          );
                        })}
                      </ul>
                    )}
                  </div>
                </div>

                {canViewReports && (
                  <div className="mt-2">
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="h-8 gap-1 rounded-lg px-3 text-[13px] font-semibold"
                      onClick={() => setExpandedId(expanded ? null : incident.id)}
                    >
                      {expanded ? (
                        <ChevronDown className="size-4" />
                      ) : (
                        <ChevronRight className="size-4" />
                      )}
                      Reports ({reports.length})
                    </Button>
                    {expanded && (
                      <ul className="mt-2 divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                        {reports.length === 0 ? (
                          <li className="px-3 py-2 text-[12px] text-[var(--color-muted-foreground)]">
                            No reports for this incident yet.
                          </li>
                        ) : (
                          reports.map((r) => (
                            <li key={r.id}>
                              <button
                                type="button"
                                className="flex w-full items-center gap-2 px-3 py-2 text-left transition-colors hover:bg-[var(--color-accent)]"
                                onClick={() => onOpenReport?.(incident.id, r.id)}
                              >
                                <FileText className="size-3.5 shrink-0 text-[var(--color-muted-foreground)]" />
                                <span className="truncate">
                                  {reportTypeLabel(r.reportTypeId)} · {formatDate(r.reportDate)}
                                  {r.signedByName ? ` · Signed by ${r.signedByName}` : ""}
                                </span>
                              </button>
                            </li>
                          ))
                        )}
                      </ul>
                    )}
                  </div>
                )}
              </div>
            );
          })}
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
