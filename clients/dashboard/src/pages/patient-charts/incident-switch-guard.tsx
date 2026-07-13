import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { AlertTriangle } from "lucide-react";
import type { PatientIncidentListItemDto } from "@/api/incidents";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { IncidentRef } from "@/pages/patient-charts/incident-ref";
import { usePatientTab, usePatientWorkspace } from "@/state/patient-workspace-context";

/** An open report reduced to what the guard needs: which incident it belongs to. */
export type OpenReportRef = { id: string; incidentId: string | null };

type RequestOptions = {
  /** Skip the confirmation for a switch the user hasn't really *chosen* — the
   *  chooser the chart auto-pops on load (see chart.tsx). */
  skipConfirm?: boolean;
  /** Runs only if the switch actually happens (immediately, or on confirm).
   *  Cancelling leaves everything untouched, so it never runs then. */
  onProceed?: () => void;
};

export type IncidentSwitchApi = {
  requestIncidentSwitch(incidentId: string, opts?: RequestOptions): void;
};

type PendingSwitch = { incidentId: string; onProceed?: () => void };

/**
 * Gates every user-initiated change of the chart's active incident behind a
 * confirmation, because switching strands the reports open from the previous
 * incident: they stay open but turn read-only (ReportEditorPanel's
 * `isForeignIncident`). Confirming applies the switch; cancelling does nothing
 * at all — no incident change, and the pending `onProceed` (e.g. "and open this
 * report") never runs.
 *
 * The state lives in this hook, used by the chart page, which renders the
 * returned `dialog` and shares `api` down through `IncidentSwitchProvider` so
 * descendants (the report editor's read-only banner) switch through the same
 * gate without prop drilling. It deliberately does NOT live in
 * patient-workspace-context: that context stays pure serializable state, and
 * its other consumers (sidebar, tab strip) shouldn't re-render on a pending
 * switch.
 *
 * `setActiveIncident` is still called directly by the chart's auto-select
 * effect — that is system-driven repair (no incident loaded, or the loaded one
 * disappeared), not a user switch, and must never prompt.
 */
export function useIncidentSwitchGuard({
  patientId,
  incidents,
  openReports,
}: {
  patientId: string | undefined;
  incidents: PatientIncidentListItemDto[];
  openReports: OpenReportRef[];
}): { api: IncidentSwitchApi; dialog: ReactNode } {
  const { setActiveIncident } = usePatientWorkspace();
  const activeIncidentId = usePatientTab(patientId)?.activeIncidentId ?? null;
  const [pending, setPending] = useState<PendingSwitch | null>(null);

  const requestIncidentSwitch = useCallback(
    (incidentId: string, opts?: RequestOptions) => {
      if (!patientId) return;
      // Not a switch (same incident), nothing loaded to strand yet, or the
      // chart's own load-time chooser: apply it straight away.
      if (incidentId === activeIncidentId || activeIncidentId === null || opts?.skipConfirm) {
        if (incidentId !== activeIncidentId) setActiveIncident(patientId, incidentId);
        opts?.onProceed?.();
        return;
      }
      setPending({ incidentId, onProceed: opts?.onProceed });
    },
    [patientId, activeIncidentId, setActiveIncident],
  );

  const api = useMemo<IncidentSwitchApi>(() => ({ requestIncidentSwitch }), [requestIncidentSwitch]);

  // Reports on the CURRENT incident are the ones this switch would lock; reports
  // already belonging to some other incident are read-only either way.
  const strandedCount = openReports.filter((r) => r.incidentId === activeIncidentId).length;
  const targetIncident = pending
    ? (incidents.find((x) => x.id === pending.incidentId) ?? null)
    : null;

  const onConfirm = () => {
    if (!patientId || !pending) return;
    setActiveIncident(patientId, pending.incidentId);
    pending.onProceed?.();
    setPending(null);
  };

  const dialog = (
    <Dialog open={pending !== null} onOpenChange={(o) => (!o ? setPending(null) : undefined)}>
      <DialogContent data-testid="incident-switch-confirm" className="!max-w-md">
        <DialogHeader>
          <DialogTitle>Switch to a different incident?</DialogTitle>
        </DialogHeader>

        <DialogBody>
          <div className="flex items-start gap-2 text-[13px]">
            <AlertTriangle className="mt-0.5 size-4 shrink-0 text-[var(--color-destructive)]" />
            <div className="min-w-0 space-y-1">
              <p>
                The chart will load{" "}
                {targetIncident ? (
                  <IncidentRef incident={targetIncident} className="font-semibold" />
                ) : (
                  "another incident"
                )}
                .
              </p>
              <p className="text-[var(--color-muted-foreground)]">
                {strandedCount === 0
                  ? "No reports are open."
                  : `${strandedCount} open report${strandedCount === 1 ? "" : "s"} from the current incident will become read-only. ${
                      strandedCount === 1 ? "It stays" : "They stay"
                    } open — you can switch back at any time.`}
              </p>
            </div>
          </div>
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => setPending(null)}>
            Cancel
          </Button>
          <Button type="button" onClick={onConfirm}>
            Switch incident
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );

  return { api, dialog };
}

const IncidentSwitchContext = createContext<IncidentSwitchApi | null>(null);

export function IncidentSwitchProvider({
  api,
  children,
}: {
  api: IncidentSwitchApi;
  children: ReactNode;
}) {
  return <IncidentSwitchContext.Provider value={api}>{children}</IncidentSwitchContext.Provider>;
}

export function useIncidentSwitch(): IncidentSwitchApi {
  const ctx = useContext(IncidentSwitchContext);
  if (!ctx) throw new Error("useIncidentSwitch must be used within IncidentSwitchProvider");
  return ctx;
}
