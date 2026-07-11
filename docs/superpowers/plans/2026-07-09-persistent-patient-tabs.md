# Persistent Patient Tabs + Administration Dialogs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the dashboard's Patient Chart a persistent, BackChart-style workspace — an open patient's chart, active incident, and open reports survive navigating elsewhere and back — convert the report editor from a full-page route into a dialog matching every other chart sub-entity, and collapse Administration's 17 full-page CRUD routes into a single hub whose sections open as dialogs layered over whatever is on screen (so an open patient tab stays visible underneath).

**Architecture:** A new React Context (`PatientWorkspaceProvider`) mounted in `AppShell` above the router `<Outlet/>` holds open-patient-tab state (active incident id, open/active report ids) and persists it to `localStorage`, rehydrated once on mount — the same durability BackChart-FE gets from its scoped-DI + localStorage ID tree, reimplemented as a single versioned key. `chart.tsx` stops owning `activeIncidentId` as local `useState` and reads/writes it through the context, keyed off the `patientId` route param; React Query continues to own all data fetching untouched. The report editor becomes `ReportEditorDialog`, opened from `chart.tsx` keyed by the context's `activeReportId`, and the old routed page + its route are removed (no other code in this repo deep-links to it — confirmed by repo-wide grep). Administration's 17 admin pages are unchanged internally; a new `AdministrationHub` page renders them as cards sourced from a single exported array in `nav-data.ts`, and a generic `AdminSectionDialog` lazy-loads whichever page component the hub or a legacy `/administration/:section` deep link asks for, rendering it inside a `DialogContent` layered over the hub grid instead of routing to a bare page.

**Tech Stack:** React 19 + Vite 7 + TypeScript, TanStack Query v5, React Router 7, Tailwind v4 + Radix (shadcn-style) `Dialog` primitive, Playwright (route-mocked E2E). No backend changes — this is a dashboard-only, presentation-layer feature.

## Global Constraints

- **Frontend only** — no `src/Modules`, no migrations, no endpoint changes. Every task in this plan touches only `clients/dashboard`.
- **No new test runner.** The dashboard has no vitest/jest — only Playwright (route-mocked E2E). The new `patient-workspace-context.tsx` is exercised end-to-end via Playwright (Task 11), not a unit-test harness that doesn't exist in this repo.
- **Frontend: pass per-call data through function arguments, never via state a callback closes over** (golden rule 9 — same rule the backend plan cites for `mutate(arg)`; here it means workspace-context actions always take `patientId`/`reportId` as explicit arguments, never read an ambient "current patient" from a closure).
- **No RHF/zod** in the dashboard app (`.agents/rules/frontend/dashboard.md`) — none of this feature needs a form, so moot, but noted for anyone tempted to add one for the hub.
- **Hand-rolled localStorage helpers**, matching the existing convention in `theme-provider.tsx` / `sidebar.tsx`: `typeof window === "undefined"` guard, `try/catch` around every read/write, versioned key names (`fsh.<feature>` or `fsh.<feature>.v<N>`).
- **Every route element wrapped in `withSuspense(...)`** — no per-route permission guards in the dashboard (`ProtectedRoute` is auth-only).
- **Docs + changelog travel with the change** (golden rule 10) — see Task 13.
- Spec: `docs/superpowers/specs/2026-07-09-persistent-patient-tabs-design.md`.

---

### Task 1: `patient-workspace-context.tsx` — state module

**Files:**
- Create: `clients/dashboard/src/state/patient-workspace-context.tsx`

**Interfaces:**
- Produces: `OpenPatientTab`, `PatientWorkspaceProvider`, `usePatientWorkspace()`, `usePatientTab(patientId: string | undefined)`, `useActivePatientTab()`.

- [ ] **Step 1: Create the context module**

Create `clients/dashboard/src/state/patient-workspace-context.tsx`:

```tsx
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
```

- [ ] **Step 2: Typecheck**

Run: `cd clients/dashboard && npm run build`
Expected: `tsc -b` passes, vite build completes. (No runtime to exercise yet — nothing imports this module until Task 2.)

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/state/patient-workspace-context.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): add PatientWorkspaceProvider (persistent patient tab state)

Isolated state module: open patient tabs, active incident/report ids,
persisted to a single versioned localStorage key. Not yet mounted —
wiring lands in the following tasks.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Mount the provider + patient tab strip in `AppShell`

**Files:**
- Create: `clients/dashboard/src/components/layout/patient-tab-strip.tsx`
- Modify: `clients/dashboard/src/components/layout/app-shell.tsx`

**Interfaces:**
- Consumes: `PatientWorkspaceProvider`, `usePatientWorkspace()` (Task 1).
- Produces: `PatientTabStrip()` — renders `null` when `openTabs.length === 0`.

- [ ] **Step 1: Create the tab strip component**

Create `clients/dashboard/src/components/layout/patient-tab-strip.tsx`:

```tsx
import { useNavigate } from "react-router-dom";
import { X } from "lucide-react";
import { usePatientWorkspace } from "@/state/patient-workspace-context";
import { cn } from "@/lib/cn";

/**
 * Horizontal strip of open patient tabs — rendered under the Topbar in
 * AppShell (above the routed <Outlet/>), so it survives every route
 * change. Renders nothing when no patient tab is open.
 */
export function PatientTabStrip() {
  const { openTabs, activePatientId, setActivePatient, closePatient } = usePatientWorkspace();
  const navigate = useNavigate();

  if (openTabs.length === 0) return null;

  const onSelect = (patientId: string) => {
    setActivePatient(patientId);
    navigate(`/patient-charts/${patientId}`);
  };

  const onClose = (patientId: string) => {
    const wasActive = patientId === activePatientId;
    const remaining = openTabs.filter((t) => t.patientId !== patientId);
    closePatient(patientId);
    if (wasActive) {
      const next = remaining[remaining.length - 1];
      navigate(next ? `/patient-charts/${next.patientId}` : "/patient-charts");
    }
  };

  return (
    <div
      role="tablist"
      aria-label="Open patient charts"
      className={cn(
        "flex h-9 shrink-0 items-center gap-1 overflow-x-auto border-b border-[var(--color-border)]",
        "bg-[var(--color-muted)] px-2",
      )}
    >
      {openTabs.map((tab) => {
        const isActive = tab.patientId === activePatientId;
        return (
          <div
            key={tab.patientId}
            role="tab"
            aria-selected={isActive}
            tabIndex={0}
            onClick={() => onSelect(tab.patientId)}
            onKeyDown={(e) => {
              if (e.key === "Enter" || e.key === " ") {
                e.preventDefault();
                onSelect(tab.patientId);
              }
            }}
            className={cn(
              "group flex h-7 shrink-0 cursor-pointer items-center gap-1.5 rounded-md px-2.5 text-[12px] font-medium",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              isActive
                ? "bg-[var(--color-card)] text-[var(--color-foreground)] shadow-xs"
                : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            )}
          >
            <span className="max-w-[140px] truncate">{tab.patientLabel}</span>
            <button
              type="button"
              aria-label={`Close ${tab.patientLabel}'s chart tab`}
              onClick={(e) => {
                e.stopPropagation();
                onClose(tab.patientId);
              }}
              className="grid size-4 shrink-0 place-items-center rounded-sm opacity-60 transition-opacity hover:bg-[var(--color-border)] hover:opacity-100"
            >
              <X className="size-3" />
            </button>
          </div>
        );
      })}
    </div>
  );
}
```

- [ ] **Step 2: Mount the provider + tab strip in `AppShell`**

In `clients/dashboard/src/components/layout/app-shell.tsx`, add the import and wrap the shell. Replace:

```tsx
import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { ImpersonationBanner } from "@/components/layout/impersonation-banner";
import { ExpiryBanner } from "@/components/layout/expiry-banner";
import {
  MobileNavProvider,
  MobileNavRoot,
} from "@/components/layout/mobile-nav";
import { SseProvider } from "@/sse/sse-context";
import { RealtimeProvider } from "@/realtime/realtime-context";
import { ChatGlobalNotifier } from "@/components/notifications/chat-global-notifier";
import { CommandPaletteRoot } from "@/components/command-palette/command-palette";
import { InactivityGuard } from "@/components/auth/inactivity-guard";
import { cn } from "@/lib/cn";

export function AppShell() {
  return (
    <SseProvider>
      <RealtimeProvider>
      <MobileNavProvider>
```

with:

```tsx
import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/sidebar";
import { Topbar } from "@/components/layout/topbar";
import { PatientTabStrip } from "@/components/layout/patient-tab-strip";
import { ImpersonationBanner } from "@/components/layout/impersonation-banner";
import { ExpiryBanner } from "@/components/layout/expiry-banner";
import {
  MobileNavProvider,
  MobileNavRoot,
} from "@/components/layout/mobile-nav";
import { SseProvider } from "@/sse/sse-context";
import { RealtimeProvider } from "@/realtime/realtime-context";
import { ChatGlobalNotifier } from "@/components/notifications/chat-global-notifier";
import { CommandPaletteRoot } from "@/components/command-palette/command-palette";
import { InactivityGuard } from "@/components/auth/inactivity-guard";
import { PatientWorkspaceProvider } from "@/state/patient-workspace-context";
import { cn } from "@/lib/cn";

export function AppShell() {
  return (
    <SseProvider>
      <RealtimeProvider>
      <PatientWorkspaceProvider>
      <MobileNavProvider>
```

Then, in the same file, add `<PatientTabStrip />` between `<Topbar />` and `<main>`, and close the new provider. Replace:

```tsx
            <div className="flex min-w-0 flex-1 flex-col">
              <Topbar />
              <main
                id="main"
                tabIndex={-1}
                className="flex-1 overflow-auto p-4 focus:outline-none md:p-6"
              >
                <Outlet />
              </main>
            </div>
          </div>
        </div>

        {/* Mobile drawer — mounted at root so it can portal above the
            shell. Hidden via Sheet open state; the trigger lives in
            the Topbar. */}
        <MobileNavRoot />
      </MobileNavProvider>
      {/* Background chat notifier — listens to ChatMessageCreated on the
          shared SignalR connection and toasts when the user isn't currently
          on that channel. Mounted inside the router subtree so the route
          predicate (current /chat/:channelId) and navigate() both work. */}
      <ChatGlobalNotifier />

      {/* Mounted inside the router subtree so useNavigate inside the
          palette resolves correctly. */}
      <CommandPaletteRoot />

      {/* Inactivity auto-logout — warning modal + countdown, signed-in only. */}
      <InactivityGuard />
      </RealtimeProvider>
    </SseProvider>
  );
}
```

with:

```tsx
            <div className="flex min-w-0 flex-1 flex-col">
              <Topbar />
              <PatientTabStrip />
              <main
                id="main"
                tabIndex={-1}
                className="flex-1 overflow-auto p-4 focus:outline-none md:p-6"
              >
                <Outlet />
              </main>
            </div>
          </div>
        </div>

        {/* Mobile drawer — mounted at root so it can portal above the
            shell. Hidden via Sheet open state; the trigger lives in
            the Topbar. */}
        <MobileNavRoot />
      </MobileNavProvider>
      </PatientWorkspaceProvider>
      {/* Background chat notifier — listens to ChatMessageCreated on the
          shared SignalR connection and toasts when the user isn't currently
          on that channel. Mounted inside the router subtree so the route
          predicate (current /chat/:channelId) and navigate() both work. */}
      <ChatGlobalNotifier />

      {/* Mounted inside the router subtree so useNavigate inside the
          palette resolves correctly. */}
      <CommandPaletteRoot />

      {/* Inactivity auto-logout — warning modal + countdown, signed-in only. */}
      <InactivityGuard />
      </RealtimeProvider>
    </SseProvider>
  );
}
```

> `PatientWorkspaceProvider` is placed inside `RealtimeProvider`/`SseProvider` but wraps `MobileNavProvider`, `Sidebar`, `Topbar`, `PatientTabStrip`, and `Outlet` — every consumer added in later tasks (Sidebar, chart.tsx, report-search-dialog.tsx) is a descendant, so `usePatientWorkspace()` resolves everywhere it's needed.

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/components/layout/app-shell.tsx clients/dashboard/src/components/layout/patient-tab-strip.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): mount PatientWorkspaceProvider + tab strip in AppShell

Tab strip renders under the Topbar, above the routed Outlet, so open
patient tabs survive every navigation. Hidden when no tab is open.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Sidebar "Patient Chart" link resolves to the active tab

**Files:**
- Modify: `clients/dashboard/src/components/layout/sidebar.tsx`

**Interfaces:**
- Consumes: `usePatientWorkspace()` (Task 1).

- [ ] **Step 1: Import the hook**

In `clients/dashboard/src/components/layout/sidebar.tsx`, add to the imports:

```tsx
import { usePatientWorkspace } from "@/state/patient-workspace-context";
```

- [ ] **Step 2: Resolve the Patient Chart link inside `SidebarNavBody`**

Replace:

```tsx
export function SidebarNavBody({
  collapsed,
  openSection,
  setOpenSection,
  onNavigate,
}: {
  collapsed: boolean;
  openSection: string | null;
  setOpenSection: React.Dispatch<React.SetStateAction<string | null>>;
  /** Called after a nav item link is clicked. Used by the mobile
   *  drawer to close itself on navigation. */
  onNavigate?: () => void;
}) {
  // Hide nav entries the current (or impersonated) user lacks permission for,
  // so they can't navigate to a page the API will reject with 403.
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  const navTop = visibleItems(topNavTop, perms);
  const navSections = visibleSections(perms);
  const navBottom = visibleItems(topNavBottom, perms);
```

with:

```tsx
export function SidebarNavBody({
  collapsed,
  openSection,
  setOpenSection,
  onNavigate,
}: {
  collapsed: boolean;
  openSection: string | null;
  setOpenSection: React.Dispatch<React.SetStateAction<string | null>>;
  /** Called after a nav item link is clicked. Used by the mobile
   *  drawer to close itself on navigation. */
  onNavigate?: () => void;
}) {
  // Hide nav entries the current (or impersonated) user lacks permission for,
  // so they can't navigate to a page the API will reject with 403.
  const { user } = useAuth();
  const { activePatientId } = usePatientWorkspace();
  const perms = user?.permissions ?? [];
  const navTop = visibleItems(topNavTop, perms);
  const navSections = resolvePatientChartLink(visibleSections(perms), activePatientId);
  const navBottom = visibleItems(topNavBottom, perms);
```

- [ ] **Step 3: Add the resolver helper**

Add this function near the other module-level helpers in `sidebar.tsx` (e.g. directly above `SidebarNavBody`):

```tsx
/** Resolve the "Patient Chart" nav entry's `to` to the active patient's
 *  chart route when one is open, so clicking it jumps straight into that
 *  patient's chart instead of landing on the search page (design decision
 *  4 in the persistent-tabs spec). No-op when no patient tab is active. */
function resolvePatientChartLink(
  sections: NavSection[],
  activePatientId: string | null,
): NavSection[] {
  if (!activePatientId) return sections;
  return sections.map((s) => ({
    ...s,
    items: s.items.map((item) =>
      item.to === "/patient-charts" ? { ...item, to: `/patient-charts/${activePatientId}` } : item,
    ),
  }));
}
```

- [ ] **Step 4: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/components/layout/sidebar.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): Patient Chart nav link resolves to the active patient tab

When a patient tab is open, the sidebar's Patient Chart entry (desktop
accordion, collapsed rail, and mobile drawer — all share SidebarNavBody)
points at that patient's chart instead of the search page.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Wire `chart.tsx` to the workspace context

**Files:**
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx`

**Interfaces:**
- Consumes: `usePatientWorkspace()`, `usePatientTab(patientId)` (Task 1).

- [ ] **Step 1: Update imports**

Remove `useNavigate` from the react-router-dom import (no longer used once Task 5 removes the report navigate() calls, but the active-incident wiring in this task no longer needs it either — `navigate` is used in exactly 3 places in this file today: the report-create `onSuccess`, and the two report IconShortcut `onClick`s, all three converted in this task/Task 5). Replace:

```tsx
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
```

with:

```tsx
import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
```

Add `X` to the lucide-react icon import (used by the new "open reports" pill row's close button) — append it to the existing list:

```tsx
import {
  AlertTriangle,
  ArrowLeft,
  CalendarDays,
  ClipboardList,
  Eye,
  FileDown,
  FilePlus,
  FileSearch,
  FileText,
  FolderOpen,
  Lock,
  Pencil,
  Pill,
  Plus,
  Stethoscope,
  StickyNote,
  Tablets,
  Trash2,
  X,
} from "lucide-react";
```

Add the `cn` helper and the workspace/report-dialog imports. After the existing `import { describe, formatDate } from "@/lib/list-helpers";` line, add:

```tsx
import { cn } from "@/lib/cn";
import { usePatientTab, usePatientWorkspace } from "@/state/patient-workspace-context";
```

In the `@/pages/patient-charts/*` import block, add `ReportEditorDialog` alphabetically between `ProceduresPerformedDialog` and `ReportSearchDialog`:

```tsx
import { ProceduresPerformedDialog } from "@/pages/patient-charts/procedures-performed-dialog";
import { ReportEditorDialog } from "@/pages/patient-charts/report-editor-dialog";
import { ReportSearchDialog } from "@/pages/patient-charts/report-search-dialog";
```

> `ReportEditorDialog` doesn't exist yet — it's created in Task 5. This task's build will fail until Task 5 lands; execute Tasks 4 and 5 back-to-back (or, if executing strictly one task at a time, stub the file first) before running `npm run build`.

- [ ] **Step 2: Remove the local `activeIncidentId` useState; derive from the workspace tab**

Replace:

```tsx
export function PatientChartDetailPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const [closedFilter, setClosedFilter] = useState<ClosedFilter>("all");
  const [showDeleted, setShowDeleted] = useState(false);
  const [transferOnly, setTransferOnly] = useState(false);
  const [deptFilter, setDeptFilter] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);

  const [activeIncidentId, setActiveIncidentId] = useState<string | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
```

with:

```tsx
export function PatientChartDetailPage() {
  const { patientId } = useParams<{ patientId: string }>();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const { openPatient, setActiveIncident, openReport, closeReport, setActiveReport } =
    usePatientWorkspace();
  const workspaceTab = usePatientTab(patientId);
  const activeIncidentId = workspaceTab?.activeIncidentId ?? null;
  const openReportIds = workspaceTab?.openReportIds ?? [];
  const activeReportId = workspaceTab?.activeReportId ?? null;

  const [closedFilter, setClosedFilter] = useState<ClosedFilter>("all");
  const [showDeleted, setShowDeleted] = useState(false);
  const [transferOnly, setTransferOnly] = useState(false);
  const [deptFilter, setDeptFilter] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState<string | null>(null);

  const [createOpen, setCreateOpen] = useState(false);
```

- [ ] **Step 3: Register the patient tab on load; move `activeIncident` selection onto the context setter**

Replace:

```tsx
  const patient = patientQuery.data;
  const allIncidents = useMemo(() => incidentsQuery.data?.items ?? [], [incidentsQuery.data]);

  // Client-side refinement (the API filters by closed/deleted; these narrow further).
  const incidents = useMemo(
    () =>
      allIncidents.filter((x) => {
        if (deptFilter && x.departmentId !== deptFilter) return false;
        if (typeFilter && x.incidentTypeId !== typeFilter) return false;
        if (transferOnly && !x.isTransfer) return false;
        return true;
      }),
    [allIncidents, deptFilter, typeFilter, transferOnly],
  );

  // Keep an active incident selected (mirrors BackChart's ActiveIncident).
  useEffect(() => {
    if (incidents.length === 0) {
      setActiveIncidentId(null);
      return;
    }
    if (!activeIncidentId || !incidents.some((x) => x.id === activeIncidentId)) {
      setActiveIncidentId(incidents[0].id);
    }
  }, [incidents, activeIncidentId]);

  const activeIncident = useMemo<PatientIncidentListItemDto | null>(
    () => incidents.find((x) => x.id === activeIncidentId) ?? null,
    [incidents, activeIncidentId],
  );

  const fullName = patient
    ? [
        patient.demographics.firstName,
        patient.demographics.middleInitial,
        patient.demographics.lastName,
      ]
        .filter(Boolean)
        .join(" ")
    : "";
```

with:

```tsx
  const patient = patientQuery.data;

  const fullName = patient
    ? [
        patient.demographics.firstName,
        patient.demographics.middleInitial,
        patient.demographics.lastName,
      ]
        .filter(Boolean)
        .join(" ")
    : "";

  // Register/refresh this patient's workspace tab as soon as the patient
  // record loads — covers both "opened from search" (tab already exists,
  // just gets marked active) and a direct/bookmarked chart URL (creates
  // the tab). The cached label avoids a refetch just to render the tab strip.
  useEffect(() => {
    if (!patientId || !patient) return;
    openPatient(patientId, fullName || patient.patientCode);
  }, [patientId, patient, fullName, openPatient]);

  const allIncidents = useMemo(() => incidentsQuery.data?.items ?? [], [incidentsQuery.data]);

  // Client-side refinement (the API filters by closed/deleted; these narrow further).
  const incidents = useMemo(
    () =>
      allIncidents.filter((x) => {
        if (deptFilter && x.departmentId !== deptFilter) return false;
        if (typeFilter && x.incidentTypeId !== typeFilter) return false;
        if (transferOnly && !x.isTransfer) return false;
        return true;
      }),
    [allIncidents, deptFilter, typeFilter, transferOnly],
  );

  // Keep an active incident selected (mirrors BackChart's ActiveIncident).
  // The "which incident is active" pointer now lives in the persisted
  // workspace context (keyed by patientId) instead of local useState, so
  // it survives navigating away from the chart and back.
  useEffect(() => {
    if (!patientId) return;
    if (incidents.length === 0) {
      if (activeIncidentId !== null) setActiveIncident(patientId, null);
      return;
    }
    if (!activeIncidentId || !incidents.some((x) => x.id === activeIncidentId)) {
      setActiveIncident(patientId, incidents[0].id);
    }
  }, [incidents, activeIncidentId, patientId, setActiveIncident]);

  const activeIncident = useMemo<PatientIncidentListItemDto | null>(
    () => incidents.find((x) => x.id === activeIncidentId) ?? null,
    [incidents, activeIncidentId],
  );
```

- [ ] **Step 4: Update the incident row's click handler**

Replace:

```tsx
                    key={incident.id}
                    className={`${DESKTOP_COLS} cursor-pointer ${
                      incident.id === activeIncidentId ? "bg-[var(--color-accent)]" : ""
                    }`}
                    isLast={i === incidents.length - 1}
                    onClick={() => setActiveIncidentId(incident.id)}
```

with:

```tsx
                    key={incident.id}
                    className={`${DESKTOP_COLS} cursor-pointer ${
                      incident.id === activeIncidentId ? "bg-[var(--color-accent)]" : ""
                    }`}
                    isLast={i === incidents.length - 1}
                    onClick={() => patientId && setActiveIncident(patientId, incident.id)}
```

- [ ] **Step 5: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: fails until Task 5 adds `ReportEditorDialog` and this task's remaining `navigate(...)` call sites (createReportMutation, the two IconShortcuts) are converted — those three edits are Task 5's Step 3 below since they're part of the report→dialog conversion. If executing task-by-task strictly, treat Tasks 4 and 5 as one combined checkpoint before running build/lint.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/chart.tsx
git commit -m "$(cat <<'EOF'
refactor(dashboard): chart.tsx reads/writes active incident via workspace context

activeIncidentId moves from local useState to PatientWorkspaceProvider,
keyed by the patientId route param; incident/report *data* fetching is
untouched (still React Query). Registers this patient's workspace tab on
load so the tab strip and sidebar's Patient Chart link pick it up.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: Report editor → dialog

**Files:**
- Rename: `clients/dashboard/src/pages/patient-charts/report-editor.tsx` → `clients/dashboard/src/pages/patient-charts/report-editor-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx` (report call sites + pill row + dialog mount)
- Modify: `clients/dashboard/src/pages/patient-charts/report-search-dialog.tsx`
- Modify: `clients/dashboard/src/routes.tsx` (drop the report-editor route)

**Interfaces:**
- Produces: `ReportEditorDialog({ patientId, reportId, open, onClose }: { patientId: string; reportId: string; open: boolean; onClose: () => void })`.
- Consumes (in chart.tsx / report-search-dialog.tsx): `usePatientWorkspace()`'s `openReport`, `closeReport`, `setActiveReport`.

- [ ] **Step 1: Rename the file**

```bash
git mv clients/dashboard/src/pages/patient-charts/report-editor.tsx clients/dashboard/src/pages/patient-charts/report-editor-dialog.tsx
```

- [ ] **Step 2: Convert `report-editor-dialog.tsx` from a routed page to a dialog**

Edit the imports. Replace:

```tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  CheckCircle2,
  ClipboardList,
  FileDown,
  FileText,
  PenLine,
  Pill,
  Plus,
  Save,
  Stethoscope,
  Tablets,
  UserCheck,
} from "lucide-react";
```

with:

```tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  CheckCircle2,
  ClipboardList,
  FileDown,
  FileText,
  PenLine,
  Pill,
  Plus,
  Save,
  Stethoscope,
  Tablets,
  UserCheck,
} from "lucide-react";
```

Add the Dialog import. Replace:

```tsx
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
```

with:

```tsx
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
```

Change the function signature. Replace:

```tsx
export function ReportEditorPage() {
  const { patientId, reportId } = useParams<{ patientId: string; reportId: string }>();
  const { user } = useAuth();
```

with:

```tsx
export function ReportEditorDialog({
  patientId,
  reportId,
  open,
  onClose,
}: {
  patientId: string;
  reportId: string;
  open: boolean;
  onClose: () => void;
}) {
  const { user } = useAuth();
```

Wrap the loading / not-found early returns in the dialog shell. Replace:

```tsx
  if (reportQuery.isLoading) {
    return <div className="skeleton h-64 rounded-xl" />;
  }

  if (!report) {
    return (
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-6 text-[13px] text-[var(--color-muted-foreground)]">
        Report not found.
      </div>
    );
  }
```

with:

```tsx
  const dialogOnOpenChange = (o: boolean) => {
    if (!o) onClose();
  };

  if (reportQuery.isLoading) {
    return (
      <Dialog open={open} onOpenChange={dialogOnOpenChange}>
        <DialogContent className="!max-w-4xl overflow-hidden p-0">
          <DialogTitle className="sr-only">Loading report…</DialogTitle>
          <div className="max-h-[85vh] overflow-y-auto p-6 pt-10">
            <div className="skeleton h-64 rounded-xl" />
          </div>
        </DialogContent>
      </Dialog>
    );
  }

  if (!report) {
    return (
      <Dialog open={open} onOpenChange={dialogOnOpenChange}>
        <DialogContent className="!max-w-4xl overflow-hidden p-0">
          <DialogTitle className="sr-only">Report not found</DialogTitle>
          <div className="max-h-[85vh] overflow-y-auto p-6 pt-10 text-[13px] text-[var(--color-muted-foreground)]">
            Report not found.
          </div>
        </DialogContent>
      </Dialog>
    );
  }
```

Replace the top of the main return (drop the "Back to Chart" link, open the Dialog wrapper). Replace:

```tsx
  return (
    <div className="space-y-4 sm:space-y-6">
      <Link
        to={`/patient-charts/${patientId}`}
        className="inline-flex items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] hover:text-[var(--color-foreground)]"
      >
        <ArrowLeft className="size-4" />
        Back to Chart
      </Link>

      {/* Patient + status strip */}
```

with:

```tsx
  return (
    <Dialog open={open} onOpenChange={dialogOnOpenChange}>
      <DialogContent className="!max-w-4xl overflow-hidden p-0">
        <DialogTitle className="sr-only">
          {fullName ? `${fullName} — Report` : "Patient report"}
        </DialogTitle>
        <div className="max-h-[85vh] space-y-4 overflow-y-auto p-6 pt-10 sm:space-y-6">
      {/* Patient + status strip */}
```

Close the new wrapper at the end of the function. Replace the final lines:

```tsx
      {patientId && (
        <ImportMedicationsDialog
          patientId={patientId}
          open={medicationsFieldId != null}
          onClose={() => setMedicationsFieldId(null)}
          onDone={(text) => {
            if (medicationsFieldId != null) insertMacro(medicationsFieldId, text);
          }}
        />
      )}
    </div>
  );
}
```

with:

```tsx
      {patientId && (
        <ImportMedicationsDialog
          patientId={patientId}
          open={medicationsFieldId != null}
          onClose={() => setMedicationsFieldId(null)}
          onDone={(text) => {
            if (medicationsFieldId != null) insertMacro(medicationsFieldId, text);
          }}
        />
      )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

> Everything between those two edited regions (header fields, vitals, field sections, associated problems, signature, peer review, addendums, action bar, and the three nested dialogs) is unchanged — `patientId`/`reportId` are now non-optional props instead of possibly-`undefined` route params, so the existing `patientId!`/`reportId!`/`patientId &&`/`reportId &&` guards throughout the body remain valid (redundant in a couple of spots, but harmless — not worth a riskier line-by-line strip in the same change that converts the routing model).

- [ ] **Step 3: Convert chart.tsx's report navigate() calls to workspace-context calls**

Replace the report-create mutation's success handler. Replace:

```tsx
  const createReportMutation = useMutation({
    mutationFn: createReport,
    onSuccess: (reportId) => {
      setPendingReportType(null);
      void queryClient.invalidateQueries({ queryKey: ["reports", activeIncidentId] });
      navigate(`/patient-charts/${patientId}/reports/${reportId}`);
    },
    onError: (err) => toast.error("Failed to create report.", { description: describe(err) }),
  });
```

with:

```tsx
  const createReportMutation = useMutation({
    mutationFn: createReport,
    onSuccess: (reportId) => {
      setPendingReportType(null);
      void queryClient.invalidateQueries({ queryKey: ["reports", activeIncidentId] });
      if (patientId) openReport(patientId, reportId);
    },
    onError: (err) => toast.error("Failed to create report.", { description: describe(err) }),
  });
```

Replace the "View report" / "Edit report" icon shortcuts. Replace:

```tsx
                    <div className="flex items-center gap-1">
                      <IconShortcut
                        label="View report"
                        onClick={() => navigate(`/patient-charts/${patientId}/reports/${r.id}`)}
                      >
                        <Eye className="size-4" />
                      </IconShortcut>
                      {canUpdateReports && !r.isSigned && (
                        <IconShortcut
                          label="Edit report"
                          onClick={() => navigate(`/patient-charts/${patientId}/reports/${r.id}`)}
                        >
                          <Pencil className="size-4" />
                        </IconShortcut>
                      )}
```

with:

```tsx
                    <div className="flex items-center gap-1">
                      <IconShortcut
                        label="View report"
                        onClick={() => patientId && openReport(patientId, r.id)}
                      >
                        <Eye className="size-4" />
                      </IconShortcut>
                      {canUpdateReports && !r.isSigned && (
                        <IconShortcut
                          label="Edit report"
                          onClick={() => patientId && openReport(patientId, r.id)}
                        >
                          <Pencil className="size-4" />
                        </IconShortcut>
                      )}
```

- [ ] **Step 4: Add the "open reports" pill row**

In the Patient Reports card, insert the pill row right before the `{!activeIncident ? (...` conditional. Replace:

```tsx
            {canCreateReports && (
              <DropdownMenu>
```

with:

```tsx
            {canCreateReports && (
              <DropdownMenu>
```

(no change here — the pill row goes further down). Instead, replace the block that opens the reports list:

```tsx
          {!activeIncident ? (
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              Select an incident to view its reports.
            </p>
          ) : reportsQuery.isLoading ? (
```

with:

```tsx
          {openReportIds.length > 0 && (
            <div className="mb-3 flex flex-wrap items-center gap-1.5">
              <span className="text-[11px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                Open reports
              </span>
              {openReportIds.map((id) => {
                const r = reportsQuery.data?.items.find((x) => x.id === id);
                const label = r ? reportTypeLabel(r.reportTypeId) : "Report";
                return (
                  <span
                    key={id}
                    className={cn(
                      "flex items-center gap-1 rounded-full border px-2.5 py-1 text-[11.5px] font-medium",
                      id === activeReportId
                        ? "border-[var(--color-primary)] bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                        : "border-[var(--color-border)] text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)]",
                    )}
                  >
                    <button
                      type="button"
                      onClick={() => patientId && setActiveReport(patientId, id)}
                      className="cursor-pointer"
                    >
                      {label}
                    </button>
                    <button
                      type="button"
                      aria-label={`Close ${label} report tab`}
                      onClick={() => patientId && closeReport(patientId, id)}
                      className="grid size-3.5 place-items-center rounded-full opacity-70 hover:opacity-100"
                    >
                      <X className="size-3" />
                    </button>
                  </span>
                );
              })}
            </div>
          )}

          {!activeIncident ? (
            <p className="text-[12px] text-[var(--color-muted-foreground)]">
              Select an incident to view its reports.
            </p>
          ) : reportsQuery.isLoading ? (
```

- [ ] **Step 5: Mount `ReportEditorDialog`**

Add this block near the end of `chart.tsx`'s JSX, alongside the other conditionally-mounted dialogs (after the `SelectAppointmentDialog` block, before the closing `</div>`):

```tsx
      {/* Report editor dialog — keyed by the workspace context's
          activeReportId. Closing this dialog (X/ESC/overlay click) only
          clears which report is showing; it does NOT remove the report
          from openReportIds — only the pill row's explicit close (×) does. */}
      {patientId && activeReportId && (
        <ReportEditorDialog
          patientId={patientId}
          reportId={activeReportId}
          open
          onClose={() => setActiveReport(patientId, null)}
        />
      )}
```

- [ ] **Step 6: Convert `report-search-dialog.tsx`'s navigate() to the workspace context**

Replace:

```tsx
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { FileSearch, FileText } from "lucide-react";
import type { PatientIncidentListItemDto } from "@/api/incidents";
import { searchPatientReports } from "@/api/reports";
import { listReportTypes } from "@/api/administration";
```

with:

```tsx
import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { FileSearch, FileText } from "lucide-react";
import type { PatientIncidentListItemDto } from "@/api/incidents";
import { searchPatientReports } from "@/api/reports";
import { listReportTypes } from "@/api/administration";
import { usePatientWorkspace } from "@/state/patient-workspace-context";
```

Replace:

```tsx
export function ReportSearchDialog({ open, onClose, incident, incidentTypeLabel }: Props) {
  const navigate = useNavigate();
  const [phrase, setPhrase] = useState("");
```

with:

```tsx
export function ReportSearchDialog({ open, onClose, incident, incidentTypeLabel }: Props) {
  const { openReport: openReportInWorkspace } = usePatientWorkspace();
  const [phrase, setPhrase] = useState("");
```

Replace:

```tsx
  const openReport = (reportId: string) => {
    if (!incident) return;
    onClose();
    navigate(`/patient-charts/${incident.patientId}/reports/${reportId}`);
  };
```

with:

```tsx
  const openReport = (reportId: string) => {
    if (!incident) return;
    onClose();
    openReportInWorkspace(incident.patientId, reportId);
  };
```

- [ ] **Step 7: Drop the report-editor route**

In `clients/dashboard/src/routes.tsx`, remove the lazy import:

```tsx
const ReportEditorPage = lazyNamed(
  () => import("@/pages/patient-charts/report-editor"),
  "ReportEditorPage",
);
```

and remove the route entry:

```tsx
          {
            path: "patient-charts/:patientId/reports/:reportId",
            element: withSuspense(<ReportEditorPage />),
          },
```

> No redirect is added: a repo-wide grep for `reports/${` / `reports/:` found only the four call sites converted above (`chart.tsx` ×3, `report-search-dialog.tsx` ×1) and the route itself — nothing else in the frontend or backend (notifications, audit trail, email templates) builds a deep link to this route.

- [ ] **Step 8: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean — no references to `ReportEditorPage` remain, no unused imports (`Link`/`ArrowLeft` in the dialog file, `useNavigate` in `chart.tsx`/`report-search-dialog.tsx`).

- [ ] **Step 9: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/report-editor-dialog.tsx clients/dashboard/src/pages/patient-charts/chart.tsx clients/dashboard/src/pages/patient-charts/report-search-dialog.tsx clients/dashboard/src/routes.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): report editor becomes a dialog, matching every other chart sub-entity

report-editor.tsx -> report-editor-dialog.tsx, opened from chart.tsx keyed
by the workspace context's activeReportId. Drops the routed
/patient-charts/:patientId/reports/:reportId page (no deep links to it
exist anywhere in the repo). Multiple reports can stay "open" per patient
tab (openReportIds); a pill row switches the one that renders. Closing the
dialog does not remove a report from openReportIds -- only the pill's
explicit close (x) does.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Migrate existing report-editor Playwright specs to the dialog flow

**Files:**
- Create: `clients/dashboard/tests/helpers/workspace-seed.ts`
- Modify: `clients/dashboard/tests/patient-charts/reports.spec.ts`
- Modify: `clients/dashboard/tests/patient-charts/diagnostic-codes.spec.ts`

**Interfaces:**
- Produces: `seedPatientWorkspace(page, tabs: SeededPatientTab[]): Promise<void>`.

- [ ] **Step 1: Add the workspace-seed test helper**

Create `clients/dashboard/tests/helpers/workspace-seed.ts`:

```ts
import type { Page } from "@playwright/test";

/**
 * Mirrors the persisted shape in src/state/patient-workspace-context.tsx.
 * Duplicated here rather than imported — the Playwright test project has
 * no path-alias config for "@/...", matching the existing convention in
 * auth-seed.ts (SeededUser is its own independent type).
 */
export type SeededPatientTab = {
  patientId: string;
  patientLabel: string;
  activeIncidentId?: string | null;
  openReportIds?: string[];
  activeReportId?: string | null;
};

const STORAGE_KEY = "fsh.patientWorkspace.v1";

/**
 * Seed the persistent patient-workspace localStorage key BEFORE React
 * boots, so a test can land directly on a patient's chart — optionally
 * with an incident and/or report dialog already active — without
 * re-driving every click through the UI. Mirrors seedAuthedSession's
 * addInitScript pattern. The last tab in the array becomes the active tab.
 */
export async function seedPatientWorkspace(page: Page, tabs: SeededPatientTab[]): Promise<void> {
  const openTabs = tabs.map((t) => ({
    patientId: t.patientId,
    patientLabel: t.patientLabel,
    activeIncidentId: t.activeIncidentId ?? null,
    openReportIds: t.openReportIds ?? [],
    activeReportId: t.activeReportId ?? null,
  }));
  const state = {
    openTabs,
    activePatientId: openTabs[openTabs.length - 1]?.patientId ?? null,
  };

  await page.addInitScript(
    ({ key, value }) => {
      localStorage.setItem(key, value);
    },
    { key: STORAGE_KEY, value: JSON.stringify(state) },
  );
}
```

- [ ] **Step 2: Rewrite `reports.spec.ts` to open the report through the chart page**

The report editor is no longer routed at `/patient-charts/:patientId/reports/:reportId` — it's a dialog opened from the chart. Seed the workspace with the report already active (avoids re-driving "click View report" in every test) and go to the chart URL instead. Also add the chart-shell mocks the now-fully-rendered chart page needs (incidents, department/incident-type options) that the old bare report route didn't require.

In `clients/dashboard/tests/patient-charts/reports.spec.ts`, add the import and an `INCIDENT` fixture. Replace:

```ts
import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
```

with:

```ts
import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";
```

Add an `INCIDENT` fixture next to the existing `PATIENT` constant. Replace:

```ts
const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: {
    firstName: "Alice",
    middleInitial: "Q",
    lastName: "Vance",
    dateOfBirth: "1990-04-12",
    gender: "F",
  },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};
```

with:

```ts
const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: {
    firstName: "Alice",
    middleInitial: "Q",
    lastName: "Vance",
    dateOfBirth: "1990-04-12",
    gender: "F",
  },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const INCIDENT = {
  id: INCIDENT_ID,
  patientId: PATIENT_ID,
  incidentTypeId: null,
  departmentId: null,
  dateOfInitialVisit: null,
  dateOfLoss: "2026-06-20",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [],
  createdAtUtc: "2026-06-20T08:00:00Z",
  updatedAtUtc: null,
};
```

Extend `mockEditorLookups` with the chart-shell mocks the fully-rendered chart page needs. Replace:

```ts
async function mockEditorLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", REPORT_FIELDS);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", PROVIDERS);
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", CLINICS);
  // The editor's Associated Problems panel loads the patient's problems.
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
}
```

with:

```ts
async function mockEditorLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", REPORT_FIELDS);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", PROVIDERS);
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", CLINICS);
  // The editor's Associated Problems panel loads the patient's problems.
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  // The report editor now renders as a dialog over the chart page, so the
  // chart's own queries (incidents list + its filter option lookups) need
  // mocking too — the old bare report route didn't require these.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

/** Seed the workspace so the chart page opens with the report dialog
 *  already active, then navigate to the chart (not the old report route,
 *  which no longer exists). */
async function gotoReportDialog(page: Page): Promise<void> {
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_ID,
      openReportIds: [REPORT_ID],
      activeReportId: REPORT_ID,
    },
  ]);
  await page.goto(`/patient-charts/${PATIENT_ID}`);
}
```

Replace every `await page.goto(\`/patient-charts/${PATIENT_ID}/reports/${REPORT_ID}\`);` call in the file (6 occurrences) with `await gotoReportDialog(page);`.

Scope the one assertion that would now collide with the chart page's own Patient Info card (both render "Alice Q Vance"). In the first test, replace:

```ts
    await expect(page.getByText("Alice Q Vance")).toBeVisible();
```

with:

```ts
    await expect(page.getByRole("dialog").getByText("Alice Q Vance")).toBeVisible();
```

- [ ] **Step 3: Update `diagnostic-codes.spec.ts`'s two `goto` calls**

`diagnostic-codes.spec.ts` already mocks a matching `INCIDENT` (with `id: INCIDENT_ID`) and the custom-diagnostics/administration lookups the chart page also needs (departments/incident-types are the only gap — its `mockReportEditorLookups` doesn't mock them, but `useDepartmentOptions`/`useIncidentTypeOptions` resolve to `undefined` harmlessly when unmocked, same as any other in-flight query, so this is optional polish, not required for the test to pass). Add the import:

```ts
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";
```

Replace both occurrences of:

```ts
  await page.goto(`/patient-charts/${PATIENT_ID}/reports/${REPORT_ID}`);
```

with:

```ts
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_ID,
      openReportIds: [REPORT_ID],
      activeReportId: REPORT_ID,
    },
  ]);
  await page.goto(`/patient-charts/${PATIENT_ID}`);
```

(one occurrence is inside `openDiagnosticCodesDialog`, the other inside the first `test(...)` body — both get the same replacement.)

- [ ] **Step 4: Run both specs**

Run: `cd clients/dashboard && npx playwright test tests/patient-charts/reports.spec.ts tests/patient-charts/diagnostic-codes.spec.ts`
Expected: PASS. If any assertion still collides with underlying chart-page content now that the dialog renders over a fully-mounted chart (e.g. another duplicated text match), scope it with `page.getByRole("dialog")` the same way Step 2 did — the chart page and the report dialog now coexist in the DOM, which they didn't when the report editor was its own route.

- [ ] **Step 5: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/tests/helpers/workspace-seed.ts clients/dashboard/tests/patient-charts/reports.spec.ts clients/dashboard/tests/patient-charts/diagnostic-codes.spec.ts
git commit -m "$(cat <<'EOF'
test(dashboard): migrate report-editor specs to the dialog flow

Adds seedPatientWorkspace (localStorage init-script seed, mirrors
seedAuthedSession) so specs can land on the chart with a report dialog
already active instead of navigating to the now-removed routed page.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Administration section registry (single source of truth)

**Files:**
- Modify: `clients/dashboard/src/components/layout/nav-data.ts`
- Create: `clients/dashboard/src/pages/administration/section-registry.ts`

**Interfaces:**
- Produces: `administrationHubItems: NavSpec[]` (nav-data.ts), `ADMIN_HUB_SECTIONS: AdminHubSection[]`, `ADMIN_SECTION_COMPONENTS: Record<string, LazyExoticComponent<ComponentType>>` (section-registry.ts).

- [ ] **Step 1: Extract the 17 admin items into a named export**

This is a pure extract-variable refactor — the sidebar still renders exactly the same 17 items afterward; only the data's *name* changes so Task 9 (routes) and Task 10 (sidebar collapse) can both reference it. In `clients/dashboard/src/components/layout/nav-data.ts`, replace:

```ts
export const sections: NavSection[] = [
  {
    id: "patients",
    caption: "Patients",
    icon: Stethoscope,
    items: [
      { to: "/patient-charts", label: "Patient Chart", icon: ClipboardList, perm: INCIDENT_PERMISSIONS.view },
    ],
  },
  {
    id: "scheduling",
    caption: "Scheduling",
    icon: CalendarClock,
    items: [
      {
        to: "/scheduling/appointments",
        label: "Appointments",
        icon: CalendarClock,
        perm: "Permissions.Scheduling.Appointments.View",
      },
    ],
  },
  {
    // Clinic reference data (formerly "Administration"). Tenant-level
    // configuration (Email Settings, Schedule) lives in the Settings
    // section below instead.
    id: "administration",
    caption: "Clinic Setup",
    icon: Building2,
    items: [
      // Each gate mirrors the permission the page's list endpoint enforces
      // server-side (Administration.{Resource}.View). View is IsBasic, so members
      // can reach the page; create/update/delete actions hide for those lacking
      // the manage perms.
      {
        to: "/administration/allergy-reactions",
        label: "Allergy Reactions",
        icon: AlertTriangle,
        perm: "Permissions.Administration.AllergyReactions.View",
      },
      {
        to: "/administration/clinics",
        label: "Clinics",
        icon: Building2,
        perm: "Permissions.Administration.Clinics.View",
      },
      {
        to: "/administration/code-sources",
        label: "Code Sources",
        icon: Tags,
        perm: "Permissions.Administration.CodeSources.View",
      },
      {
        to: "/administration/custom-diagnostics",
        label: "Custom Diagnostics",
        icon: ClipboardPlus,
        perm: "Permissions.Administration.CustomDiagnostics.View",
      },
      {
        to: "/administration/departments",
        label: "Departments",
        icon: Network,
        perm: "Permissions.Administration.Departments.View",
      },
      {
        to: "/administration/diagnostic-categories",
        label: "Diagnostic Categories",
        icon: FolderTree,
        perm: "Permissions.Administration.DiagnosticCategories.View",
      },
      {
        to: "/administration/diagnostics",
        label: "Diagnostic Details",
        icon: Stethoscope,
        perm: "Permissions.Administration.Diagnostics.View",
      },
      {
        to: "/administration/drugs",
        label: "Drugs",
        icon: Pill,
        perm: "Permissions.Administration.Drugs.View",
      },
      {
        to: "/administration/incident-types",
        label: "Incident Types",
        icon: AlertTriangle,
        perm: "Permissions.Administration.IncidentTypes.View",
      },
      {
        to: "/administration/insurance-companies",
        label: "Insurance Companies",
        icon: Building,
        perm: "Permissions.Administration.InsuranceCompanies.View",
      },
      {
        to: "/administration/insurance-types",
        label: "Insurance Types",
        icon: ShieldPlus,
        perm: "Permissions.Administration.InsuranceTypes.View",
      },
      {
        to: "/administration/macros",
        label: "Macros",
        icon: ScrollText,
        perm: "Permissions.Administration.Macros.View",
      },
      {
        to: "/administration/medication-dose-units",
        label: "Medication Dose Units",
        icon: Beaker,
        perm: "Permissions.Administration.MedicationDoseUnits.View",
      },
      {
        to: "/administration/patient-document-types",
        label: "Patient Document Types",
        icon: FileText,
        perm: "Permissions.Administration.PatientDocumentTypes.View",
      },
      {
        to: "/administration/procedure-categories",
        label: "Procedure Categories",
        icon: Layers,
        perm: "Permissions.Administration.ProcedureCategories.View",
      },
      {
        to: "/administration/procedure-codes",
        label: "Procedure Codes",
        icon: ListChecks,
        perm: "Permissions.Administration.ProcedureCodes.View",
      },
      {
        to: "/administration/providers",
        label: "Providers",
        icon: Stethoscope,
        perm: "Permissions.Administration.Providers.View",
      },
    ],
  },
```

with:

```ts
/**
 * The 17 Administration ("Clinic Setup") entries — single source of truth
 * for labels/icons/permission strings, consumed by both this file's
 * `sections` (the sidebar) and the Administration hub page
 * (`pages/administration/section-registry.ts`, which maps each `to`
 * path's last segment to its lazy page component). Task 10 collapses the
 * sidebar's own rendering of these to a single "Administration" entry
 * pointing at the hub; this array keeps backing the hub regardless.
 */
export const administrationHubItems: NavSpec[] = [
  // Each gate mirrors the permission the page's list endpoint enforces
  // server-side (Administration.{Resource}.View). View is IsBasic, so members
  // can reach the page; create/update/delete actions hide for those lacking
  // the manage perms.
  {
    to: "/administration/allergy-reactions",
    label: "Allergy Reactions",
    icon: AlertTriangle,
    perm: "Permissions.Administration.AllergyReactions.View",
  },
  {
    to: "/administration/clinics",
    label: "Clinics",
    icon: Building2,
    perm: "Permissions.Administration.Clinics.View",
  },
  {
    to: "/administration/code-sources",
    label: "Code Sources",
    icon: Tags,
    perm: "Permissions.Administration.CodeSources.View",
  },
  {
    to: "/administration/custom-diagnostics",
    label: "Custom Diagnostics",
    icon: ClipboardPlus,
    perm: "Permissions.Administration.CustomDiagnostics.View",
  },
  {
    to: "/administration/departments",
    label: "Departments",
    icon: Network,
    perm: "Permissions.Administration.Departments.View",
  },
  {
    to: "/administration/diagnostic-categories",
    label: "Diagnostic Categories",
    icon: FolderTree,
    perm: "Permissions.Administration.DiagnosticCategories.View",
  },
  {
    to: "/administration/diagnostics",
    label: "Diagnostic Details",
    icon: Stethoscope,
    perm: "Permissions.Administration.Diagnostics.View",
  },
  {
    to: "/administration/drugs",
    label: "Drugs",
    icon: Pill,
    perm: "Permissions.Administration.Drugs.View",
  },
  {
    to: "/administration/incident-types",
    label: "Incident Types",
    icon: AlertTriangle,
    perm: "Permissions.Administration.IncidentTypes.View",
  },
  {
    to: "/administration/insurance-companies",
    label: "Insurance Companies",
    icon: Building,
    perm: "Permissions.Administration.InsuranceCompanies.View",
  },
  {
    to: "/administration/insurance-types",
    label: "Insurance Types",
    icon: ShieldPlus,
    perm: "Permissions.Administration.InsuranceTypes.View",
  },
  {
    to: "/administration/macros",
    label: "Macros",
    icon: ScrollText,
    perm: "Permissions.Administration.Macros.View",
  },
  {
    to: "/administration/medication-dose-units",
    label: "Medication Dose Units",
    icon: Beaker,
    perm: "Permissions.Administration.MedicationDoseUnits.View",
  },
  {
    to: "/administration/patient-document-types",
    label: "Patient Document Types",
    icon: FileText,
    perm: "Permissions.Administration.PatientDocumentTypes.View",
  },
  {
    to: "/administration/procedure-categories",
    label: "Procedure Categories",
    icon: Layers,
    perm: "Permissions.Administration.ProcedureCategories.View",
  },
  {
    to: "/administration/procedure-codes",
    label: "Procedure Codes",
    icon: ListChecks,
    perm: "Permissions.Administration.ProcedureCodes.View",
  },
  {
    to: "/administration/providers",
    label: "Providers",
    icon: Stethoscope,
    perm: "Permissions.Administration.Providers.View",
  },
];

export const sections: NavSection[] = [
  {
    id: "patients",
    caption: "Patients",
    icon: Stethoscope,
    items: [
      { to: "/patient-charts", label: "Patient Chart", icon: ClipboardList, perm: INCIDENT_PERMISSIONS.view },
    ],
  },
  {
    id: "scheduling",
    caption: "Scheduling",
    icon: CalendarClock,
    items: [
      {
        to: "/scheduling/appointments",
        label: "Appointments",
        icon: CalendarClock,
        perm: "Permissions.Scheduling.Appointments.View",
      },
    ],
  },
  {
    // Clinic reference data (formerly "Administration"). Tenant-level
    // configuration (Email Settings, Schedule) lives in the Settings
    // section below instead. Task 10 collapses `items` to a single
    // "Administration" hub link — until then this still renders all 17.
    id: "administration",
    caption: "Clinic Setup",
    icon: Building2,
    items: administrationHubItems,
  },
```

(the rest of the `sections` array — `helpdesk`, `identity`, `operations`, `settings`, `system` — is unchanged).

- [ ] **Step 2: Create the section registry**

Create `clients/dashboard/src/pages/administration/section-registry.ts`:

```ts
import { lazy, type ComponentType, type LazyExoticComponent } from "react";
import { administrationHubItems } from "@/components/layout/nav-data";

function lazyNamed<T extends Record<string, unknown>, K extends keyof T>(
  importer: () => Promise<T>,
  name: K,
): LazyExoticComponent<ComponentType> {
  return lazy(async () => {
    const mod = await importer();
    return { default: mod[name] as ComponentType<unknown> };
  });
}

/** slug (last path segment of each administrationHubItems `to`) -> lazy
 *  page component. Single place that decides which existing Administration
 *  page backs each hub card / dialog / `/administration/:section` deep link. */
export const ADMIN_SECTION_COMPONENTS: Record<string, LazyExoticComponent<ComponentType>> = {
  "allergy-reactions": lazyNamed(
    () => import("@/pages/administration/allergy-reactions"),
    "AllergyReactionsPage",
  ),
  clinics: lazyNamed(() => import("@/pages/administration/clinics"), "ClinicsPage"),
  "code-sources": lazyNamed(() => import("@/pages/administration/code-sources"), "CodeSourcesPage"),
  "custom-diagnostics": lazyNamed(
    () => import("@/pages/administration/custom-diagnostics"),
    "CustomDiagnosticsPage",
  ),
  departments: lazyNamed(() => import("@/pages/administration/departments"), "DepartmentsPage"),
  "diagnostic-categories": lazyNamed(
    () => import("@/pages/administration/diagnostic-categories"),
    "DiagnosticCategoriesPage",
  ),
  diagnostics: lazyNamed(() => import("@/pages/administration/diagnostics"), "DiagnosticsPage"),
  drugs: lazyNamed(() => import("@/pages/administration/drugs"), "DrugsPage"),
  "incident-types": lazyNamed(
    () => import("@/pages/administration/incident-types"),
    "IncidentTypesPage",
  ),
  "insurance-companies": lazyNamed(
    () => import("@/pages/administration/insurance-companies"),
    "InsuranceCompaniesPage",
  ),
  "insurance-types": lazyNamed(
    () => import("@/pages/administration/insurance-types"),
    "InsuranceTypesPage",
  ),
  macros: lazyNamed(() => import("@/pages/administration/macros"), "MacrosPage"),
  "medication-dose-units": lazyNamed(
    () => import("@/pages/administration/medication-dose-units"),
    "MedicationDoseUnitsPage",
  ),
  "patient-document-types": lazyNamed(
    () => import("@/pages/administration/patient-document-types"),
    "PatientDocumentTypesPage",
  ),
  "procedure-categories": lazyNamed(
    () => import("@/pages/administration/procedure-categories"),
    "ProcedureCategoriesPage",
  ),
  "procedure-codes": lazyNamed(
    () => import("@/pages/administration/procedure-codes"),
    "ProcedureCodesPage",
  ),
  providers: lazyNamed(() => import("@/pages/administration/providers"), "ProvidersPage"),
};

export type AdminHubSection = {
  slug: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  perm?: string;
};

/** The 17 Administration hub entries, derived from nav-data.ts's
 *  `administrationHubItems` (single source of truth for labels/icons/
 *  permission strings — Email Settings and Schedule live in the Settings
 *  section, not here, and are unaffected by this feature). */
export const ADMIN_HUB_SECTIONS: AdminHubSection[] = administrationHubItems.map((item) => ({
  slug: item.to.replace("/administration/", ""),
  label: item.label,
  icon: item.icon,
  perm: item.perm,
}));
```

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean — `sections` still renders all 17 items exactly as before (behavior-neutral extraction); `section-registry.ts` isn't imported by anything yet.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/components/layout/nav-data.ts clients/dashboard/src/pages/administration/section-registry.ts
git commit -m "$(cat <<'EOF'
refactor(dashboard): extract administrationHubItems + add the section registry

nav-data.ts: pure extract-variable, sidebar behavior unchanged (still 17
items). New section-registry.ts maps each item's slug to its lazy page
component -- single source of truth the Administration hub (Task 8) and
routes.tsx (Task 9) both consume.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: Administration hub page + `AdminSectionDialog`

**Files:**
- Create: `clients/dashboard/src/pages/administration/admin-section-dialog.tsx`
- Create: `clients/dashboard/src/pages/administration/hub.tsx`

**Interfaces:**
- Consumes: `ADMIN_HUB_SECTIONS`, `ADMIN_SECTION_COMPONENTS` (Task 7).
- Produces: `AdminSectionDialog({ slug, onClose }: { slug: string | null; onClose: () => void })`, `AdministrationHub()`.

- [ ] **Step 1: Create the generic dialog shell**

Create `clients/dashboard/src/pages/administration/admin-section-dialog.tsx`:

```tsx
import { Suspense } from "react";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import {
  ADMIN_HUB_SECTIONS,
  ADMIN_SECTION_COMPONENTS,
} from "@/pages/administration/section-registry";

function SectionFallback() {
  return (
    <div className="space-y-4 p-2" role="status" aria-busy="true">
      <span className="sr-only">Loading…</span>
      <Skeleton className="h-8 w-48" />
      <Skeleton className="h-40 w-full rounded-xl" />
    </div>
  );
}

/**
 * Generic dialog shell — lazy-loads and renders an EXISTING Administration
 * page component (ClinicsPage, DepartmentsPage, etc.) inside a DialogContent
 * instead of routing to a bare page. Every admin page is already a
 * self-contained list+CRUD unit on shared EntityListCard/Dialog primitives,
 * so this is a thin wrapper, not a rewrite.
 */
export function AdminSectionDialog({
  slug,
  onClose,
}: {
  /** Section slug (e.g. "clinics"), or null when nothing should be open. */
  slug: string | null;
  onClose: () => void;
}) {
  const section = slug ? ADMIN_HUB_SECTIONS.find((s) => s.slug === slug) : undefined;
  const Section = slug ? ADMIN_SECTION_COMPONENTS[slug] : null;

  return (
    <Dialog open={slug != null} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-4xl overflow-hidden p-0">
        <DialogTitle className="sr-only">{section?.label ?? "Administration"}</DialogTitle>
        <div className="max-h-[85vh] overflow-y-auto p-6 pt-10">
          {Section && (
            <Suspense fallback={<SectionFallback />}>
              <Section />
            </Suspense>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 2: Create the hub page**

Create `clients/dashboard/src/pages/administration/hub.tsx`:

```tsx
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Building2 } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { EntityPageHeader } from "@/components/list";
import { AdminSectionDialog } from "@/pages/administration/admin-section-dialog";
import { ADMIN_HUB_SECTIONS } from "@/pages/administration/section-registry";
import { cn } from "@/lib/cn";

/**
 * Administration hub — a grid of the 17 existing Administration sections.
 * Clicking a card opens that section's existing page inside a dialog
 * layered over this grid (AdminSectionDialog) instead of navigating to a
 * bare page, so a patient tab strip open above the AppShell's Outlet stays
 * visible underneath. Also backs the legacy `/administration/:section`
 * deep-link routes (routes.tsx points both `/administration` and
 * `/administration/:section` at this same component) — the optional
 * `:section` param opens the matching dialog on mount.
 */
export function AdministrationHub() {
  const { section } = useParams<{ section?: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const perms = user?.permissions ?? [];

  const [openSlug, setOpenSlug] = useState<string | null>(section ?? null);

  // Keep the open dialog in sync with the :section route param — covers
  // direct deep links (/administration/clinics) and browser back/forward.
  useEffect(() => {
    setOpenSlug(section ?? null);
  }, [section]);

  const visibleCards = ADMIN_HUB_SECTIONS.filter((s) => !s.perm || perms.includes(s.perm));

  const openSection = (slug: string) => {
    setOpenSlug(slug);
    navigate(`/administration/${slug}`, { replace: true });
  };

  const closeSection = () => {
    setOpenSlug(null);
    navigate("/administration", { replace: true });
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Building2}
        title="Administration"
        total={visibleCards.length}
        unit="section"
        description="Clinic reference data — clinics, providers, diagnostics, drugs, and more. Choose a section to manage its records."
      />

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {visibleCards.map((s) => {
          const Icon = s.icon;
          return (
            <button
              key={s.slug}
              type="button"
              onClick={() => openSection(s.slug)}
              className={cn(
                "flex items-center gap-3 rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-left",
                "transition-colors hover:bg-[var(--color-accent)]",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              )}
            >
              <span className="grid size-10 shrink-0 place-items-center rounded-lg bg-[var(--color-primary-soft)] text-[var(--color-primary)]">
                <Icon className="size-5" />
              </span>
              <span className="text-[14px] font-medium">{s.label}</span>
            </button>
          );
        })}
      </div>

      <AdminSectionDialog slug={openSlug} onClose={closeSection} />
    </div>
  );
}
```

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean. Nothing routes to `hub.tsx` yet — wired in Task 9.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/pages/administration/admin-section-dialog.tsx clients/dashboard/src/pages/administration/hub.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): Administration hub page + generic AdminSectionDialog

Hub renders the 17 sections as cards (permission-gated); AdminSectionDialog
lazy-loads and renders the EXISTING page component for a given slug inside
a DialogContent. No admin CRUD logic changes -- pure presentation wrapper.
Not yet routed -- Task 9 wires routes.tsx.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 9: `routes.tsx` — collapse the 17 admin routes into the hub

**Files:**
- Modify: `clients/dashboard/src/routes.tsx`

**Interfaces:**
- Consumes: `AdministrationHub` (Task 8).

- [ ] **Step 1: Replace the 17 lazy consts + redirect with one hub lazy const**

Remove the 17 individual lazy imports:

```tsx
const ClinicsPage = lazyNamed(
  () => import("@/pages/administration/clinics"),
  "ClinicsPage",
);
const DepartmentsPage = lazyNamed(
  () => import("@/pages/administration/departments"),
  "DepartmentsPage",
);
const ProvidersPage = lazyNamed(
  () => import("@/pages/administration/providers"),
  "ProvidersPage",
);
const InsuranceTypesPage = lazyNamed(
  () => import("@/pages/administration/insurance-types"),
  "InsuranceTypesPage",
);
const InsuranceCompaniesPage = lazyNamed(
  () => import("@/pages/administration/insurance-companies"),
  "InsuranceCompaniesPage",
);
const DiagnosticCategoriesPage = lazyNamed(
  () => import("@/pages/administration/diagnostic-categories"),
  "DiagnosticCategoriesPage",
);
const IncidentTypesPage = lazyNamed(
  () => import("@/pages/administration/incident-types"),
  "IncidentTypesPage",
);
const PatientDocumentTypesPage = lazyNamed(
  () => import("@/pages/administration/patient-document-types"),
  "PatientDocumentTypesPage",
);
const MacrosPage = lazyNamed(
  () => import("@/pages/administration/macros"),
  "MacrosPage",
);
const EmailSettingsPage = lazyNamed(
  () => import("@/pages/administration/email-settings"),
  "EmailSettingsPage",
);
const SchedulePage = lazyNamed(
  () => import("@/pages/administration/schedule"),
  "SchedulePage",
);
const CustomDiagnosticsPage = lazyNamed(
  () => import("@/pages/administration/custom-diagnostics"),
  "CustomDiagnosticsPage",
);
const DiagnosticsPage = lazyNamed(
  () => import("@/pages/administration/diagnostics"),
  "DiagnosticsPage",
);
const DrugsPage = lazyNamed(() => import("@/pages/administration/drugs"), "DrugsPage");
const AllergyReactionsPage = lazyNamed(
  () => import("@/pages/administration/allergy-reactions"),
  "AllergyReactionsPage",
);
const MedicationDoseUnitsPage = lazyNamed(
  () => import("@/pages/administration/medication-dose-units"),
  "MedicationDoseUnitsPage",
);
const ProcedureCategoriesPage = lazyNamed(
  () => import("@/pages/administration/procedure-categories"),
  "ProcedureCategoriesPage",
);
const ProcedureCodesPage = lazyNamed(
  () => import("@/pages/administration/procedure-codes"),
  "ProcedureCodesPage",
);
const CodeSourcesPage = lazyNamed(
  () => import("@/pages/administration/code-sources"),
  "CodeSourcesPage",
);
```

with (keep `EmailSettingsPage` and `SchedulePage` — those two live in the Settings section, not the hub, and are untouched by this feature):

```tsx
const EmailSettingsPage = lazyNamed(
  () => import("@/pages/administration/email-settings"),
  "EmailSettingsPage",
);
const SchedulePage = lazyNamed(
  () => import("@/pages/administration/schedule"),
  "SchedulePage",
);
const AdministrationHub = lazyNamed(() => import("@/pages/administration/hub"), "AdministrationHub");
```

- [ ] **Step 2: Replace the 17 admin routes + redirect with two hub routes**

Replace:

```tsx
          { path: "administration", element: <Navigate to="/administration/clinics" replace /> },
          { path: "administration/clinics", element: withSuspense(<ClinicsPage />) },
          { path: "administration/departments", element: withSuspense(<DepartmentsPage />) },
          { path: "administration/providers", element: withSuspense(<ProvidersPage />) },
          { path: "administration/insurance-types", element: withSuspense(<InsuranceTypesPage />) },
          { path: "administration/insurance-companies", element: withSuspense(<InsuranceCompaniesPage />) },
          { path: "administration/diagnostic-categories", element: withSuspense(<DiagnosticCategoriesPage />) },
          { path: "administration/custom-diagnostics", element: withSuspense(<CustomDiagnosticsPage />) },
          { path: "administration/diagnostics", element: withSuspense(<DiagnosticsPage />) },
          { path: "administration/drugs", element: withSuspense(<DrugsPage />) },
          { path: "administration/allergy-reactions", element: withSuspense(<AllergyReactionsPage />) },
          { path: "administration/medication-dose-units", element: withSuspense(<MedicationDoseUnitsPage />) },
          { path: "administration/procedure-categories", element: withSuspense(<ProcedureCategoriesPage />) },
          { path: "administration/procedure-codes", element: withSuspense(<ProcedureCodesPage />) },
          { path: "administration/code-sources", element: withSuspense(<CodeSourcesPage />) },
          { path: "administration/incident-types", element: withSuspense(<IncidentTypesPage />) },
          { path: "administration/patient-document-types", element: withSuspense(<PatientDocumentTypesPage />) },
          { path: "administration/macros", element: withSuspense(<MacrosPage />) },
          { path: "administration/schedule", element: withSuspense(<SchedulePage />) },
          { path: "administration/email-settings", element: withSuspense(<EmailSettingsPage />) },
```

with:

```tsx
          { path: "administration", element: withSuspense(<AdministrationHub />) },
          { path: "administration/schedule", element: withSuspense(<SchedulePage />) },
          { path: "administration/email-settings", element: withSuspense(<EmailSettingsPage />) },
          { path: "administration/:section", element: withSuspense(<AdministrationHub />) },
```

> `administration/:section` is registered AFTER the two literal sibling routes (`schedule`, `email-settings`) so those keep resolving to their own dedicated pages rather than being swallowed by the `:section` wildcard — React Router matches static segments before dynamic ones regardless of declaration order, but keeping the literal routes textually first documents the precedence for readers.

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean — no dangling references to the removed 15 lazy consts (`ClinicsPage` etc. — `EmailSettingsPage`/`SchedulePage` are kept).

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/routes.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): route /administration and /administration/:section to the hub

Both routes render AdministrationHub; the optional :section param opens
that section's AdminSectionDialog on mount, so legacy deep links
(/administration/clinics, etc.) keep working but now render as a dialog
over the hub grid instead of a bare page. Email Settings and Schedule are
unaffected (Settings-section pages, not part of the hub).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 10: Sidebar nav-data — collapse "Clinic Setup" to a single "Administration" entry

**Files:**
- Modify: `clients/dashboard/src/components/layout/nav-data.ts`

**Interfaces:**
- Consumes: `administrationHubItems` (Task 7).

- [ ] **Step 1: Add the combined any-permission list + collapse the sidebar section**

Mirror the existing Trash-entry convention (`anyPerm: ALL_TRASH_PERMISSIONS` — "show if the user can reach any one tab"). Replace:

```ts
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";
import { INCIDENT_PERMISSIONS } from "@/lib/patient-permissions";
```

with:

```ts
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";
import { INCIDENT_PERMISSIONS } from "@/lib/patient-permissions";
```

(no change to the import block itself — the helper below is derived from `administrationHubItems`, already in this file).

Replace the "administration" section entry in `sections`:

```ts
    // Clinic reference data (formerly "Administration"). Tenant-level
    // configuration (Email Settings, Schedule) lives in the Settings
    // section below instead. Task 10 collapses `items` to a single
    // "Administration" hub link — until then this still renders all 17.
    id: "administration",
    caption: "Clinic Setup",
    icon: Building2,
    items: administrationHubItems,
  },
```

with:

```ts
    // Clinic reference data (formerly "Administration"). Tenant-level
    // configuration (Email Settings, Schedule) lives in the Settings
    // section below instead. Collapsed to a single hub link — the 17
    // section-level permission checks now gate which hub CARDS render
    // (see pages/administration/hub.tsx) instead of gating nav items.
    id: "administration",
    caption: "Clinic Setup",
    icon: Building2,
    items: [
      {
        to: "/administration",
        label: "Administration",
        icon: Building2,
        anyPerm: ALL_ADMINISTRATION_PERMISSIONS,
      },
    ],
  },
```

Add the `ALL_ADMINISTRATION_PERMISSIONS` constant right above `sections`, next to `administrationHubItems`:

```ts
/** Any one of these grants the sidebar's single "Administration" entry —
 *  mirrors ALL_TRASH_PERMISSIONS' "show if the user can reach any tab"
 *  convention. Individual card visibility inside the hub is still gated
 *  per-section (see ADMIN_HUB_SECTIONS in section-registry.ts). */
const ALL_ADMINISTRATION_PERMISSIONS: string[] = administrationHubItems
  .map((item) => item.perm)
  .filter((p): p is string => !!p);

export const sections: NavSection[] = [
```

- [ ] **Step 2: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/components/layout/nav-data.ts
git commit -m "$(cat <<'EOF'
feat(dashboard): collapse Clinic Setup nav section to a single Administration entry

Mirrors the Patients-section simplification from the 2026-06-30 patient
chart search change. Visible if the user holds any one of the 17
Administration.*.View permissions (ALL_ADMINISTRATION_PERMISSIONS, same
anyPerm convention as Trash); per-section gating moves to which hub cards
render.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 11: New Playwright coverage

**Files:**
- Create: `clients/dashboard/tests/layout/patient-tabs.spec.ts`
- Create: `clients/dashboard/tests/patient-charts/report-tabs.spec.ts`
- Create: `clients/dashboard/tests/administration/hub.spec.ts`

**Interfaces:**
- Consumes: `seedPatientWorkspace` (Task 6), `seedAuthedSession`, `installShellMocks`, `mockJsonResponse`, `paged`.

- [ ] **Step 1: Tab persistence + explicit close**

Create `clients/dashboard/tests/layout/patient-tabs.spec.ts`:

```ts
// E2E coverage for the persistent patient tab strip: opening a patient
// from search survives a full page reload (localStorage rehydration —
// AppShell's PatientWorkspaceProvider persists above the routed Outlet),
// and an explicit close (x) removes the tab and falls back to search.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const ALICE_ID = "00000000-0000-0000-0000-0000000a1111";
const BOB_ID = "00000000-0000-0000-0000-0000000b2222";

const ALICE = {
  id: ALICE_ID,
  patientCode: "P-10293",
  firstName: "Alice",
  lastName: "Vance",
  middleInitial: "Q",
  dateOfBirth: "1990-04-12",
  gender: "F",
  email: "alice.vance@example.com",
  phone: "555-0101",
  isActive: true,
  lastVisitDate: null,
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: null,
};

const ALICE_DETAIL = {
  id: ALICE_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

async function mockChartLookups(page: import("@playwright/test").Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + ALICE_ID, ALICE_DETAIL);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

test.describe("persistent patient tabs", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("opening a patient chart adds a tab that survives a full reload", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await mockChartLookups(page);

    await page.goto("/patient-charts");
    await page.getByPlaceholder(/search by name or patient code/i).fill("Vance");
    await page.getByRole("link", { name: /open chart for alice q vance/i }).first().click();
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${ALICE_ID}$`));

    await expect(page.getByRole("tab", { name: /alice/i })).toBeVisible();

    // Full navigation (not client-side) — proves the tab survived via
    // localStorage rehydration on AppShell mount, not just in-memory state.
    await page.goto("/");
    await expect(page.getByRole("tab", { name: /alice/i })).toBeVisible();

    await page.getByRole("tab", { name: /alice/i }).click();
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${ALICE_ID}$`));
  });

  test("closing a tab removes it and falls back to the search page", async ({ page }) => {
    await seedPatientWorkspace(page, [{ patientId: ALICE_ID, patientLabel: "Alice Q Vance" }]);
    await mockChartLookups(page);
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));

    await page.goto(`/patient-charts/${ALICE_ID}`);
    await expect(page.getByRole("tab", { name: /alice/i })).toBeVisible();

    await page.getByRole("button", { name: /close alice q vance's chart tab/i }).click();

    await expect(page).toHaveURL(/\/patient-charts$/);
    await expect(page.getByRole("tab", { name: /alice/i })).toHaveCount(0);
  });

  test("a second patient opens a second tab; the sidebar Patient Chart link follows the active one", async ({
    page,
  }) => {
    await seedPatientWorkspace(page, [
      { patientId: ALICE_ID, patientLabel: "Alice Q Vance" },
      { patientId: BOB_ID, patientLabel: "Bob R Diaz" },
    ]);
    await mockChartLookups(page);
    await mockJsonResponse(page, "**/api/v1/patient/patients/" + BOB_ID, {
      ...ALICE_DETAIL,
      id: BOB_ID,
      patientCode: "P-10294",
      demographics: { firstName: "Bob", middleInitial: "R", lastName: "Diaz", dateOfBirth: "1985-02-01", gender: "M" },
    });

    await page.goto(`/patient-charts/${BOB_ID}`);
    await expect(page.getByRole("tab", { name: /alice/i })).toBeVisible();
    await expect(page.getByRole("tab", { name: /bob/i })).toBeVisible();

    // Sidebar's Patient Chart entry resolves to whichever patient is active (Bob).
    await page.getByRole("link", { name: "Patient Chart" }).first().click();
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${BOB_ID}$`));
  });
});
```

- [ ] **Step 2: Report dialog switch + explicit close**

Create `clients/dashboard/tests/patient-charts/report-tabs.spec.ts`:

```ts
// E2E coverage for the report dialog's "open reports" pill row: switching
// the active report between two open ones, and the distinction between
// closing the dialog (keeps the pill) and explicitly closing a pill
// (removes it from openReportIds).

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const REPORT_PERMS = ["Permissions.Patient.Reports.View", "Permissions.Patient.Reports.Update"];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3333";
const REPORT_1 = "00000000-0000-0000-0000-0000000d4444";
const REPORT_2 = "00000000-0000-0000-0000-0000000d5555";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const INCIDENT = {
  id: INCIDENT_ID,
  patientId: PATIENT_ID,
  incidentTypeId: null,
  departmentId: null,
  dateOfInitialVisit: null,
  dateOfLoss: "2026-06-20",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [],
  createdAtUtc: "2026-06-20T08:00:00Z",
  updatedAtUtc: null,
};

function reportOfType(id: string, reportTypeId: number) {
  return {
    id,
    incidentId: INCIDENT_ID,
    patientId: PATIENT_ID,
    reportTypeId,
    reportDate: "2026-06-26T00:00:00Z",
    version: 1,
    providerId: null,
    clinicId: null,
    isNoShow: false,
    vitals: { heightInches: null, weightLbs: null, bmi: null, systolic: null, diastolic: null, pulse: null, temperatureF: null },
    workflowStatus: "Draft",
    isSigned: false,
    signedByUserId: null,
    signedByName: null,
    signedOnUtc: null,
    signatureImagePath: null,
    signatureImageUrl: null,
    reviewRequestedByUserId: null,
    reviewRequestedOnUtc: null,
    reviewerProviderId: null,
    reviewSignedByUserId: null,
    reviewSignedByName: null,
    reviewSignedOnUtc: null,
    reviewSignatureImagePath: null,
    reviewSignatureImageUrl: null,
    fieldValues: [],
    addendums: [],
    associatedProblemIds: [],
    createdAtUtc: "2026-06-26T08:00:00Z",
    updatedAtUtc: null,
  };
}

const REPORT_TYPES = [
  { id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true },
  { id: 2, name: "Progress Note", displayOrder: 1, isActive: true },
];

async function mockLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", []);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  await mockJsonResponse(
    page,
    "**/api/v1/patient/reports**",
    paged([reportOfType(REPORT_1, 1), reportOfType(REPORT_2, 2)]),
  );
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_1, reportOfType(REPORT_1, 1));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_2, reportOfType(REPORT_2, 2));
}

test.describe("report dialog — open-reports pill row", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", REPORT_PERMS);
    await mockLookups(page);
  });

  test("two open reports show as pills; switching pills swaps the active dialog", async ({ page }) => {
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_ID,
        openReportIds: [REPORT_1, REPORT_2],
        activeReportId: REPORT_1,
      },
    ]);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByText("Initial Evaluation")).toBeVisible();

    await page.getByRole("button", { name: "Progress Note" }).click();
    await expect(dialog.getByText("Progress Note")).toBeVisible();
  });

  test("closing the dialog keeps the pill; the pill's own close removes it", async ({ page }) => {
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_ID,
        openReportIds: [REPORT_1, REPORT_2],
        activeReportId: REPORT_1,
      },
    ]);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Close the dialog itself (X button) — the pill for REPORT_1 must remain.
    await page.getByRole("dialog").getByRole("button", { name: "Close" }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Initial Evaluation" })).toBeVisible();

    // Explicitly close the Progress Note pill — it disappears from the row.
    await page.getByRole("button", { name: "Close Progress Note report tab" }).click();
    await expect(page.getByRole("button", { name: "Progress Note" })).toHaveCount(0);
    await expect(page.getByRole("button", { name: "Initial Evaluation" })).toBeVisible();
  });
});
```

- [ ] **Step 3: Administration hub → dialog → CRUD with a patient tab visible underneath**

Create `clients/dashboard/tests/administration/hub.spec.ts`:

```ts
// E2E coverage for the Administration hub: card grid, opening a section as
// a dialog (from the hub AND via a legacy /administration/:section deep
// link), and — the key persistent-workspace interaction — an open patient
// tab stays visible in the tab strip while an admin dialog is open on top.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const ADMIN_PERMS = [
  "Permissions.Administration.Clinics.View",
  "Permissions.Administration.Providers.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";

const CLINIC = {
  id: "clinic-1",
  code: "MAIN",
  name: "Main Clinic",
  address1: "123 Main St",
  address2: null,
  city: "Springfield",
  state: "IL",
  zip: "62704",
  phone: null,
  timeZoneId: "UTC",
  isActive: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: null,
};

test.describe("administration hub", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", ADMIN_PERMS);
  });

  test("lists only permitted sections and opens one as a dialog", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([CLINIC]));

    await page.goto("/administration");
    await expect(page.getByRole("heading", { name: "Administration", level: 1 })).toBeVisible();
    await expect(page.getByRole("button", { name: "Clinics" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Providers" })).toBeVisible();
    // Not permitted (no Departments.View grant) — must not render.
    await expect(page.getByRole("button", { name: "Departments" })).toHaveCount(0);

    await page.getByRole("button", { name: "Clinics" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByText("Main Clinic")).toBeVisible();
    await expect(page).toHaveURL(/\/administration\/clinics$/);
  });

  test("a direct deep link to /administration/clinics opens the dialog over the hub", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([CLINIC]));

    await page.goto("/administration/clinics");

    // The hub grid is still mounted behind the dialog.
    await expect(page.getByRole("heading", { name: "Administration", level: 1 })).toBeVisible();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Clinics" })).toBeVisible();
  });

  test("an open patient tab stays visible while an admin section dialog is open", async ({ page }) => {
    await seedPatientWorkspace(page, [{ patientId: PATIENT_ID, patientLabel: "Alice Q Vance" }]);
    await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([CLINIC]));

    await page.goto("/administration/clinics");

    await expect(page.getByRole("tab", { name: /alice/i })).toBeVisible();
    await expect(page.getByRole("dialog").getByRole("heading", { name: "Clinics" })).toBeVisible();
  });
});
```

- [ ] **Step 4: Run the three new specs**

Run: `cd clients/dashboard && npx playwright test tests/layout/patient-tabs.spec.ts tests/patient-charts/report-tabs.spec.ts tests/administration/hub.spec.ts`
Expected: PASS. If a card/pill/tab accessible name doesn't match what the earlier tasks actually rendered (e.g. the tab's accessible name composition, or `DialogClose`'s default "Close" label), adjust the selector to match the real DOM rather than the component — these three specs are new and have no prior passing baseline to preserve.

- [ ] **Step 5: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/tests/layout/patient-tabs.spec.ts clients/dashboard/tests/patient-charts/report-tabs.spec.ts clients/dashboard/tests/administration/hub.spec.ts
git commit -m "$(cat <<'EOF'
test(dashboard): E2E coverage for persistent tabs, report pills, and the Administration hub

Tab persistence across a full reload (proves localStorage rehydration,
not just in-memory state survival across client-side nav); explicit close
removing a tab; report dialog pill switching + close-dialog-vs-close-pill
distinction; Administration hub card grid + permission gating + dialog
deep links; a patient tab remains visible while an admin dialog is open.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 12: Full verification

**Files:** none (verification only)

- [ ] **Step 1: Full frontend build + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 2: Full E2E suite**

Run: `cd clients/dashboard && npm run test:e2e`
Expected: PASS — every existing spec (patients, patient-charts, administration, identity, etc.) plus the new/migrated ones from Tasks 6 and 11.

- [ ] **Step 3: Manual smoke (recommended)**

Start the API (`dotnet run --project src/Host/FSH.Starter.Api`) and dashboard (`cd clients/dashboard && npm run dev`). Verify:
- Open a patient's chart, navigate to Overview, come back via the sidebar's "Patient Chart" link — same patient's chart, same active incident.
- Open a report from the chart — it's a dialog, chart visible faintly behind the overlay; close it (X) — chart is back, report still listed as an "open report" pill; click the pill to reopen; use the pill's own × to actually remove it.
- Open a second patient from a different route (command palette / direct URL) — two tabs in the strip; switch between them; close one — falls back to the other.
- Reload the browser entirely — tabs are still there (localStorage rehydration).
- Sidebar "Clinic Setup" shows a single "Administration" entry → hub grid of cards → click "Clinics" → dialog opens over the hub, patient tab strip (if any patient tab is open) still visible at the top.
- Visit `/administration/providers` directly (paste in address bar) — hub renders with the Providers dialog already open.

- [ ] **Step 4: Report status**

Note any flaky selectors found in Task 11's new specs (accessible names are the biggest risk in a plan written without running the app) and fix them in a follow-up commit rather than leaving the suite red.

---

### Task 13: Docs + changelog (golden rule 10)

**Files:**
- Modify: the separate docs repo (`github.com/fullstackhero/docs`) — dashboard navigation/patient-chart/administration pages.
- Create: a changelog entry under `src/content/docs/changelog/` in that docs repo.

- [ ] **Step 1: Update the docs site**

In the docs repo, update any page documenting the dashboard's Patient Chart and Administration navigation to describe: patient tabs persist across navigation (open/close, active incident, open reports survive route changes and page reloads); the report editor opens as a dialog from the chart instead of a separate page; the sidebar's Patient Chart link jumps to the active patient when one is open; Administration is a single hub of dialog-backed sections instead of 17 routed pages.

- [ ] **Step 2: Add a changelog entry**

Add a dated entry under `src/content/docs/changelog/` summarizing: persistent patient workspace tabs, report editor as a dialog, Administration hub.

- [ ] **Step 3: Commit in the docs repo**

```bash
git add src/content/docs
git commit -m "docs: persistent patient tabs, report editor dialog, Administration hub"
```

> If the docs repo is not checked out locally, flag this task to the user as follow-up rather than skipping it — the golden rule requires docs to travel with the change.

---

## Self-Review

**Spec coverage:**
- New `patient-workspace-context.tsx` (types + actions + localStorage) → Task 1. ✓
- Mounted in `AppShell` above `<Outlet/>` → Task 2. ✓
- `PatientTabStrip` under Topbar, only when `openTabs.length > 0` → Task 2 (`if (openTabs.length === 0) return null;`). ✓
- Clicking a tab navigates + `setActivePatient`; `×` calls `closePatient`, falls back to next open tab or `/patient-charts` → Task 2 (`onSelect`/`onClose` in `patient-tab-strip.tsx`). ✓
- Sidebar "Patient Chart" resolves to the active patient's chart when one is open, else the search page → Task 3 (`resolvePatientChartLink`). ✓
- `chart.tsx` stops owning `activeIncidentId` as local state; reads/writes through context keyed by `patientId`; incident/report data fetching untouched (still React Query) → Task 4. ✓
- Report editor → dialog, opened from `chart.tsx` keyed by `activeReportId`, same trigger points converted (`chart.tsx:267,878,885` in the spec's line numbers) → Task 5. ✓
- `/patient-charts/:patientId/reports/:reportId` route dropped; no redirect needed (repo-wide grep found no other deep links) — explicitly verified and documented in Task 5 Step 7, resolving the spec's first open item. ✓
- `openReportIds` + pill switcher; closing the dialog ≠ closing the report (only explicit pill close removes it) → Task 5 Step 4/5, exercised in Task 11 Step 2. ✓
- Administration hub sourced from `nav-data.ts`'s admin items (single source of truth) → Task 7 (`administrationHubItems` extraction) + Task 8 (hub consumes `ADMIN_HUB_SECTIONS`). ✓
- `AdminSectionDialog` lazy-loads the EXISTING page component, no CRUD rewrite → Task 8. ✓
- Existing `/administration/:section` deep links kept, render dialog content over the hub instead of a bare page → Task 9 (`AdministrationHub` backs both `/administration` and `/administration/:section`; the optional param opens the matching dialog on mount). ✓
- Sidebar "Clinic Setup" collapses to a single "Administration" entry; per-section permission checks move to gating hub cards → Task 10. ✓
- Playwright coverage: tab persistence across navigation/reload, explicit close removing a tab, report dialog open/close/switch, Administration hub → dialog → CRUD with a patient tab visible underneath → Task 11 (all three named scenarios present, plus the two-patient/sidebar-resolution case). ✓
- No admin CRUD business-logic change (Non-goal) → Task 8, explicitly a wrapper. ✓
- No nested tabs-within-tabs for reports (Non-goal) → single `activeReportId` + pill row, Task 5. ✓
- No server-persisted state, localStorage only (Non-goal) → Task 1. ✓
- No hard cap on open patient tabs (Non-goal) → not implemented, matches "follow-up if it proves necessary." ✓
- Open item "exact visual design of tab strip/hub cards" → resolved with a concrete, simple design in Tasks 2 and 8 (a quick UI pass, as the spec allowed). ✓
- Open item "staleness handling for openReportIds/tab state" → NOT implemented; this plan does not add any handling for a report/incident deleted elsewhere while its tab is open (the existing per-entity React Query fetches will simply 404/empty on next open, same as today's behavior when navigating to a stale route) — flagging as an explicit follow-up, not silently dropped.
- Docs + changelog → Task 13. ✓

**Placeholder scan:** No TBD/TODO in any code block; every step shows the complete, real code being written (or the complete replaced/replacing snippet for existing files). The one intentional exception is Task 5 Step 2's explicit callout that the *unchanged* middle of `report-editor-dialog.tsx` (vitals/fields/associated-problems/signature/review/addendums/action-bar — several hundred lines) is not reproduced because it is not modified by this task; both edited regions around it are shown in full. ✓

**Type consistency:** `OpenPatientTab` (Task 1) is consumed identically by `patient-tab-strip.tsx` (Task 2), `chart.tsx` (Task 4/5), `report-search-dialog.tsx` (Task 5), and the `SeededPatientTab` test shape (Task 6) — same five fields throughout. `ReportEditorDialog({ patientId, reportId, open, onClose })` defined in Task 5 and consumed with the same shape in `chart.tsx`. `ADMIN_HUB_SECTIONS`/`ADMIN_SECTION_COMPONENTS` (Task 7) keyed by the identical `slug` string in both `hub.tsx` and `admin-section-dialog.tsx` (Task 8) — derived by the same `item.to.replace("/administration/", "")` logic, so a slug produced by one always exists as a key in the other. `administrationHubItems` (Task 7) is the same array referenced by `sections`' "administration" item list until Task 10 replaces that one line — `ALL_ADMINISTRATION_PERMISSIONS` (Task 10) and `ADMIN_HUB_SECTIONS` (Task 7) both derive from it, so adding/removing an admin section only ever requires editing `administrationHubItems` once. ✓

**Known risk flagged honestly (not hidden):** Tasks 6 and 11 were written without running Playwright against real code — accessible-name selectors (`role="tab"` names, the `DialogClose` default "Close" label, pill button names) are my best-effort match against the actual component code in Tasks 2/5/8, but a plan can't substitute for execution. Both tasks' final steps explicitly instruct fixing any selector mismatch against real DOM output rather than treating a first red run as a blocker to work around blindly.
