import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";

/**
 * One open patient tab. Mirrors BackChart-FE's per-patient scoped state
 * (active incident + open reports), persisted here in a single React
 * Context instead of two localStorage ID trees.
 */
export type OpenPatientTab = {
  patientId: string;
  /** Cached display label (full name) — avoids a refetch just to render the tab. */
  patientLabel: string;
  activeIncidentId: string | null;
  /** Reports considered "open" for this patient tab; multiple may be open,
   *  only one (activeReportId) renders at a time (see report-editor-dialog.tsx). */
  openReportIds: string[];
  activeReportId: string | null;
};

type WorkspaceState = {
  openTabs: OpenPatientTab[];
  activePatientId: string | null;
};

type PatientWorkspaceContextValue = WorkspaceState & {
  openPatient: (patientId: string, patientLabel: string) => void;
  closePatient: (patientId: string) => void;
  setActivePatient: (patientId: string) => void;
  setActiveIncident: (patientId: string, incidentId: string | null) => void;
  openReport: (patientId: string, reportId: string) => void;
  closeReport: (patientId: string, reportId: string) => void;
  setActiveReport: (patientId: string, reportId: string | null) => void;
};

const STORAGE_KEY = "fsh.dashboard.patientWorkspace.v1";
const EMPTY_STATE: WorkspaceState = { openTabs: [], activePatientId: null };

/** Replace the tab matching `patientId` via `updater`; no-op (same array
 *  reference) if no tab matches, so callers never trigger a redundant
 *  state update / localStorage write for a stale id. */
function replaceTab(
  tabs: OpenPatientTab[],
  patientId: string,
  updater: (tab: OpenPatientTab) => OpenPatientTab,
): OpenPatientTab[] {
  const index = tabs.findIndex((t) => t.patientId === patientId);
  if (index === -1) return tabs;
  const next = tabs.slice();
  next[index] = updater(next[index]);
  return next;
}

/** Remove `removedId` from `items`/deactivate it from `activeId`, falling
 *  back to whichever item now occupies the same position (i.e. the next
 *  item, or the new last item if it was rightmost) — matches browser/editor
 *  tab-close behavior instead of always jumping to the last-inserted item. */
function closeAndPickFallback<T>(
  items: T[],
  removedId: string,
  activeId: string | null,
  keyOf: (item: T) => string,
): { items: T[]; activeId: string | null } {
  const removedIndex = items.findIndex((item) => keyOf(item) === removedId);
  const next = items.filter((item) => keyOf(item) !== removedId);
  if (activeId !== removedId) return { items: next, activeId };
  if (next.length === 0) return { items: next, activeId: null };
  const fallbackIndex = Math.min(removedIndex, next.length - 1);
  return { items: next, activeId: keyOf(next[fallbackIndex]) };
}

function isOpenPatientTab(value: unknown): value is OpenPatientTab {
  if (!value || typeof value !== "object") return false;
  const v = value as Record<string, unknown>;
  return (
    typeof v.patientId === "string" &&
    typeof v.patientLabel === "string" &&
    (v.activeIncidentId === null || typeof v.activeIncidentId === "string") &&
    Array.isArray(v.openReportIds) &&
    v.openReportIds.every((x) => typeof x === "string") &&
    (v.activeReportId === null || typeof v.activeReportId === "string")
  );
}

function readStoredState(): WorkspaceState {
  if (typeof window === "undefined") return EMPTY_STATE;
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return EMPTY_STATE;
    const parsed = JSON.parse(raw) as Partial<WorkspaceState>;
    const openTabs = Array.isArray(parsed.openTabs)
      ? parsed.openTabs.filter(isOpenPatientTab)
      : [];
    const activePatientId =
      typeof parsed.activePatientId === "string" &&
      openTabs.some((t) => t.patientId === parsed.activePatientId)
        ? parsed.activePatientId
        : null;
    return { openTabs, activePatientId };
  } catch {
    return EMPTY_STATE;
  }
}

function writeStoredState(state: WorkspaceState): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch {
    /* storage unavailable (private browsing / quota) — state stays in-memory only */
  }
}

const PatientWorkspaceContext = createContext<PatientWorkspaceContextValue | null>(null);

/** Mount ABOVE the router `<Outlet/>` (see app-shell.tsx) so open patient
 *  tabs survive route changes; rehydrates from localStorage once on mount. */
export function PatientWorkspaceProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<WorkspaceState>(() => readStoredState());

  // Persist on every mutation.
  useEffect(() => {
    writeStoredState(state);
  }, [state]);

  const updateTab = useCallback(
    (patientId: string, updater: (tab: OpenPatientTab) => OpenPatientTab) => {
      setState((prev) => {
        const openTabs = replaceTab(prev.openTabs, patientId, updater);
        return openTabs === prev.openTabs ? prev : { ...prev, openTabs };
      });
    },
    [],
  );

  const openPatient = useCallback((patientId: string, patientLabel: string) => {
    setState((prev) => {
      const existing = prev.openTabs.some((t) => t.patientId === patientId);
      if (existing) {
        const openTabs = replaceTab(prev.openTabs, patientId, (t) => ({ ...t, patientLabel }));
        return { openTabs, activePatientId: patientId };
      }
      const tab: OpenPatientTab = {
        patientId,
        patientLabel,
        activeIncidentId: null,
        openReportIds: [],
        activeReportId: null,
      };
      return { openTabs: [...prev.openTabs, tab], activePatientId: patientId };
    });
  }, []);

  const closePatient = useCallback((patientId: string) => {
    setState((prev) => {
      const { items: openTabs, activeId: activePatientId } = closeAndPickFallback(
        prev.openTabs,
        patientId,
        prev.activePatientId,
        (t) => t.patientId,
      );
      return { openTabs, activePatientId };
    });
  }, []);

  const setActivePatient = useCallback((patientId: string) => {
    setState((prev) =>
      prev.openTabs.some((t) => t.patientId === patientId)
        ? { ...prev, activePatientId: patientId }
        : prev,
    );
  }, []);

  const setActiveIncident = useCallback(
    (patientId: string, incidentId: string | null) => {
      updateTab(patientId, (t) => ({ ...t, activeIncidentId: incidentId }));
    },
    [updateTab],
  );

  const openReport = useCallback(
    (patientId: string, reportId: string) => {
      updateTab(patientId, (t) => ({
        ...t,
        openReportIds: t.openReportIds.includes(reportId)
          ? t.openReportIds
          : [...t.openReportIds, reportId],
        activeReportId: reportId,
      }));
    },
    [updateTab],
  );

  const closeReport = useCallback(
    (patientId: string, reportId: string) => {
      updateTab(patientId, (t) => {
        const { items: openReportIds, activeId: activeReportId } = closeAndPickFallback(
          t.openReportIds,
          reportId,
          t.activeReportId,
          (id) => id,
        );
        return { ...t, openReportIds, activeReportId };
      });
    },
    [updateTab],
  );

  /** Setting an id not present in `openReportIds` is a no-op — `activeReportId`
   *  must always be a member of the open list (or null) so the pill-list UI
   *  never has to render an "active" tab that doesn't exist. */
  const setActiveReport = useCallback(
    (patientId: string, reportId: string | null) => {
      updateTab(patientId, (t) =>
        reportId === null || t.openReportIds.includes(reportId)
          ? { ...t, activeReportId: reportId }
          : t,
      );
    },
    [updateTab],
  );

  const value = useMemo<PatientWorkspaceContextValue>(
    () => ({
      ...state,
      openPatient,
      closePatient,
      setActivePatient,
      setActiveIncident,
      openReport,
      closeReport,
      setActiveReport,
    }),
    [
      state,
      openPatient,
      closePatient,
      setActivePatient,
      setActiveIncident,
      openReport,
      closeReport,
      setActiveReport,
    ],
  );

  return (
    <PatientWorkspaceContext.Provider value={value}>{children}</PatientWorkspaceContext.Provider>
  );
}

export function usePatientWorkspace(): PatientWorkspaceContextValue {
  const ctx = useContext(PatientWorkspaceContext);
  if (!ctx) throw new Error("usePatientWorkspace must be used within PatientWorkspaceProvider");
  return ctx;
}

/** The open tab for a given patientId (route param), or null if that
 *  patient has no open tab yet. Use this instead of useActivePatientTab
 *  when a page is scoped to a specific :patientId — the active tab and
 *  the routed patient are usually the same, but a directly-navigated /
 *  bookmarked chart URL may not have registered a tab yet. */
export function usePatientTab(patientId: string | undefined): OpenPatientTab | null {
  const { openTabs } = usePatientWorkspace();
  if (!patientId) return null;
  return openTabs.find((t) => t.patientId === patientId) ?? null;
}

/** The tab matching activePatientId, or null. Used by the sidebar to
 *  resolve the "Patient Chart" link to the active patient's chart. */
export function useActivePatientTab(): OpenPatientTab | null {
  const { openTabs, activePatientId } = usePatientWorkspace();
  return openTabs.find((t) => t.patientId === activePatientId) ?? null;
}
