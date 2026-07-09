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

const STORAGE_KEY = "fsh.patientWorkspace.v1";
const EMPTY_STATE: WorkspaceState = { openTabs: [], activePatientId: null };

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
      setState((prev) => ({
        ...prev,
        openTabs: prev.openTabs.map((t) => (t.patientId === patientId ? updater(t) : t)),
      }));
    },
    [],
  );

  const openPatient = useCallback((patientId: string, patientLabel: string) => {
    setState((prev) => {
      const existing = prev.openTabs.find((t) => t.patientId === patientId);
      if (existing) {
        const openTabs = prev.openTabs.map((t) =>
          t.patientId === patientId ? { ...t, patientLabel } : t,
        );
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
      const openTabs = prev.openTabs.filter((t) => t.patientId !== patientId);
      const activePatientId =
        prev.activePatientId === patientId
          ? (openTabs[openTabs.length - 1]?.patientId ?? null)
          : prev.activePatientId;
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
        const openReportIds = t.openReportIds.filter((id) => id !== reportId);
        const activeReportId =
          t.activeReportId === reportId
            ? (openReportIds[openReportIds.length - 1] ?? null)
            : t.activeReportId;
        return { ...t, openReportIds, activeReportId };
      });
    },
    [updateTab],
  );

  const setActiveReport = useCallback(
    (patientId: string, reportId: string | null) => {
      updateTab(patientId, (t) => ({ ...t, activeReportId: reportId }));
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
