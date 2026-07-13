import type { PatientIncidentListItemDto } from "@/api/incidents";
import { cn } from "@/lib/cn";
import { formatDate } from "@/lib/list-helpers";

/**
 * The two dates that identify an incident to a clinician: date of initial visit
 * (DOIV) and date of loss (DOL). A chart can hold several incidents at once and
 * reports from different ones can sit open side by side, so any surface showing
 * a report away from its incident's own context labels itself with this pair.
 */
export type IncidentDates = Pick<
  PatientIncidentListItemDto,
  "dateOfInitialVisit" | "dateOfLoss"
>;

function incidentRefText(incident: IncidentDates): string {
  return `DOIV ${formatDate(incident.dateOfInitialVisit)} · DOL ${formatDate(incident.dateOfLoss)}`;
}

/**
 * Inline DOIV/DOL label. `tone="foreign"` marks a report whose incident is NOT
 * the chart's currently active one — the mix-up this label exists to prevent.
 *
 * Renders nothing until the incident resolves, so a chart mid-load shows no
 * label rather than a wrong or placeholder one.
 */
export function IncidentRef({
  incident,
  tone = "muted",
  className,
  testId,
}: {
  incident: IncidentDates | null | undefined;
  tone?: "muted" | "foreign";
  className?: string;
  testId?: string;
}) {
  if (!incident) return null;
  return (
    <span
      data-testid={testId}
      title={incidentRefText(incident)}
      className={cn(
        "whitespace-nowrap tabular-nums",
        tone === "foreign"
          ? "text-[var(--color-destructive)]"
          : "text-[var(--color-muted-foreground)]",
        className,
      )}
    >
      {incidentRefText(incident)}
    </span>
  );
}
