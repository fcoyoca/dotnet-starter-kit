# Administration Global Dialog + Live Lookup Refresh + Report Panel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refine the persistent-patient-tabs work (Tasks 1-10 of `2026-07-09-persistent-patient-tabs.md`, already on `clinic-app`) in three ways. **Part A:** Administration stops being a routed page entirely — clicking the sidebar's Administration entry opens a single, globally-mounted dialog *without navigating*, so an open patient chart (or any page) stays mounted untouched underneath; `/administration[/:section]` deep links become a thin bridge that opens the dialog and lands on Overview. **Part B:** every Administration entity's mutations invalidate the shared lookup query keys its data is displayed under elsewhere (report editor dropdowns, chart filter combos, diagnostic-codes dialog, SuperBill pickers), so admin edits propagate live without a reload — driven by the concrete per-entity audit table below (10 of the 17 sections need a fix; 7 are already correct). **Part C:** the report editor moves from a modal dialog to a persistent split-pane — the incident list narrows into a left rail, the right panel hosts the open-report tab strip plus the report's fields inline — and unsaved report typing is persisted to a per-report localStorage draft (debounced, cleared on Save/Sign) so it survives tab switches, navigation, and reloads. Matches BackChart's actual "reports are always there" UX; no server autosave.

**Architecture:** **Part A** adds a tiny ephemeral React context (`administration-dialog-context.tsx` — deliberately no localStorage: transient UI state) holding a discriminated view union (`closed | hub | section(slug)`), plus one `AdministrationDialogRoot` mounted in `AppShell` that renders a single Radix `Dialog` whose content switches between the extracted hub grid (`AdministrationHubGrid`, pulled out of `hub.tsx`) and the lazy section component from the existing `section-registry.ts` (with a "← Administration" back affordance). The sidebar's Administration `NavSpec` stays data-only; `sidebar.tsx`'s `NavItemLink` grows a render branch (mirroring the existing `resolvePatientChartLink` special-case precedent) that renders a `<button onClick={openHub}>` for `to === "/administration"`. `routes.tsx` points `/administration` and `/administration/:section` at a null-rendering `AdminDeepLinkOpener` that opens the right dialog view and `navigate("/", { replace: true })`s. `admin-section-dialog.tsx`'s lazy-load-inside-DialogContent logic is absorbed into the root and the file deleted. **Part B** is mechanical per the audit table: where an options key shares the admin list key's array prefix, the existing prefix invalidate already covers it (no change); where the options hook uses a different key (`"administration.xOptions"` dotted-string keys, `["report-types"]`, `["procedure-codes","picker",…]`, etc.), add explicit sibling `invalidateQueries` calls at the same `onSuccess` sites. **Part C** renames `report-editor-dialog.tsx` → `report-editor-panel.tsx`, strips the `Dialog`/`DialogContent` wrapper (data fetching, mutations, and field rendering untouched), and adds draft read/write against a new `report-draft-store.ts` (`fsh.dashboard.reportDrafts.v1`, `Record<reportId, ReportDraft>`, 500 ms debounced writes instrumented once via a snapshot effect, flush-on-unmount, cleared in Save/Sign `onSuccess`). `chart.tsx`'s layout becomes `[400px_minmax(0,1fr)]`: left rail = patient info + incident shortcuts + compact incident list (same filters/actions) + the Patient Reports list; right panel = the open-report pill strip (repositioned, markup unchanged) + `ReportEditorPanel` inline (or an empty state). The workspace context (`patient-workspace-context.tsx`) is untouched — drafts are keyed purely by `reportId` in their own store.

**Tech Stack:** React 19 + Vite 7 + TypeScript, TanStack Query v5, React Router 7, Tailwind v4 + Radix (shadcn-style) `Dialog` primitive, Playwright (route-mocked E2E). No backend changes — dashboard-only, presentation + cache-invalidation layer.

## Global Constraints

- **Frontend only** — every task touches only `clients/dashboard`. No `src/Modules`, no migrations, no endpoint changes.
- **No new test runner** — Playwright route-mocked E2E only (Tasks 5, 8, 12); no vitest/jest exists in this repo.
- **`mutate(arg)` race-safe rule (golden rule 9)** — Part B's added invalidations reuse the values already flowing through each mutation's existing `onSuccess`; no new state captured in closures.
- **No RHF/zod** in the dashboard (`.agents/rules/frontend/dashboard.md`) — the panel keeps its hand-rolled controlled inputs.
- **localStorage keys are `fsh.dashboard.*`-namespaced and versioned** (`fsh.dashboard.reportDrafts.v1`, matching the existing `fsh.dashboard.patientWorkspace.v1`), with `typeof window === "undefined"` guards and `try/catch` around every read/write. The Administration dialog state is deliberately **not** persisted (ephemeral UI state, per the spec).
- **Every route element wrapped in `withSuspense(...)`**; `ProtectedRoute` stays auth-only (no per-route permission guards).
- **React Query invalidation is prefix-match by default** — `invalidateQueries({ queryKey: ["administration", "clinics"] })` refetches every query whose key array *starts with* those elements. A dotted single-string key like `["administration.clinicOptions"]` does **not** share that prefix (one array element vs two) — that mismatch is exactly what Part B fixes with explicit second invalidates rather than renaming keys and breaking unrelated call sites.
- **Docs + changelog travel with the change** (golden rule 10) — Task 13.
- Spec (source of truth): `docs/superpowers/specs/2026-07-10-admin-global-dialog-and-report-panel-design.md`.

## Part B reference — per-entity lookup-invalidation audit

Audited against actual code on `clinic-app` (every file under `src/pages/administration/` + every consumer of `src/api/administration.ts` list/options functions outside it). "Fix" = the exact key(s) to add at that page's existing mutation `onSuccess` sites. Tasks 6-7 implement this table; do not re-derive it.

| # | Section (file) | Mutations invalidate today | Lookup key(s) consumed elsewhere | Consumer file(s) | Fix |
|---|---|---|---|---|---|
| 1 | Allergy Reactions (`allergy-reactions.tsx`) | `["administration","allergy-reactions-page"]` + `["allergy-reactions"]` | `["allergy-reactions","active"]` | `patient-charts/allergy-dialog.tsx` | **None** — `["allergy-reactions"]` prefix already covers it |
| 2 | Clinics (`clinics.tsx`) | `["administration","clinics"]` | `["administration.clinicOptions"]` (useClinicOptions); `["scheduling.clinics"]`; `["administration","clinicOptions","schedule"]` | report editor panel, `patient-charts/list.tsx`, `scheduling/appointments.tsx`, `administration/schedule.tsx` | **Add** `["administration.clinicOptions"]`, `["scheduling.clinics"]`, `["administration","clinicOptions"]` |
| 3 | Code Sources (`code-sources.tsx`) | `["administration","code-sources-page"]` + `["administration.codeSources"]` | `["administration.codeSources"]` (useCodeSourceOptions) | `diagnostics.tsx` / `procedure-codes.tsx` pickers (admin-internal) | **None** — already invalidates the options key |
| 4 | Custom Diagnostics (`custom-diagnostics.tsx`) | `["administration","custom-diagnostics"]` | `["custom-diagnostics","by-ids",…]`; `["dx-search",…]` | `diagnostic-codes-dialog.tsx`, `procedures-performed-dialog.tsx`, `incident-dialog.tsx` | **Add** `["custom-diagnostics"]` + `["dx-search"]` |
| 5 | Departments (`departments.tsx`) | `["administration","departments"]` | `["administration.departmentOptions"]` (useDepartmentOptions) | `chart.tsx`, `incident-dialog.tsx`, `incident-view-dialog.tsx` | **Add** `["administration.departmentOptions"]` |
| 6 | Diagnostic Categories (`diagnostic-categories.tsx`) | `["administration","diagnostic-categories"]` (codes editor also does `["diagnostics","by-category",id]` — already correct) | `["diagnostic-categories","dx-dialog"]` | `diagnostic-codes-dialog.tsx` | **Add** `["diagnostic-categories"]` at the two CRUD sites |
| 7 | Diagnostic Details (`diagnostics.tsx`) | `["administration","diagnostics"]` | `["diagnostics","by-category",…]`, `["diagnostics","search",…]`; `["dx-icd-search",…]` | `diagnostic-codes-dialog.tsx`, `problem-dialog.tsx` | **Add** `["diagnostics"]` + `["dx-icd-search"]` |
| 8 | Drugs (`drugs.tsx`) | `["administration","drugs-page"]` + `["drug-search"]` (CRUD **and** RxNav import) | `["drug-search",…]` | `patient-charts/drug-picker.tsx` | **None** — already correct |
| 9 | Incident Types (`incident-types.tsx`) | `["administration","incident-types"]` | `["administration.incidentTypeOptions"]` (useIncidentTypeOptions) | `chart.tsx`, `incident-dialog.tsx`, `incident-view-dialog.tsx` | **Add** `["administration.incidentTypeOptions"]` |
| 10 | Insurance Companies (`insurance-companies.tsx`) | `["administration","insurance-companies"]` | *(none — no consumer outside the admin page; patient insurance is free-text in `api/patients.ts`)* | — | **None** — nothing to refresh |
| 11 | Insurance Types (`insurance-types.tsx`) | `["administration","insurance-types"]` + `["administration.insuranceTypeOptions"]` + `["administration","insurance-type-procedures"]` | `["administration.insuranceTypeOptions"]` ✓; `["insurance-type-procedures", id]` (SuperBill price lookup — different prefix!) | `procedures-performed-dialog.tsx` | **Add** `["insurance-type-procedures"]` |
| 12 | Macros (`macros.tsx`, three CRUD groups) | macros: `["administration","macros"]`; report types: `["administration","report-types"]`; report fields: `["administration","report-fields"]` | `["administration.macros",…]` (macro-insert); `["report-types"]` (chart Add Report menu, export/search dialogs); `["report-fields", typeId]` (report editor panel) | `components/ui/macro-insert.tsx`, `chart.tsx`, `export-reports-dialog.tsx`, `report-search-dialog.tsx`, report editor panel | **Add** `["administration.macros"]` / `["report-types"]` / `["report-fields"]` respectively |
| 13 | Medication Dose Units (`medication-dose-units.tsx`) | `["administration","medication-dose-units-page"]` + `["medication-dose-units"]` | `["medication-dose-units","active"]` | `patient-charts/medication-dialog.tsx` | **None** — prefix covers it |
| 14 | Patient Document Types (`patient-document-types.tsx`) | `["administration","patient-document-types"]` | `["administration","patient-document-types","options"]` | `patient-charts/documents-list-dialog.tsx` | **None** — same array prefix, already covered |
| 15 | Procedure Categories (`procedure-categories.tsx`) | `["administration","procedure-categories"]` + `["administration.procedureCategoryOptions"]` | `["administration.procedureCategoryOptions"]` | `procedures-performed-dialog.tsx` | **None** — already invalidates the options key |
| 16 | Procedure Codes (`procedure-codes.tsx`) | `["administration","procedure-codes"]` | `["procedure-codes","picker",…]` (SuperBill); admin insurance-types picker `["administration","procedure-codes","picker",…]` is prefix-covered | `procedures-performed-dialog.tsx` | **Add** `["procedure-codes"]` |
| 17 | Providers (`providers.tsx`) | `["administration","providers"]` | `["administration.providerOptions"]` (useProviderOptions); `["scheduling.providers",…]` | report editor panel, `select-appointment-dialog.tsx`, `patient-charts/list.tsx`, `scheduling/appointments.tsx` | **Add** `["administration.providerOptions"]` + `["scheduling.providers"]` |

**Net: 10 pages get edits** (clinics, custom-diagnostics, departments, diagnostic-categories, diagnostics, incident-types, insurance-types, macros, procedure-codes, providers); **7 need nothing** (allergy-reactions, code-sources, drugs, insurance-companies, medication-dose-units, patient-document-types, procedure-categories). Non-hub pages (`schedule.tsx`, `email-settings.tsx`) were checked too: their data has no cross-page consumer beyond what `["scheduling.*"]` invalidation already handles — out of scope.

---

### Task 1: `administration-dialog-context.tsx` — ephemeral dialog-state module

**Files:**
- Create: `clients/dashboard/src/state/administration-dialog-context.tsx`

**Interfaces:**
- Produces: `AdminDialogView`, `AdministrationDialogProvider`, `useAdministrationDialog()` (actions `openHub()`, `openSection(slug)`, `backToHub()`, `close()`).

- [ ] **Step 1: Create the context module**

Create `clients/dashboard/src/state/administration-dialog-context.tsx`:

```tsx
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from "react";

/**
 * Which view the single global Administration dialog is showing.
 * Deliberately NOT persisted to localStorage — this is ephemeral UI state
 * (unlike patient-workspace-context.tsx's tabs, which model durable work).
 */
export type AdminDialogView =
  | { kind: "closed" }
  | { kind: "hub" }
  | { kind: "section"; slug: string };

type AdministrationDialogContextValue = {
  view: AdminDialogView;
  /** Open the dialog on the 17-card hub grid. */
  openHub: () => void;
  /** Open the dialog directly on one section (also used by hub cards + deep links). */
  openSection: (slug: string) => void;
  /** From a section, return to the hub grid without closing the dialog. */
  backToHub: () => void;
  close: () => void;
};

const AdministrationDialogContext = createContext<AdministrationDialogContextValue | null>(null);

/** Mount once in AppShell (see app-shell.tsx) wrapping the Sidebar, the
 *  mobile drawer, and the routed <Outlet/> — every trigger (sidebar button,
 *  mobile drawer button, AdminDeepLinkOpener route) is a descendant. */
export function AdministrationDialogProvider({ children }: { children: ReactNode }) {
  const [view, setView] = useState<AdminDialogView>({ kind: "closed" });

  const openHub = useCallback(() => setView({ kind: "hub" }), []);
  const openSection = useCallback((slug: string) => setView({ kind: "section", slug }), []);
  const backToHub = useCallback(() => setView({ kind: "hub" }), []);
  const close = useCallback(() => setView({ kind: "closed" }), []);

  const value = useMemo<AdministrationDialogContextValue>(
    () => ({ view, openHub, openSection, backToHub, close }),
    [view, openHub, openSection, backToHub, close],
  );

  return (
    <AdministrationDialogContext.Provider value={value}>
      {children}
    </AdministrationDialogContext.Provider>
  );
}

export function useAdministrationDialog(): AdministrationDialogContextValue {
  const ctx = useContext(AdministrationDialogContext);
  if (!ctx)
    throw new Error("useAdministrationDialog must be used within AdministrationDialogProvider");
  return ctx;
}
```

- [ ] **Step 2: Typecheck**

Run: `cd clients/dashboard && npm run build`
Expected: clean — nothing imports this module yet.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/state/administration-dialog-context.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): add AdministrationDialogProvider (global admin dialog state)

Ephemeral view union (closed | hub | section) + open/back/close actions.
Deliberately not persisted (transient UI state, unlike the patient
workspace tabs). Not yet mounted -- wiring lands in the following tasks.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Extract `AdministrationHubGrid` + create `AdministrationDialogRoot`

**Files:**
- Modify: `clients/dashboard/src/pages/administration/hub.tsx`
- Create: `clients/dashboard/src/pages/administration/administration-dialog-root.tsx`

**Interfaces:**
- Consumes: `useAdministrationDialog()` (Task 1), `ADMIN_HUB_SECTIONS` / `ADMIN_SECTION_COMPONENTS` (existing `section-registry.ts`).
- Produces: `AdministrationHubGrid({ onSelectSection })` (pure content, no routing hooks), `AdministrationDialogRoot()`.

- [ ] **Step 1: Extract the pure grid from `hub.tsx`**

Replace the entire contents of `clients/dashboard/src/pages/administration/hub.tsx` with:

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
 * Pure content component: the Administration header + permission-gated
 * 17-card grid. No routing hooks — where a card click "goes" is the
 * caller's decision (the global AdministrationDialogRoot switches its
 * view; the legacy routed page below navigates). Extracted so the grid
 * renders identically inside the global dialog (Part A).
 */
export function AdministrationHubGrid({
  onSelectSection,
}: {
  onSelectSection: (slug: string) => void;
}) {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  const visibleCards = ADMIN_HUB_SECTIONS.filter((s) => !s.perm || perms.includes(s.perm));

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
              onClick={() => onSelectSection(s.slug)}
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
    </div>
  );
}

/** Routed page — RETIRED in Task 4 (replaced by AdminDeepLinkOpener).
 *  Kept compiling through Tasks 2-3 so each task lands green independently;
 *  behavior is unchanged from before this task. */
export function AdministrationHub() {
  const { section } = useParams<{ section?: string }>();
  const navigate = useNavigate();

  const [openSlug, setOpenSlug] = useState<string | null>(section ?? null);

  // Keep the open dialog in sync with the :section route param — covers
  // direct deep links (/administration/clinics) and browser back/forward.
  useEffect(() => {
    setOpenSlug(section ?? null);
  }, [section]);

  const openSection = (slug: string) => {
    setOpenSlug(slug);
    navigate(`/administration/${slug}`, { replace: true });
  };

  const closeSection = () => {
    setOpenSlug(null);
    navigate("/administration", { replace: true });
  };

  return (
    <>
      <AdministrationHubGrid onSelectSection={openSection} />
      <AdminSectionDialog slug={openSlug} onClose={closeSection} />
    </>
  );
}
```

> Behavior-neutral: the routed page renders the same header/grid/dialog as before (permission filtering moved into the grid component with it). `useAuth` moved into the grid, so the page function no longer needs it.

- [ ] **Step 2: Create the dialog root**

Create `clients/dashboard/src/pages/administration/administration-dialog-root.tsx`:

```tsx
import { Suspense } from "react";
import { ArrowLeft } from "lucide-react";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdministrationDialog } from "@/state/administration-dialog-context";
import {
  ADMIN_HUB_SECTIONS,
  ADMIN_SECTION_COMPONENTS,
} from "@/pages/administration/section-registry";
import { AdministrationHubGrid } from "@/pages/administration/hub";

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
 * THE Administration dialog — mounted once in AppShell (Task 3), layered
 * over whatever page is behind it, which stays mounted untouched. One
 * Radix Dialog whose content switches between the hub grid and a lazy
 * section page (the same components section-registry.ts always backed),
 * so hub → section → back never re-opens the overlay. Absorbs the old
 * AdminSectionDialog's lazy-load-inside-DialogContent logic (that file is
 * deleted in Task 4 once nothing routes to the hub page).
 */
export function AdministrationDialogRoot() {
  const { view, openSection, backToHub, close } = useAdministrationDialog();

  const section =
    view.kind === "section" ? ADMIN_HUB_SECTIONS.find((s) => s.slug === view.slug) : undefined;
  const Section = view.kind === "section" ? (ADMIN_SECTION_COMPONENTS[view.slug] ?? null) : null;

  return (
    <Dialog open={view.kind !== "closed"} onOpenChange={(o) => (!o ? close() : undefined)}>
      <DialogContent className="!max-w-4xl overflow-hidden p-0">
        <DialogTitle className="sr-only">{section?.label ?? "Administration"}</DialogTitle>
        <div className="max-h-[85vh] overflow-y-auto p-6 pt-10">
          {view.kind === "hub" && <AdministrationHubGrid onSelectSection={openSection} />}
          {view.kind === "section" && (
            <div className="space-y-4">
              <button
                type="button"
                onClick={backToHub}
                className="inline-flex cursor-pointer items-center gap-1.5 text-[13px] text-[var(--color-muted-foreground)] transition-colors hover:text-[var(--color-foreground)]"
              >
                <ArrowLeft className="size-4" />
                Administration
              </button>
              {Section ? (
                <Suspense fallback={<SectionFallback />}>
                  <Section />
                </Suspense>
              ) : (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  Unknown administration section.
                </p>
              )}
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean — the root isn't mounted yet (Task 3); the routed hub page still works exactly as before.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/pages/administration/hub.tsx clients/dashboard/src/pages/administration/administration-dialog-root.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): extract AdministrationHubGrid + add AdministrationDialogRoot

Pure grid component (no routing hooks) reused by both the legacy routed
hub page (unchanged behavior, retired in a later task) and the new single
global Administration dialog, which switches between hub and lazy section
views with a back-to-hub affordance. Not yet mounted.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Mount the provider + root in `AppShell`; sidebar Administration entry becomes a button

**Files:**
- Modify: `clients/dashboard/src/components/layout/app-shell.tsx`
- Modify: `clients/dashboard/src/components/layout/sidebar.tsx`

**Interfaces:**
- Consumes: `AdministrationDialogProvider`, `useAdministrationDialog()` (Task 1), `AdministrationDialogRoot` (Task 2).

- [ ] **Step 1: Mount provider + root in `app-shell.tsx`**

Add two imports after the existing `PatientWorkspaceProvider` import:

```tsx
import { PatientWorkspaceProvider } from "@/state/patient-workspace-context";
import { AdministrationDialogProvider } from "@/state/administration-dialog-context";
import { AdministrationDialogRoot } from "@/pages/administration/administration-dialog-root";
```

Wrap the shell. Replace:

```tsx
      <PatientWorkspaceProvider>
      <MobileNavProvider>
```

with:

```tsx
      <PatientWorkspaceProvider>
      <AdministrationDialogProvider>
      <MobileNavProvider>
```

and replace:

```tsx
        <MobileNavRoot />
      </MobileNavProvider>
      </PatientWorkspaceProvider>
```

with:

```tsx
        <MobileNavRoot />

        {/* THE single global Administration dialog — layered over whatever
            page is behind it (Part A). Inside the provider so the sidebar
            button, the mobile drawer, and the AdminDeepLinkOpener route
            (all descendants of the Outlet subtree) can drive it. */}
        <AdministrationDialogRoot />
      </MobileNavProvider>
      </AdministrationDialogProvider>
      </PatientWorkspaceProvider>
```

> `AdministrationDialogProvider` sits inside `PatientWorkspaceProvider` but the two are independent — nesting order doesn't matter (per the spec). Everything that needs `useAdministrationDialog()` (Sidebar, mobile drawer via `MobileNavRoot`, and the routed `AdminDeepLinkOpener` under `<Outlet/>`) is a descendant.

- [ ] **Step 2: `sidebar.tsx` — render the Administration entry as a button**

Add the import after the existing workspace-context import:

```tsx
import { usePatientWorkspace } from "@/state/patient-workspace-context";
import { useAdministrationDialog } from "@/state/administration-dialog-context";
```

In `NavItemLink`, insert a render branch. Replace:

```tsx
  const Icon = item.icon;
  return (
    <NavLink
```

with:

```tsx
  const Icon = item.icon;
  const adminDialog = useAdministrationDialog();

  // Part A: the Administration entry opens the global dialog instead of
  // navigating — whatever page is behind stays mounted, untouched. Mirrors
  // resolvePatientChartLink's "special-case one nav entry" precedent, but
  // as a render branch since the affordance is a button, not a Link. One
  // branch covers all three renders of this component: the expanded
  // accordion, the collapsed icon rail, and the mobile drawer.
  if (item.to === "/administration") {
    const isDialogOpen = adminDialog.view.kind !== "closed";
    return (
      <button
        type="button"
        title={collapsed ? item.label : undefined}
        aria-label={collapsed ? item.label : undefined}
        aria-haspopup="dialog"
        onClick={() => {
          adminDialog.openHub();
          onNavigate?.();
        }}
        className={cn(
          "group/nav relative flex h-9 w-full cursor-pointer items-center gap-3 rounded-md text-left text-sm font-medium",
          "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          isDialogOpen
            ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            : "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
          collapsed ? "justify-center px-0" : "px-3",
        )}
      >
        {/* 2px brand bar while the dialog is open — same active affordance
            the NavLink branch gets from the router. */}
        <span
          aria-hidden
          className={cn(
            "absolute left-0 top-1/2 h-4 w-0.5 -translate-y-1/2 rounded-r-full bg-[var(--color-primary)]",
            "transition-opacity duration-[var(--duration-default)]",
            isDialogOpen ? "opacity-100" : "opacity-0",
          )}
        />

        <Icon className="h-4 w-4 shrink-0" />

        {!collapsed && <span className="whitespace-nowrap">{item.label}</span>}

        {collapsed && (
          <span
            role="tooltip"
            className={cn(
              "pointer-events-none absolute left-full top-1/2 z-50 ml-3 -translate-y-1/2 whitespace-nowrap",
              "rounded-md border border-[var(--color-border)] bg-[var(--color-popover)] px-2 py-1",
              "text-xs text-[var(--color-popover-foreground)] shadow-[var(--shadow-md)]",
              "opacity-0 transition-opacity duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "group-hover/nav:opacity-100 group-focus-visible/nav:opacity-100",
            )}
          >
            {item.label}
          </span>
        )}
      </button>
    );
  }

  return (
    <NavLink
```

> `nav-data.ts` is untouched: the entry keeps `to: "/administration"` as its identity/key (and as the deep-link the mobile drawer would have navigated to), `NavItemLink` just intercepts it. The `indent` prop is unused by the button branch (both branches render `px-3` when expanded).

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean. Manual sanity (optional): `npm run dev` → clicking sidebar "Administration" from any page opens the hub dialog without changing the URL; the old `/administration` routed page ALSO still works (retired next task).

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/components/layout/app-shell.tsx clients/dashboard/src/components/layout/sidebar.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): sidebar Administration opens the global dialog, not a route

Mounts AdministrationDialogProvider + AdministrationDialogRoot in AppShell;
NavItemLink renders the /administration entry as a button (openHub) across
the expanded accordion, collapsed rail, and mobile drawer. Whatever page is
behind stays mounted -- an open patient chart is untouched.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: `/administration[/:section]` → `AdminDeepLinkOpener` bridge; retire the hub page + `AdminSectionDialog`

**Files:**
- Modify: `clients/dashboard/src/pages/administration/hub.tsx` (page → grid + bridge)
- Modify: `clients/dashboard/src/routes.tsx`
- Delete: `clients/dashboard/src/pages/administration/admin-section-dialog.tsx`
- Modify: `clients/dashboard/tests/administration/diagnostics.spec.ts`

**Interfaces:**
- Produces: `AdminDeepLinkOpener()` (route element, renders `null`).
- Consumes: `useAdministrationDialog()` (Task 1), `ADMIN_SECTION_COMPONENTS` (registry).

- [ ] **Step 1: Rewrite `hub.tsx` — drop the routed page, add the bridge**

Replace the entire contents of `clients/dashboard/src/pages/administration/hub.tsx` with:

```tsx
import { useEffect } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { Building2 } from "lucide-react";
import { useAuth } from "@/auth/use-auth";
import { EntityPageHeader } from "@/components/list";
import { useAdministrationDialog } from "@/state/administration-dialog-context";
import {
  ADMIN_HUB_SECTIONS,
  ADMIN_SECTION_COMPONENTS,
} from "@/pages/administration/section-registry";
import { cn } from "@/lib/cn";

/**
 * Pure content component: the Administration header + permission-gated
 * 17-card grid, rendered inside the global AdministrationDialogRoot.
 * No routing hooks — a card click switches the dialog's view.
 */
export function AdministrationHubGrid({
  onSelectSection,
}: {
  onSelectSection: (slug: string) => void;
}) {
  const { user } = useAuth();
  const perms = user?.permissions ?? [];
  const visibleCards = ADMIN_HUB_SECTIONS.filter((s) => !s.perm || perms.includes(s.perm));

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
              onClick={() => onSelectSection(s.slug)}
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
    </div>
  );
}

/**
 * Route bridge for legacy /administration[/:section] deep links: opens the
 * matching view of the global Administration dialog, then replaces the URL
 * with Overview ("/") — a cold deep link has no "previous page" to
 * preserve, so Overview is the surface the dialog layers over. An unknown
 * slug falls back to the hub. Renders nothing.
 */
export function AdminDeepLinkOpener() {
  const { section } = useParams<{ section?: string }>();
  const navigate = useNavigate();
  const { openHub, openSection } = useAdministrationDialog();

  useEffect(() => {
    if (section && ADMIN_SECTION_COMPONENTS[section]) openSection(section);
    else openHub();
    navigate("/", { replace: true });
  }, [section, openHub, openSection, navigate]);

  return null;
}
```

- [ ] **Step 2: Delete the old section dialog wrapper**

```bash
git rm clients/dashboard/src/pages/administration/admin-section-dialog.tsx
```

Its lazy-load-inside-`DialogContent` logic lives on in `AdministrationDialogRoot` (Task 2); nothing imports it after Step 1.

- [ ] **Step 3: Point the routes at the bridge**

In `clients/dashboard/src/routes.tsx`, replace:

```tsx
const AdministrationHub = lazyNamed(() => import("@/pages/administration/hub"), "AdministrationHub");
```

with:

```tsx
const AdminDeepLinkOpener = lazyNamed(
  () => import("@/pages/administration/hub"),
  "AdminDeepLinkOpener",
);
```

and replace:

```tsx
          { path: "administration", element: withSuspense(<AdministrationHub />) },
          { path: "administration/schedule", element: withSuspense(<SchedulePage />) },
          { path: "administration/email-settings", element: withSuspense(<EmailSettingsPage />) },
          { path: "administration/:section", element: withSuspense(<AdministrationHub />) },
```

with:

```tsx
          { path: "administration", element: withSuspense(<AdminDeepLinkOpener />) },
          { path: "administration/schedule", element: withSuspense(<SchedulePage />) },
          { path: "administration/email-settings", element: withSuspense(<EmailSettingsPage />) },
          { path: "administration/:section", element: withSuspense(<AdminDeepLinkOpener />) },
```

> `administration/schedule` and `administration/email-settings` stay literal routed pages (Settings-section, not part of the hub) — React Router matches static segments before the `:section` dynamic one regardless of order, but keeping them textually between the two bridge routes documents the precedence.

- [ ] **Step 4: Update `tests/administration/diagnostics.spec.ts` for the bridge**

The deep link now lands on Overview (`/`) with the dialog on top, so Overview's own queries need mocks and assertions must scope to the dialog. In the `beforeEach`, replace:

```ts
    await mockJsonResponse(page, "**/api/v1/identity/permissions", DIAG_PERMS);
    await mockLookups(page);
```

with:

```ts
    await mockJsonResponse(page, "**/api/v1/identity/permissions", DIAG_PERMS);
    await mockLookups(page);
    // The deep-link bridge redirects to Overview ("/") and opens the
    // Administration dialog on top — mock Overview's queries so the
    // background page settles deterministically.
    await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
    await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", { plan: "Scale", status: "Active" });
    await mockJsonResponse(page, "**/api/v1/audits**", paged([]));
```

In the first test, replace:

```ts
    await page.goto("/administration/diagnostics");

    await expect(page.getByRole("heading", { name: "Diagnostic Details", level: 1 })).toBeVisible();
    await expect(page.getByText("A00").last()).toBeVisible();
    await expect(page.getByText("Cholera").last()).toBeVisible();
    await expect(page.getByText("ICD-10-CM").last()).toBeVisible();
```

with:

```ts
    await page.goto("/administration/diagnostics");

    // The bridge opens the section dialog and replaces the URL with "/".
    await expect(page).toHaveURL("/");
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Diagnostic Details", level: 1 })).toBeVisible();
    await expect(dialog.getByText("A00").last()).toBeVisible();
    await expect(dialog.getByText("Cholera").last()).toBeVisible();
    await expect(dialog.getByText("ICD-10-CM").last()).toBeVisible();
```

In the second test, replace:

```ts
    await page.goto("/administration/diagnostics");
    await page.getByRole("button", { name: /new diagnostic/i }).first().click();
```

with:

```ts
    await page.goto("/administration/diagnostics");
    await expect(page).toHaveURL("/");
    await page.getByRole("button", { name: /new diagnostic/i }).first().click();
```

> The rest of the second test is unchanged: once the create dialog opens as the top-most modal, Radix marks the Administration dialog `aria-hidden`, so `page.getByRole("dialog")` resolves to the create dialog alone — same as it did when the admin page was routed.

- [ ] **Step 5: Run the spec, typecheck + lint**

Run: `cd clients/dashboard && npx playwright test tests/administration/diagnostics.spec.ts && npm run build && npm run lint`
Expected: PASS / clean — no dangling references to `AdministrationHub` or `AdminSectionDialog`.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/pages/administration/hub.tsx clients/dashboard/src/routes.tsx clients/dashboard/tests/administration/diagnostics.spec.ts
git rm --cached clients/dashboard/src/pages/administration/admin-section-dialog.tsx 2>/dev/null || true
git commit -m "$(cat <<'EOF'
feat(dashboard): /administration deep links become a bridge into the global dialog

AdminDeepLinkOpener opens the matching dialog view (hub or section) and
replaces the URL with Overview -- Administration no longer owns page-level
real estate. Retires the routed hub page and the standalone
AdminSectionDialog (its lazy-load logic lives in AdministrationDialogRoot).
diagnostics.spec.ts updated for the redirect + dialog-scoped assertions.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 5: New Playwright coverage — admin dialog over an open chart + deep links

**Files:**
- Create: `clients/dashboard/tests/administration/admin-dialog.spec.ts`

**Interfaces:**
- Consumes: `seedAuthedSession`, `installShellMocks`, `mockJsonResponse`, `paged`, `seedPatientWorkspace` (all existing helpers).

- [ ] **Step 1: Create the spec**

Create `clients/dashboard/tests/administration/admin-dialog.spec.ts`:

```ts
// E2E coverage for Part A: Administration as a globally-triggered dialog.
// The key interaction: opening Administration from the sidebar while a
// patient chart is open does NOT navigate — the chart stays mounted
// underneath, untouched. Deep links open the dialog over Overview.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.Incidents.View",
  "Permissions.Administration.Clinics.View",
  "Permissions.Administration.Providers.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";

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

async function mockChartLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

async function mockOverviewLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
  await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", { plan: "Scale", status: "Active" });
  await mockJsonResponse(page, "**/api/v1/audits**", paged([]));
}

test.describe("administration global dialog", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([CLINIC]));
  });

  test("sidebar button opens the dialog over an open chart without navigating", async ({ page }) => {
    await seedPatientWorkspace(page, [{ patientId: PATIENT_ID, patientLabel: "Alice Q Vance" }]);
    await mockChartLookups(page);

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await expect(page.getByText("Alice Q Vance").first()).toBeVisible();

    // The Administration entry lives inside the Clinic Setup accordion —
    // expand it first (closed accordion panels are aria-hidden).
    await page.getByRole("button", { name: "Clinic Setup" }).click();
    await page.getByRole("button", { name: "Administration", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Administration", level: 1 })).toBeVisible();
    // No navigation happened — the chart URL is untouched.
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${PATIENT_ID}$`));

    // Hub → section → back-to-hub, all inside the ONE dialog.
    await dialog.getByRole("button", { name: "Clinics" }).click();
    await expect(dialog.getByRole("heading", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByText("Main Clinic")).toBeVisible();
    await dialog.getByRole("button", { name: "Administration", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Clinics" })).toBeVisible();

    // Close — the chart is exactly where it was.
    await dialog.getByRole("button", { name: "Close" }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.getByText("Alice Q Vance").first()).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${PATIENT_ID}$`));
  });

  test("a deep link opens the section dialog and lands on Overview", async ({ page }) => {
    await mockOverviewLookups(page);

    await page.goto("/administration/clinics");

    await expect(page).toHaveURL("/");
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByText("Main Clinic")).toBeVisible();
    // The back-to-hub affordance is present in section view.
    await expect(dialog.getByRole("button", { name: "Administration", exact: true })).toBeVisible();
  });

  test("hub cards are permission-gated", async ({ page }) => {
    await mockOverviewLookups(page);

    await page.goto("/administration");

    await expect(page).toHaveURL("/");
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("button", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Providers" })).toBeVisible();
    // No Departments.View grant — its card must not render.
    await expect(dialog.getByRole("button", { name: "Departments" })).toHaveCount(0);
  });
});
```

- [ ] **Step 2: Run the spec**

Run: `cd clients/dashboard && npx playwright test tests/administration/admin-dialog.spec.ts`
Expected: PASS. If an accessible name doesn't match the rendered DOM (e.g. the back affordance's name composition), fix the selector against the real DOM — this spec is new, there is no prior baseline to preserve.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/tests/administration/admin-dialog.spec.ts
git commit -m "$(cat <<'EOF'
test(dashboard): E2E coverage for the global Administration dialog

Sidebar button opens the dialog over an open patient chart without
navigating (URL + chart untouched, hub -> section -> back -> close);
deep links land on Overview with the right dialog view open; hub cards
are permission-gated.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 6: Part B fixes, cluster 1 — chart/scheduling lookups (clinics, departments, incident-types, providers)

Implements rows 2, 5, 9, 17 of the audit table. Each page has exactly two invalidation sites (an `invalidate` helper in the editor dialog + a plain call in the delete dialog); both get the added keys.

**Files:**
- Modify: `clients/dashboard/src/pages/administration/clinics.tsx`
- Modify: `clients/dashboard/src/pages/administration/departments.tsx`
- Modify: `clients/dashboard/src/pages/administration/incident-types.tsx`
- Modify: `clients/dashboard/src/pages/administration/providers.tsx`

**Interfaces:**
- No new exports — only added `queryClient.invalidateQueries(...)` calls inside existing mutation `onSuccess` paths.

- [ ] **Step 1: `clinics.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["administration", "clinics"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "clinics"] });
    // Live lookup refresh (Part B): clinic names feed the report editor /
    // patient-chart-search pickers (useClinicOptions), the scheduling
    // clinic filter, and the Settings > Schedule clinic picker — none of
    // which share the ["administration","clinics"] array prefix.
    void queryClient.invalidateQueries({ queryKey: ["administration.clinicOptions"] });
    void queryClient.invalidateQueries({ queryKey: ["scheduling.clinics"] });
    void queryClient.invalidateQueries({ queryKey: ["administration", "clinicOptions"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "clinics"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "clinics"] });
      queryClient.invalidateQueries({ queryKey: ["administration.clinicOptions"] });
      queryClient.invalidateQueries({ queryKey: ["scheduling.clinics"] });
      queryClient.invalidateQueries({ queryKey: ["administration", "clinicOptions"] });
```

- [ ] **Step 2: `departments.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "departments"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "departments"] });
    // Live lookup refresh (Part B): department names feed the chart's
    // incident filter + incident dialogs (useDepartmentOptions).
    void queryClient.invalidateQueries({ queryKey: ["administration.departmentOptions"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "departments"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "departments"] });
      queryClient.invalidateQueries({ queryKey: ["administration.departmentOptions"] });
```

- [ ] **Step 3: `incident-types.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["administration", "incident-types"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "incident-types"] });
    // Live lookup refresh (Part B): incident type names feed the chart's
    // incident filter + incident dialogs (useIncidentTypeOptions).
    void queryClient.invalidateQueries({ queryKey: ["administration.incidentTypeOptions"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "incident-types"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "incident-types"] });
      queryClient.invalidateQueries({ queryKey: ["administration.incidentTypeOptions"] });
```

- [ ] **Step 4: `providers.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "providers"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "providers"] });
    // Live lookup refresh (Part B): provider names feed the report editor's
    // Provider/Reviewer pickers, Select Appointment, patient chart search
    // (useProviderOptions), and the scheduling provider filter.
    void queryClient.invalidateQueries({ queryKey: ["administration.providerOptions"] });
    void queryClient.invalidateQueries({ queryKey: ["scheduling.providers"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "providers"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "providers"] });
      queryClient.invalidateQueries({ queryKey: ["administration.providerOptions"] });
      queryClient.invalidateQueries({ queryKey: ["scheduling.providers"] });
```

- [ ] **Step 5: Typecheck + lint + affected specs**

Run: `cd clients/dashboard && npm run build && npm run lint && npx playwright test tests/scheduling/appointments.spec.ts tests/patient-charts/search.spec.ts`
Expected: clean / PASS — added invalidations are behavior-additive (extra refetches only when those queries exist).

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/pages/administration/clinics.tsx clients/dashboard/src/pages/administration/departments.tsx clients/dashboard/src/pages/administration/incident-types.tsx clients/dashboard/src/pages/administration/providers.tsx
git commit -m "$(cat <<'EOF'
fix(dashboard): clinic/department/incident-type/provider edits refresh shared lookups

Admin mutations now also invalidate the options-hook keys consumed outside
Administration (useClinicOptions, useDepartmentOptions,
useIncidentTypeOptions, useProviderOptions, scheduling.* filters), so an
open report's dropdowns and the chart's filter combos reflect admin edits
live instead of after a reload. Part B audit rows 2, 5, 9, 17.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 7: Part B fixes, cluster 2 — diagnostics/procedures/report-template lookups

Implements rows 4, 6, 7, 11, 12, 16 of the audit table.

**Files:**
- Modify: `clients/dashboard/src/pages/administration/custom-diagnostics.tsx`
- Modify: `clients/dashboard/src/pages/administration/diagnostics.tsx`
- Modify: `clients/dashboard/src/pages/administration/diagnostic-categories.tsx`
- Modify: `clients/dashboard/src/pages/administration/procedure-codes.tsx`
- Modify: `clients/dashboard/src/pages/administration/insurance-types.tsx`
- Modify: `clients/dashboard/src/pages/administration/macros.tsx`

**Interfaces:**
- No new exports — only added `queryClient.invalidateQueries(...)` calls inside existing mutation `onSuccess` paths.

- [ ] **Step 1: `custom-diagnostics.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "custom-diagnostics"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "custom-diagnostics"] });
    // Live lookup refresh (Part B): custom diagnostics feed the diagnostic
    // codes dialog + SuperBill (["custom-diagnostics","by-ids",…]) and the
    // incident dialog's dx search (["dx-search",…]).
    void queryClient.invalidateQueries({ queryKey: ["custom-diagnostics"] });
    void queryClient.invalidateQueries({ queryKey: ["dx-search"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "custom-diagnostics"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "custom-diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["custom-diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["dx-search"] });
```

- [ ] **Step 2: `diagnostics.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "diagnostics"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "diagnostics"] });
    // Live lookup refresh (Part B): the ICD catalog feeds the diagnostic
    // codes dialog (["diagnostics","by-category"/"search",…]) and the
    // problem dialog's dx search (["dx-icd-search",…]).
    void queryClient.invalidateQueries({ queryKey: ["diagnostics"] });
    void queryClient.invalidateQueries({ queryKey: ["dx-icd-search"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "diagnostics"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["diagnostics"] });
      queryClient.invalidateQueries({ queryKey: ["dx-icd-search"] });
```

- [ ] **Step 3: `diagnostic-categories.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () =>
    queryClient.invalidateQueries({ queryKey: ["administration", "diagnostic-categories"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "diagnostic-categories"] });
    // Live lookup refresh (Part B): category names feed the diagnostic
    // codes dialog's category dropdown (["diagnostic-categories","dx-dialog"]).
    void queryClient.invalidateQueries({ queryKey: ["diagnostic-categories"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "diagnostic-categories"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "diagnostic-categories"] });
      queryClient.invalidateQueries({ queryKey: ["diagnostic-categories"] });
```

> The category-codes editor in this file already invalidates `["diagnostics","by-category", id]` on save — no change there.

- [ ] **Step 4: `procedure-codes.tsx`**

Replace (editor dialog helper):

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "procedure-codes"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "procedure-codes"] });
    // Live lookup refresh (Part B): procedure codes feed the SuperBill's
    // code picker (["procedure-codes","picker",…]).
    void queryClient.invalidateQueries({ queryKey: ["procedure-codes"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "procedure-codes"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "procedure-codes"] });
      queryClient.invalidateQueries({ queryKey: ["procedure-codes"] });
```

- [ ] **Step 5: `insurance-types.tsx`**

This page already invalidates its options key — only the SuperBill's price-association key is missing. Replace (editor dialog helper — already a block body):

```tsx
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "insurance-types"] });
    queryClient.invalidateQueries({ queryKey: ["administration.insuranceTypeOptions"] });
    queryClient.invalidateQueries({ queryKey: ["administration", "insurance-type-procedures"] });
  };
```

with:

```tsx
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["administration", "insurance-types"] });
    queryClient.invalidateQueries({ queryKey: ["administration.insuranceTypeOptions"] });
    queryClient.invalidateQueries({ queryKey: ["administration", "insurance-type-procedures"] });
    // Live lookup refresh (Part B): the SuperBill reads prices under
    // ["insurance-type-procedures", id] — a different prefix.
    queryClient.invalidateQueries({ queryKey: ["insurance-type-procedures"] });
  };
```

Replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "insurance-types"] });
      queryClient.invalidateQueries({ queryKey: ["administration.insuranceTypeOptions"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "insurance-types"] });
      queryClient.invalidateQueries({ queryKey: ["administration.insuranceTypeOptions"] });
      queryClient.invalidateQueries({ queryKey: ["insurance-type-procedures"] });
```

- [ ] **Step 6: `macros.tsx` (three CRUD groups in one file)**

Macros group — replace (editor dialog helper):

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });
    // Live lookup refresh (Part B): the report editor's macro popover reads
    // ["administration.macros", …] (dotted key — different prefix).
    void queryClient.invalidateQueries({ queryKey: ["administration.macros"] });
  };
```

Macros group — replace (delete dialog):

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });
```

with:

```tsx
      queryClient.invalidateQueries({ queryKey: ["administration", "macros"] });
      queryClient.invalidateQueries({ queryKey: ["administration.macros"] });
```

Report-types manage group — replace:

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "report-types"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "report-types"] });
    // Live lookup refresh (Part B): the chart's Add Report menu, report
    // search, and export dialogs read ["report-types"].
    void queryClient.invalidateQueries({ queryKey: ["report-types"] });
  };
```

Report-fields manage group — replace:

```tsx
  const invalidate = () => queryClient.invalidateQueries({ queryKey: ["administration", "report-fields"] });
```

with:

```tsx
  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["administration", "report-fields"] });
    // Live lookup refresh (Part B): the report editor renders its field
    // sections from ["report-fields", reportTypeId].
    void queryClient.invalidateQueries({ queryKey: ["report-fields"] });
  };
```

- [ ] **Step 7: Typecheck + lint + affected specs**

Run: `cd clients/dashboard && npm run build && npm run lint && npx playwright test tests/patient-charts/diagnostic-codes.spec.ts tests/patient-charts/procedures-performed.spec.ts tests/patient-charts/problems.spec.ts`
Expected: clean / PASS.

- [ ] **Step 8: Commit**

```bash
git add clients/dashboard/src/pages/administration/custom-diagnostics.tsx clients/dashboard/src/pages/administration/diagnostics.tsx clients/dashboard/src/pages/administration/diagnostic-categories.tsx clients/dashboard/src/pages/administration/procedure-codes.tsx clients/dashboard/src/pages/administration/insurance-types.tsx clients/dashboard/src/pages/administration/macros.tsx
git commit -m "$(cat <<'EOF'
fix(dashboard): diagnostics/procedures/report-template edits refresh shared lookups

Custom diagnostics, ICD catalog, diagnostic categories, procedure codes,
insurance-type price associations, macros, report types, and report fields
now also invalidate the query keys their data is consumed under in the
diagnostic-codes dialog, problem/incident dialogs, SuperBill, macro
popover, and report editor. Part B audit rows 4, 6, 7, 11, 12, 16.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 8: New Playwright coverage — live lookup refresh (edit in admin dialog → chart lookup updates without reload)

**Files:**
- Create: `clients/dashboard/tests/administration/live-lookup-refresh.spec.ts`

**Interfaces:**
- Consumes: existing helpers; exercises the Task 3 sidebar button, the Task 2 dialog root, and the Task 6 departments invalidation fix end-to-end.

- [ ] **Step 1: Create the spec**

Create `clients/dashboard/tests/administration/live-lookup-refresh.spec.ts`:

```ts
// E2E coverage for Part B: editing a lookup record inside the global
// Administration dialog refreshes the same data where it's rendered
// elsewhere — here, the chart's Department filter combobox — without a
// manual reload. Exercises the departments invalidation fix
// (["administration.departmentOptions"]) through the real UI.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.Incidents.View",
  "Permissions.Administration.Departments.View",
  "Permissions.Administration.Departments.Update",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";

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

const DEPT = {
  id: "dept-1",
  name: "Physio",
  displayOrder: 0,
  isActive: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: null,
};

async function mockChartLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

test.describe("live lookup refresh", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChartLookups(page);
    await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([DEPT]));
  });

  test("renaming a department in the admin dialog updates the chart's filter options live", async ({
    page,
  }) => {
    await seedPatientWorkspace(page, [{ patientId: PATIENT_ID, patientLabel: "Alice Q Vance" }]);

    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Baseline: the Department filter combobox offers "Physio".
    await page.locator("#filter-dept").click();
    await expect(page.getByRole("menuitemradio", { name: "Physio" })).toBeVisible();
    await page.keyboard.press("Escape");

    // Open the Administration dialog over the chart, drill into Departments.
    await page.getByRole("button", { name: "Clinic Setup" }).click();
    await page.getByRole("button", { name: "Administration", exact: true }).click();
    const adminDialog = page.getByRole("dialog");
    await adminDialog.getByRole("button", { name: "Departments" }).click();
    await expect(adminDialog.getByText("Physio").first()).toBeVisible();

    // Re-register the GET mock FIRST (Playwright matches most-recent-first)
    // so the invalidation-triggered refetch sees the renamed department,
    // then register the method-filtered PUT mock on top.
    await mockJsonResponse(
      page,
      "**/api/v1/administration/departments**",
      paged([{ ...DEPT, name: "Physiotherapy", updatedAtUtc: "2026-07-10T00:00:00Z" }]),
    );
    await mockJsonResponse(page, "**/api/v1/administration/departments/" + DEPT.id, "", {
      method: "PUT",
    });

    // Edit "Physio" → "Physiotherapy" (row edit button → editor dialog).
    await adminDialog.getByRole("button", { name: "Edit Physio" }).click();
    const editDialog = page.getByRole("dialog"); // top-most modal; admin dialog is aria-hidden behind it
    await editDialog.locator("#dept-name").fill("Physiotherapy");
    await editDialog.getByRole("button", { name: "Save changes" }).click();

    // Close the Administration dialog (its content refreshed too).
    await expect(page.getByRole("dialog").getByText("Physiotherapy").first()).toBeVisible();
    await page.getByRole("dialog").getByRole("button", { name: "Close" }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);

    // The chart's Department filter now offers the renamed value — no reload.
    await page.locator("#filter-dept").click();
    await expect(page.getByRole("menuitemradio", { name: "Physiotherapy" })).toBeVisible();
    await expect(page.getByRole("menuitemradio", { name: "Physio", exact: true })).toHaveCount(0);
  });
});
```

- [ ] **Step 2: Run the spec**

Run: `cd clients/dashboard && npx playwright test tests/administration/live-lookup-refresh.spec.ts`
Expected: PASS. The row-edit button is `aria-label="Edit Physio"` and the editor input is `#dept-name` (verified against `departments.tsx`); the Combobox options render as `menuitemradio` (same pattern reports.spec.ts already relies on). Adjust selectors against real DOM if a name composition differs — new spec, no baseline.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/tests/administration/live-lookup-refresh.spec.ts
git commit -m "$(cat <<'EOF'
test(dashboard): E2E coverage for live lookup refresh from the admin dialog

Renames a department inside the global Administration dialog over an open
chart and asserts the chart's Department filter combobox reflects the new
name without a reload -- proves the Part B options-key invalidation works
through the real UI.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 9: `report-draft-store.ts` — localStorage draft persistence module

**Files:**
- Create: `clients/dashboard/src/state/report-draft-store.ts`

**Interfaces:**
- Produces: `ReportDraft`, `DRAFT_WRITE_DEBOUNCE_MS`, `readReportDraft(reportId)`, `writeReportDraft(reportId, draft)`, `clearReportDraft(reportId)`.
- Consumed by: `report-editor-panel.tsx` (Task 10) and the draft-persistence spec (Task 11).

- [ ] **Step 1: Create the store module**

Create `clients/dashboard/src/state/report-draft-store.ts`:

```ts
/**
 * Per-report localStorage drafts — unsaved report-editor typing survives
 * switching report tabs, navigating away, and full reloads (Part C).
 * Local-only by design (matches BackChart, which also never auto-saved to
 * the server); the draft for a report is cleared the moment a Save/Sign
 * succeeds, so a stale draft can never shadow newer server data.
 *
 * Keyed purely by reportId — deliberately NOT part of
 * patient-workspace-context.tsx (drafts don't need to know which patient
 * tab they belong to, and the workspace context shouldn't grow an
 * unrelated responsibility).
 *
 * Storage conventions match patient-workspace-context.tsx: fsh.dashboard.*
 * namespaced + versioned key, `typeof window` guard, try/catch on every
 * read/write.
 */

const STORAGE_KEY = "fsh.dashboard.reportDrafts.v1";

/** Debounce for draft writes on field changes (see report-editor-panel.tsx). */
export const DRAFT_WRITE_DEBOUNCE_MS = 500;

/**
 * Snapshot of the panel's editable form state — header fields, vitals
 * (kept as the input strings the panel holds, not parsed numbers), and the
 * per-field text map. Excludes state owned by separate flows (addendum
 * composer, reviewer picker, associated-problem checkboxes) — those save
 * through their own mutations and are not part of the Save Draft payload.
 */
export type ReportDraft = {
  reportDate: string;
  providerId: string | null;
  clinicId: string | null;
  isNoShow: boolean;
  height: string;
  weight: string;
  systolic: string;
  diastolic: string;
  pulse: string;
  temperature: string;
  /** report-field id → text (mirrors the panel's `values` map). */
  values: Record<number, string>;
};

type DraftMap = Record<string, ReportDraft>;

function isReportDraft(value: unknown): value is ReportDraft {
  if (!value || typeof value !== "object") return false;
  const v = value as Record<string, unknown>;
  return (
    typeof v.reportDate === "string" &&
    (v.providerId === null || typeof v.providerId === "string") &&
    (v.clinicId === null || typeof v.clinicId === "string") &&
    typeof v.isNoShow === "boolean" &&
    typeof v.height === "string" &&
    typeof v.weight === "string" &&
    typeof v.systolic === "string" &&
    typeof v.diastolic === "string" &&
    typeof v.pulse === "string" &&
    typeof v.temperature === "string" &&
    typeof v.values === "object" &&
    v.values !== null
  );
}

function readAll(): DraftMap {
  if (typeof window === "undefined") return {};
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== "object") return {};
    const out: DraftMap = {};
    for (const [id, draft] of Object.entries(parsed as Record<string, unknown>)) {
      if (isReportDraft(draft)) out[id] = draft;
    }
    return out;
  } catch {
    return {};
  }
}

function writeAll(map: DraftMap): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(map));
  } catch {
    /* storage unavailable (private browsing / quota) — drafts stay in-memory only */
  }
}

/** The stored draft for a report, or null when none exists. */
export function readReportDraft(reportId: string): ReportDraft | null {
  return readAll()[reportId] ?? null;
}

export function writeReportDraft(reportId: string, draft: ReportDraft): void {
  const map = readAll();
  map[reportId] = draft;
  writeAll(map);
}

/** Remove a report's draft (called after a successful Save/Sign — the
 *  server now holds the authoritative value). Idempotent. */
export function clearReportDraft(reportId: string): void {
  const map = readAll();
  if (!(reportId in map)) return;
  delete map[reportId];
  writeAll(map);
}
```

- [ ] **Step 2: Typecheck**

Run: `cd clients/dashboard && npm run build`
Expected: clean — nothing imports this module until Task 10.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/state/report-draft-store.ts
git commit -m "$(cat <<'EOF'
feat(dashboard): add report-draft-store (per-report localStorage drafts)

fsh.dashboard.reportDrafts.v1 holds Record<reportId, DraftFields> --
header fields + vitals + field-text map, validated on read, cleared on
successful Save/Sign. Local-only by design (matches BackChart; no server
autosave). Not yet wired -- the report editor panel consumes it next.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 10: Report editor → persistent panel + `chart.tsx` split-pane layout

One task (like the original plan's Task 5): the rename breaks `chart.tsx`'s import, so the panel conversion and the chart layout land as a single buildable unit.

**Files:**
- Rename: `clients/dashboard/src/pages/patient-charts/report-editor-dialog.tsx` → `clients/dashboard/src/pages/patient-charts/report-editor-panel.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/report-editor-panel.tsx` (drop Dialog wrapper, add draft persistence)
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx` (two-column split layout, inline panel mount)

**Interfaces:**
- Produces: `ReportEditorPanel({ patientId, reportId }: { patientId: string; reportId: string })` — no `open`/`onClose`: the panel has no close affordance of its own; which report renders (or none) is entirely the workspace context's `activeReportId`, driven by the pill strip.
- Consumes: `readReportDraft` / `writeReportDraft` / `clearReportDraft` / `DRAFT_WRITE_DEBOUNCE_MS` / `ReportDraft` (Task 9); `usePatientWorkspace` (unchanged).
- Unchanged: `report-search-dialog.tsx` (it only calls the workspace's `openReport` — it doesn't care whether the consumer renders a dialog or a panel); all other chart sub-entity dialogs (problems/allergies/medications/notes/documents/export/procedures) stay exactly as they are.

- [ ] **Step 1: Rename the file**

```bash
git mv clients/dashboard/src/pages/patient-charts/report-editor-dialog.tsx clients/dashboard/src/pages/patient-charts/report-editor-panel.tsx
```

- [ ] **Step 2: Update the panel's imports**

In `report-editor-panel.tsx`, remove the Dialog import. Replace:

```tsx
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
```

with:

```tsx
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
```

Add the draft-store import. Replace:

```tsx
import { REPORT_PERMISSIONS, SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
```

with:

```tsx
import { REPORT_PERMISSIONS, SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
import {
  clearReportDraft,
  DRAFT_WRITE_DEBOUNCE_MS,
  readReportDraft,
  writeReportDraft,
  type ReportDraft,
} from "@/state/report-draft-store";
```

- [ ] **Step 3: Rename the component and drop the dialog props**

Replace:

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
```

with:

```tsx
export function ReportEditorPanel({
  patientId,
  reportId,
}: {
  patientId: string;
  reportId: string;
}) {
```

- [ ] **Step 4: Draft persistence — hydration, debounced write, unmount flush**

Replace the existing hydrate-from-report effect:

```tsx
  useEffect(() => {
    if (!report) return;
    setReportDate(report.reportDate.slice(0, 10));
    setProviderId(report.providerId ?? null);
    setClinicId(report.clinicId ?? null);
    setIsNoShow(report.isNoShow);
    setHeight(report.vitals.heightInches != null ? String(report.vitals.heightInches) : "");
    setWeight(report.vitals.weightLbs != null ? String(report.vitals.weightLbs) : "");
    setSystolic(report.vitals.systolic != null ? String(report.vitals.systolic) : "");
    setDiastolic(report.vitals.diastolic != null ? String(report.vitals.diastolic) : "");
    setPulse(report.vitals.pulse != null ? String(report.vitals.pulse) : "");
    setTemperature(report.vitals.temperatureF != null ? String(report.vitals.temperatureF) : "");
    setReviewerProviderId(report.reviewerProviderId ?? null);
    setAssociatedProblemIds(report.associatedProblemIds ?? []);
    const map: Record<number, string> = {};
    for (const fv of report.fieldValues) map[fv.reportFieldId] = fv.text;
    setValues(map);
  }, [report]);
```

with:

```tsx
  // ─── Draft persistence (Part C) ───
  // `suppress` arms before hydration: the snapshot change hydration causes
  // must NOT be written back as a draft — an untouched draft equal to the
  // server copy would later shadow genuinely newer server data.
  const suppressDraftWriteRef = useRef(false);
  const draftDirtyRef = useRef(false);

  useEffect(() => {
    if (!report) return;
    suppressDraftWriteRef.current = true;
    draftDirtyRef.current = false;
    const draft = report.isSigned ? null : readReportDraft(reportId);
    if (draft) {
      // Unsaved local edits win over the server copy until Save/Sign.
      setReportDate(draft.reportDate);
      setProviderId(draft.providerId);
      setClinicId(draft.clinicId);
      setIsNoShow(draft.isNoShow);
      setHeight(draft.height);
      setWeight(draft.weight);
      setSystolic(draft.systolic);
      setDiastolic(draft.diastolic);
      setPulse(draft.pulse);
      setTemperature(draft.temperature);
      setValues(draft.values);
    } else {
      setReportDate(report.reportDate.slice(0, 10));
      setProviderId(report.providerId ?? null);
      setClinicId(report.clinicId ?? null);
      setIsNoShow(report.isNoShow);
      setHeight(report.vitals.heightInches != null ? String(report.vitals.heightInches) : "");
      setWeight(report.vitals.weightLbs != null ? String(report.vitals.weightLbs) : "");
      setSystolic(report.vitals.systolic != null ? String(report.vitals.systolic) : "");
      setDiastolic(report.vitals.diastolic != null ? String(report.vitals.diastolic) : "");
      setPulse(report.vitals.pulse != null ? String(report.vitals.pulse) : "");
      setTemperature(report.vitals.temperatureF != null ? String(report.vitals.temperatureF) : "");
      const map: Record<number, string> = {};
      for (const fv of report.fieldValues) map[fv.reportFieldId] = fv.text;
      setValues(map);
    }
    // Never drafted — these save through their own mutations (Request
    // Review / Save Associated Problems), so the server copy always wins.
    setReviewerProviderId(report.reviewerProviderId ?? null);
    setAssociatedProblemIds(report.associatedProblemIds ?? []);
  }, [report, reportId]);

  // One snapshot of everything the draft persists — the ONLY draft-write
  // instrumentation point (spec: instrument once, not per input).
  const draftSnapshot = useMemo<ReportDraft>(
    () => ({
      reportDate,
      providerId,
      clinicId,
      isNoShow,
      height,
      weight,
      systolic,
      diastolic,
      pulse,
      temperature,
      values,
    }),
    [
      reportDate,
      providerId,
      clinicId,
      isNoShow,
      height,
      weight,
      systolic,
      diastolic,
      pulse,
      temperature,
      values,
    ],
  );
  const draftSnapshotRef = useRef(draftSnapshot);
  draftSnapshotRef.current = draftSnapshot;

  const isSignedRef = useRef(isSigned);
  isSignedRef.current = isSigned;

  // Debounced draft write on any form change. The hydration effect above
  // arms `suppress`, so the snapshot change IT causes is skipped; only
  // real typing marks the draft dirty and schedules a write.
  useEffect(() => {
    if (suppressDraftWriteRef.current) {
      suppressDraftWriteRef.current = false;
      return;
    }
    if (isSignedRef.current || !canUpdate) return;
    draftDirtyRef.current = true;
    const t = window.setTimeout(() => {
      writeReportDraft(reportId, draftSnapshotRef.current);
      draftDirtyRef.current = false;
    }, DRAFT_WRITE_DEBOUNCE_MS);
    return () => window.clearTimeout(t);
  }, [draftSnapshot, reportId, canUpdate]);

  // Flush a pending (sub-debounce) write when the panel unmounts — e.g.
  // switching to another open report's pill, or client-side navigation
  // away from the chart. (A hard reload mid-debounce can lose <500ms of
  // typing — accepted; effect cleanups don't run on browser unload.)
  useEffect(() => {
    return () => {
      if (draftDirtyRef.current) {
        writeReportDraft(reportId, draftSnapshotRef.current);
        draftDirtyRef.current = false;
      }
    };
  }, [reportId]);
```

Clear the draft once the server holds the authoritative value. In `saveMutation`, replace:

```tsx
    onSuccess: () => {
      toast.success("Report saved.");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
```

with:

```tsx
    onSuccess: () => {
      toast.success("Report saved.");
      clearReportDraft(reportId);
      draftDirtyRef.current = false;
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
```

In `signMutation`, replace:

```tsx
    onSuccess: () => {
      toast.success("Report signed.");
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
```

with:

```tsx
    onSuccess: () => {
      toast.success("Report signed.");
      clearReportDraft(reportId);
      draftDirtyRef.current = false;
      void queryClient.invalidateQueries({ queryKey: ["report", reportId] });
      void queryClient.invalidateQueries({ queryKey: ["reports"] });
    },
```

- [ ] **Step 5: Strip the Dialog shell**

Replace the `dialogOnOpenChange` helper and both early returns:

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

with:

```tsx
  if (reportQuery.isLoading) {
    return <div data-testid="report-editor-panel" className="skeleton h-64 rounded-xl" />;
  }

  if (!report) {
    return (
      <div
        data-testid="report-editor-panel"
        className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-6 text-[13px] text-[var(--color-muted-foreground)]"
      >
        Report not found.
      </div>
    );
  }
```

Replace the top of the main return:

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

with:

```tsx
  return (
    <div data-testid="report-editor-panel" className="min-w-0 space-y-4 sm:space-y-6">
      {/* Patient + status strip */}
```

Replace the end of the function:

```tsx
      <ImportMedicationsDialog
        patientId={patientId}
        open={medicationsFieldId != null}
        onClose={() => setMedicationsFieldId(null)}
        onDone={(text) => {
          if (medicationsFieldId != null) insertMacro(medicationsFieldId, text);
        }}
      />
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

with:

```tsx
      <ImportMedicationsDialog
        patientId={patientId}
        open={medicationsFieldId != null}
        onClose={() => setMedicationsFieldId(null)}
        onDone={(text) => {
          if (medicationsFieldId != null) insertMacro(medicationsFieldId, text);
        }}
      />
    </div>
  );
}
```

> Everything between the edited regions (header fields, vitals, field sections + per-field affordances, associated problems, signature, peer review, addendums, sticky action bar, and the three nested dialogs — Procedures Performed, Import Allergies, Import Medications) is byte-for-byte unchanged. The nested dialogs stay real Radix dialogs — they modal over the inline panel exactly as they modaled over the report dialog. `data-testid="report-editor-panel"` is the panel's stable test hook now that there is no `role="dialog"` to scope selectors by.

- [ ] **Step 6: `chart.tsx` — import swap + rail column constant**

Replace:

```tsx
import { ReportEditorDialog } from "@/pages/patient-charts/report-editor-dialog";
```

with:

```tsx
import { ReportEditorPanel } from "@/pages/patient-charts/report-editor-panel";
```

Replace:

```tsx
const DESKTOP_COLS = "grid-cols-[1fr_1fr_1fr_84px_72px_auto]";
```

with:

```tsx
/** Compact incident-row grid for the left rail: content column + actions. */
const RAIL_COLS = "grid-cols-[minmax(0,1fr)_auto]";
```

Remove `EntityListHeader` from the `@/components/list` import (the compact rail rows have no column header). Replace:

```tsx
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
```

with:

```tsx
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityListCard,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntityStatusBadge,
} from "@/components/list";
```

- [ ] **Step 7: `chart.tsx` — replace the layout region with the split pane**

Replace the ENTIRE region from the line

```tsx
      <div className="grid gap-4 lg:grid-cols-[330px_1fr]">
```

through the closing `)}` of the `{canViewReports && ( ... )}` Patient Reports card (the line immediately above the `{/* Create dialog */}` comment) with the block below. The Patient Info and Incident-shortcuts cards inside it are byte-for-byte today's markup — only their surrounding structure changed; the incident list is re-laid as compact two-line rows; the Patient Reports card moves into the rail minus its pill row; the pill row and the inline panel form the new right column.

```tsx
      <div className="grid gap-4 lg:grid-cols-[400px_minmax(0,1fr)]">
        {/* ─── Left rail: patient info + incident shortcuts + incident list +
            reports list (Part C: the incident list narrows into a rail; the
            right column is the persistent report workspace) ─── */}
        <div className="min-w-0 space-y-4">
          {/* Patient Info card */}
          {patientQuery.isLoading ? (
            <div className="skeleton h-64 rounded-xl" />
          ) : patient ? (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[13px]">
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  Patient Info
                </h2>
                <div className="flex items-center gap-2">
                  <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
                    {patient.isActive ? "Active" : "Inactive"}
                  </EntityStatusBadge>
                  <Link
                    to={`/patients/${patientId}`}
                    title="Edit patient info"
                    aria-label="Edit patient info"
                    className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <Pencil className="size-4" />
                  </Link>
                </div>
              </div>

              <p className="text-[15px] font-semibold leading-tight">{fullName}</p>

              <div className="mt-3 space-y-1.5">
                <SidebarRow label="Code" value={patient.patientCode} />
                <SidebarRow
                  label="DOB"
                  value={`${formatDate(patient.demographics.dateOfBirth)} · ${ageFromDob(patient.demographics.dateOfBirth)}y`}
                />
                <SidebarRow label="Gender" value={patient.demographics.gender || "—"} />
              </div>

              <div className="mt-3 rounded-lg border border-[var(--color-border)] p-2">
                <p className="text-[11px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                  Insurance
                </p>
                <p className="mt-0.5 text-[13px]">
                  {patient.insurance?.insuredFullName || "—"}
                </p>
              </div>

              {patient.demographics.medicalAlertNotes && (
                <div className="mt-3 rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.3)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-[12px] font-medium text-[var(--color-destructive)]">
                  ⚠ Medical Alert: {patient.demographics.medicalAlertNotes}
                </div>
              )}

              {/* Appointments / visit dates */}
              <div className="mt-3 flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                <CalendarDays className="size-3.5" />
                Appointments
              </div>
              <div className="mt-1.5 space-y-1.5">
                <SidebarRow label="Last Visit" value={formatDate(patient.lastVisitDate)} />
                <SidebarRow label="Next Visit" value={formatDate(patient.nextVisitDate)} />
              </div>
            </div>
          ) : (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[13px] text-[var(--color-muted-foreground)]">
              Patient not found.
            </div>
          )}

          {/* Incident shortcuts card */}
          <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 text-[13px]">
            <div className="mb-2 flex items-center justify-between">
              <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                Incident
              </h2>
              <div className="flex items-center gap-1.5">
                {canCreate && (
                  <IconShortcut label="Add incident" onClick={() => setCreateOpen(true)}>
                    <Plus className="size-4" />
                  </IconShortcut>
                )}
                {canUpdate && (
                  <IconShortcut
                    label="Edit selected incident"
                    disabled={!activeIncident}
                    onClick={() => activeIncident && setEditIncidentId(activeIncident.id)}
                  >
                    <Pencil className="size-4" />
                  </IconShortcut>
                )}
                <IconShortcut
                  label="View selected incident"
                  disabled={!activeIncident}
                  onClick={() => activeIncident && setViewIncidentId(activeIncident.id)}
                >
                  <Eye className="size-4" />
                </IconShortcut>
                <IconShortcut
                  label="Search patient reports"
                  disabled={!activeIncident}
                  onClick={() => setReportSearchOpen(true)}
                >
                  <FileSearch className="size-4" />
                </IconShortcut>
              </div>
            </div>

            {activeIncident ? (
              <div className="space-y-1.5 rounded-lg border border-[var(--color-border)] p-2.5">
                <SidebarRow
                  label="Date of Initial Visit"
                  value={formatDate(activeIncident.dateOfInitialVisit)}
                />
                <SidebarRow label="Date of Loss" value={formatDate(activeIncident.dateOfLoss)} />
                <SidebarRow
                  label="Incident Type"
                  value={resolveLabel(activeIncident.incidentTypeId, incidentTypeOptions)}
                />
                <SidebarRow
                  label="Department"
                  value={resolveLabel(activeIncident.departmentId, departmentOptions)}
                />
                <div className="flex flex-wrap gap-1.5 pt-1">
                  <EntityStatusBadge tone={activeIncident.isClosed ? "default" : "success"}>
                    {activeIncident.isClosed ? "Closed" : "Open"}
                  </EntityStatusBadge>
                  {activeIncident.isTransfer && (
                    <EntityStatusBadge tone="info">Transfer</EntityStatusBadge>
                  )}
                  {activeIncident.isAccident && (
                    <EntityStatusBadge tone="warning">Accident</EntityStatusBadge>
                  )}
                </div>
              </div>
            ) : (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                No incident selected. Add one or pick a row from the list.
              </p>
            )}
          </div>

          {/* Incidents — compact rail list: same data, filters, and row
              actions as the old full-width table, re-laid as two-line rows
              for the narrow column. */}
          <div>
            <EntityPageHeader
              icon={ClipboardList}
              title="Incidents"
              total={incidents.length}
              unit="incident"
              description="Clinical incidents (episodes of care) for this patient."
            >
              {canCreate && (
                <Button
                  onClick={() => setCreateOpen(true)}
                  className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
                >
                  <Plus className="size-4" />
                  Add Incident
                </Button>
              )}
            </EntityPageHeader>

            {/* Filters — stacked for the rail */}
            <div className="mt-3 space-y-2">
              <EntityFilterPill
                label="Status"
                value={closedFilter}
                onChange={(v) => setClosedFilter(v as ClosedFilter)}
                options={[
                  { value: "all", label: "All" },
                  { value: "open", label: "Open" },
                  { value: "closed", label: "Closed" },
                ]}
              />
              <div className="grid grid-cols-2 gap-2">
                <Combobox
                  id="filter-type"
                  label="Incident type"
                  value={typeFilter}
                  onChange={setTypeFilter}
                  options={incidentTypeOptions ?? []}
                  placeholder="All types"
                />
                <Combobox
                  id="filter-dept"
                  label="Department"
                  value={deptFilter}
                  onChange={setDeptFilter}
                  options={departmentOptions ?? []}
                  placeholder="All departments"
                />
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={transferOnly}
                    onChange={(e) => setTransferOnly(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Transfers only</span>
                </label>
                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={showDeleted}
                    onChange={(e) => setShowDeleted(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Show deleted</span>
                </label>
              </div>
            </div>

            <div className="mt-4">
              {incidentsQuery.isLoading && allIncidents.length === 0 ? (
                <EntityListLoading desktopColumns={RAIL_COLS} />
              ) : incidents.length === 0 ? (
                <EntityEmpty
                  icon={ClipboardList}
                  title="No incidents"
                  body={
                    closedFilter !== "all" || deptFilter || typeFilter || transferOnly || showDeleted
                      ? "No incidents match the current filters."
                      : "No incidents have been recorded for this patient yet."
                  }
                  action={
                    canCreate ? (
                      <Button
                        onClick={() => setCreateOpen(true)}
                        className="h-9 rounded-lg px-4 text-[13px]"
                      >
                        <Plus className="mr-1.5 size-4" />
                        Add Incident
                      </Button>
                    ) : undefined
                  }
                />
              ) : (
                <EntityListCard>
                  {incidents.map((incident, i) => (
                    <EntityListRow
                      key={incident.id}
                      className={`${RAIL_COLS} cursor-pointer ${
                        incident.id === activeIncidentId ? "bg-[var(--color-accent)]" : ""
                      }`}
                      isLast={i === incidents.length - 1}
                      onClick={() => patientId && setActiveIncident(patientId, incident.id)}
                    >
                      <div className="min-w-0">
                        <p className="text-[13px] font-medium">
                          {formatDate(incident.dateOfLoss)}
                          <span className="text-[var(--color-muted-foreground)]">
                            {" · "}
                            {resolveLabel(incident.incidentTypeId, incidentTypeOptions)}
                          </span>
                        </p>
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {resolveLabel(incident.departmentId, departmentOptions)}
                        </p>
                        <div className="mt-1 flex flex-wrap gap-1.5">
                          <EntityStatusBadge tone={incident.isClosed ? "default" : "success"}>
                            {incident.isClosed ? "Closed" : "Open"}
                          </EntityStatusBadge>
                          {incident.isTransfer && (
                            <EntityStatusBadge tone="info">Transfer</EntityStatusBadge>
                          )}
                        </div>
                      </div>
                      <div
                        className="flex items-start justify-end gap-1"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <IconShortcut label="View incident" onClick={() => setViewIncidentId(incident.id)}>
                          <Eye className="size-4" />
                        </IconShortcut>
                        {canUpdate && (
                          <IconShortcut
                            label="Edit incident"
                            onClick={() => setEditIncidentId(incident.id)}
                          >
                            <Pencil className="size-4" />
                          </IconShortcut>
                        )}
                        {canClose && !incident.isClosed && (
                          <IconShortcut
                            label="Close incident"
                            disabled={closeMutation.isPending}
                            onClick={() => closeMutation.mutate(incident.id)}
                          >
                            <Lock className="size-4" />
                          </IconShortcut>
                        )}
                        {canDelete && (
                          <IconShortcut
                            label="Delete incident"
                            tone="destructive"
                            disabled={deleteMutation.isPending}
                            onClick={() => deleteMutation.mutate(incident.id)}
                          >
                            <Trash2 className="size-4" />
                          </IconShortcut>
                        )}
                      </div>
                    </EntityListRow>
                  ))}
                </EntityListCard>
              )}
            </div>
          </div>

          {/* Patient Reports (for the active incident) — Add Report + rows.
              The open-report pill strip moved to the right panel. */}
          {canViewReports && (
            <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h2 className="flex items-center gap-1.5 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  <FileText className="size-3.5" />
                  Patient Reports
                </h2>
                {canCreateReports && (
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild disabled={!activeIncident || createReportMutation.isPending}>
                      <Button
                        size="sm"
                        className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                        disabled={!activeIncident || createReportMutation.isPending}
                      >
                        <FilePlus className="size-4" />
                        Add Report
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="max-h-[min(340px,55vh)] w-56 overflow-y-auto">
                      <DropdownMenuLabel>Report Type</DropdownMenuLabel>
                      {(reportTypesQuery.data ?? []).length === 0 ? (
                        <p className="px-3 py-3 text-[12px] text-[var(--color-muted-foreground)]">
                          No report types defined.
                        </p>
                      ) : (
                        (reportTypesQuery.data ?? []).map((t) => (
                          <DropdownMenuItem key={t.id} onSelect={() => onAddReport(t.id)}>
                            {t.name}
                          </DropdownMenuItem>
                        ))
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                )}
              </div>

              {!activeIncident ? (
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  Select an incident to view its reports.
                </p>
              ) : reportsQuery.isLoading ? (
                <div className="skeleton h-16 rounded-lg" />
              ) : (reportsQuery.data?.items ?? []).length === 0 ? (
                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  No reports for this incident yet.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {(reportsQuery.data?.items ?? []).map((r) => (
                    <li key={r.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                      <div className="min-w-0">
                        <p className="truncate text-[13px] font-medium">{reportTypeLabel(r.reportTypeId)}</p>
                        <p className="text-[12px] text-[var(--color-muted-foreground)]">
                          {formatDate(r.reportDate)}
                          {r.signedByName ? ` · Signed by ${r.signedByName}` : ""}
                        </p>
                      </div>
                      <div className="flex items-center gap-2">
                        <EntityStatusBadge tone={r.isSigned ? "info" : "default"}>
                          {r.workflowStatus}
                        </EntityStatusBadge>
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
                          {canDeleteReports && (
                            <IconShortcut
                              label="Delete report"
                              tone="destructive"
                              disabled={deleteReportMutation.isPending}
                              onClick={() => deleteReportMutation.mutate(r.id)}
                            >
                              <Trash2 className="size-4" />
                            </IconShortcut>
                          )}
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          )}
        </div>

        {/* ─── Right: persistent report workspace (Part C) — pill tab strip
            + the active report's editor rendered INLINE (no dialog), or an
            empty state. Switching pills / navigating never closes a report;
            only a pill's explicit × removes it from openReportIds. ─── */}
        <div className="min-w-0">
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

          {patientId && activeReportId ? (
            <ReportEditorPanel patientId={patientId} reportId={activeReportId} />
          ) : (
            <div className="grid min-h-[280px] place-items-center rounded-xl border border-dashed border-[var(--color-border)] bg-[var(--color-card)] p-8 text-center">
              <div>
                <FileText className="mx-auto size-8 text-[var(--color-muted-foreground)]" />
                <p className="mt-2 text-[14px] font-medium">No report open</p>
                <p className="mt-1 text-[12px] text-[var(--color-muted-foreground)]">
                  Select or add a report from the Patient Reports list.
                </p>
              </div>
            </div>
          )}
        </div>
      </div>
```

- [ ] **Step 8: `chart.tsx` — remove the old dialog mount**

Delete the block at the end of the JSX (the panel is mounted inline by Step 7):

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

> `setActiveReport` remains in use (the pill buttons call it), so the `usePatientWorkspace()` destructure is unchanged. There is no longer a "close the editor but keep the pill" affordance — the panel is persistent by design; a pill's × (`closeReport`) is the only way to take a report off screen, and `closeAndPickFallback` inside the context picks the next active pill.

- [ ] **Step 9: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean — no references to `ReportEditorDialog` remain; `EntityListHeader` / `DESKTOP_COLS` fully removed; `Dialog`/`DialogContent`/`DialogTitle` imports gone from the panel.

- [ ] **Step 10: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/report-editor-panel.tsx clients/dashboard/src/pages/patient-charts/chart.tsx
git commit -m "$(cat <<'EOF'
feat(dashboard): report editor becomes a persistent split-pane panel with local drafts

report-editor-dialog.tsx -> report-editor-panel.tsx: Dialog wrapper
removed, same data fetching/mutations/fields, rendered inline in
chart.tsx's new [400px|1fr] layout -- incident list (same filters and row
actions) narrows into a left rail with the reports list; the right panel
hosts the open-report pill strip + the active report's editor (or an
empty state). Unsaved typing persists to fsh.dashboard.reportDrafts.v1
(debounced 500ms, flushed on unmount, hydrated on mount, cleared on
successful Save/Sign) so it survives tab switches, navigation, and
reloads. Local-only -- no server autosave, matching BackChart.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
EOF
)"
```

---

### Task 11: Migrate report specs to the panel + new draft-persistence spec

**Files:**
- Modify: `clients/dashboard/tests/patient-charts/reports.spec.ts`
- Create: `clients/dashboard/tests/patient-charts/report-drafts.spec.ts`
- Verify (run, likely unchanged): `clients/dashboard/tests/patient-charts/diagnostic-codes.spec.ts`, `procedures-performed.spec.ts`, `select-appointment.spec.ts`

**Interfaces:**
- Consumes: `seedPatientWorkspace` (existing helper), the Task 10 panel's `data-testid="report-editor-panel"`.

- [ ] **Step 1: `reports.spec.ts` — the report is inline content now, not a dialog**

The workspace seeding + chart-shell mocks from the previous migration still apply verbatim; only the `role="dialog"` scoping changes. Rename the helper to match reality. Replace:

```ts
/** Seed the workspace so the chart page opens with the report dialog
 *  already active, then navigate to the chart (not the old report route,
 *  which no longer exists). */
async function gotoReportDialog(page: Page): Promise<void> {
```

with:

```ts
/** Seed the workspace so the chart page opens with the report already
 *  active in the right-hand panel, then navigate to the chart. */
async function gotoReportPanel(page: Page): Promise<void> {
```

Replace every `await gotoReportDialog(page);` call in the file (6 occurrences) with `await gotoReportPanel(page);`.

Update the one dialog-scoped assertion (the chart's Patient Info card and the panel's status strip both render the patient name; scope by the panel's testid instead of the removed `role="dialog"`). Replace:

```ts
    await expect(page.getByRole("dialog").getByText("Alice Q Vance", { exact: true })).toBeVisible();
```

with:

```ts
    await expect(
      page.getByTestId("report-editor-panel").getByText("Alice Q Vance", { exact: true }),
    ).toBeVisible();
```

> The "create-new-macro" test's `page.getByRole("dialog")` is untouched — the macro create dialog is now the ONLY dialog on screen (previously Radix hid the report dialog behind it via `aria-hidden`), so the locator resolves identically.

- [ ] **Step 2: Create `report-drafts.spec.ts`**

Create `clients/dashboard/tests/patient-charts/report-drafts.spec.ts`:

```ts
// E2E coverage for Part C's draft persistence: unsaved report typing
// survives switching between open report tabs (unmount flush) and a full
// reload (debounced localStorage write), and a successful Save clears the
// draft so it can't shadow the server copy.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const REPORT_PERMS = [
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Reports.Update",
  "Permissions.Patient.Incidents.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3333";
const REPORT_1 = "00000000-0000-0000-0000-0000000d4444"; // Initial Evaluation (type 1, field 11)
const REPORT_2 = "00000000-0000-0000-0000-0000000d5555"; // Progress Note (type 2, field 21)

const DRAFTS_KEY = "fsh.dashboard.reportDrafts.v1";

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

const REPORT_TYPES = [
  { id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true },
  { id: 2, name: "Progress Note", displayOrder: 1, isActive: true },
];

// Per-type field sets so each report's textarea has a distinct id.
const FIELDS_TYPE_1 = [
  { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
];
const FIELDS_TYPE_2 = [
  { id: 21, reportTypeId: 2, name: "Progress", category: "Subjective", displayOrder: 0, isActive: true },
];

function report(id: string, reportTypeId: number) {
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

async function mockLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  // The panel queries report-fields?reportTypeId=N — route on the param.
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=1**", FIELDS_TYPE_1);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=2**", FIELDS_TYPE_2);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  await mockJsonResponse(
    page,
    "**/api/v1/patient/reports**",
    paged([report(REPORT_1, 1), report(REPORT_2, 2)]),
  );
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_1, report(REPORT_1, 1));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_2, report(REPORT_2, 2));
}

async function seedBothReportsOpen(page: Page) {
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_ID,
      openReportIds: [REPORT_1, REPORT_2],
      activeReportId: REPORT_1,
    },
  ]);
}

/** The parsed draft map from localStorage (empty object when unset). */
async function readDraftMap(page: Page): Promise<Record<string, unknown>> {
  return page.evaluate((key) => {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as Record<string, unknown>) : {};
  }, DRAFTS_KEY);
}

test.describe("report drafts", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", REPORT_PERMS);
    await mockLookups(page);
  });

  test("typing survives switching between open report tabs", async ({ page }) => {
    await seedBothReportsOpen(page);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    await page.locator("#f-11").fill("unsaved chief complaint text");

    // Switch to the second open report (unmounts the panel → flush write).
    await page.getByRole("button", { name: "Progress Note", exact: true }).click();
    await expect(page.locator("#f-21")).toBeVisible();
    await expect(page.locator("#f-21")).toHaveValue("");

    // Switch back — the draft (not the empty server copy) hydrates.
    await page.getByRole("button", { name: "Initial Evaluation", exact: true }).click();
    await expect(page.locator("#f-11")).toHaveValue("unsaved chief complaint text");
  });

  test("typing survives a full reload via the debounced localStorage write", async ({ page }) => {
    await seedBothReportsOpen(page);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    await page.locator("#f-11").fill("text that must survive a reload");

    // Wait for the 500ms debounce to land in localStorage (a hard
    // navigation skips React cleanup, so the flush-on-unmount can't help).
    await expect
      .poll(async () => {
        const map = await readDraftMap(page);
        return map[REPORT_1] != null;
      })
      .toBe(true);

    await page.reload();

    await expect(page.locator("#f-11")).toHaveValue("text that must survive a reload");
  });

  test("a successful Save clears the report's draft", async ({ page }) => {
    await seedBothReportsOpen(page);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    await page.locator("#f-11").fill("about to be saved");
    await expect
      .poll(async () => {
        const map = await readDraftMap(page);
        return map[REPORT_1] != null;
      })
      .toBe(true);

    // Method-filtered PUT mock layers over the GET detail mock.
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_1, '""', { method: "PUT" });
    await page.getByRole("button", { name: /save draft/i }).click();
    await expect(page.getByText("Report saved.")).toBeVisible();

    await expect
      .poll(async () => {
        const map = await readDraftMap(page);
        return map[REPORT_1] == null;
      })
      .toBe(true);
  });
});
```

- [ ] **Step 3: Run the report-surface specs**

Run: `cd clients/dashboard && npx playwright test tests/patient-charts/reports.spec.ts tests/patient-charts/report-drafts.spec.ts tests/patient-charts/diagnostic-codes.spec.ts tests/patient-charts/procedures-performed.spec.ts tests/patient-charts/select-appointment.spec.ts`
Expected: PASS. `diagnostic-codes` / `procedures-performed` / `select-appointment` drive nested dialogs that are still real dialogs and locate the Plan-field button on the page — they should pass unmodified; if one asserts against content that now duplicates between the rail and the panel, scope it with `page.getByTestId("report-editor-panel")`.

- [ ] **Step 4: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/tests/patient-charts/reports.spec.ts clients/dashboard/tests/patient-charts/report-drafts.spec.ts
git commit -m "$(cat <<'EOF'
test(dashboard): migrate report specs to the inline panel + draft coverage

reports.spec.ts scopes by the panel's data-testid instead of the removed
role=dialog. New report-drafts.spec.ts: typing survives switching open
report tabs (unmount flush) and a full reload (debounced write); a
successful Save clears the localStorage draft.

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
Expected: PASS — every existing spec plus the new/updated ones from Tasks 4, 5, 8, and 11. Specs most likely to surface regressions: `patient-charts/*` (the chart re-layout), `administration/diagnostics.spec.ts` (the bridge), and anything asserting sidebar structure.

- [ ] **Step 3: Manual smoke (recommended)**

Start the API (`dotnet run --project src/Host/FSH.Starter.Api`) and dashboard (`cd clients/dashboard && npm run dev`). Verify:

- Open a patient chart with a report open → sidebar Administration → dialog opens ON TOP; the chart (URL, active incident, open report, any half-typed field) is untouched behind it; close → everything exactly as left.
- Hub → section → "← Administration" back → different section → close; reopen — it starts at the hub again (state is ephemeral by design).
- Paste `/administration/providers` in the address bar → Overview loads with the Providers dialog already open; `/administration` alone → hub dialog.
- Rename a Diagnostic Category (or Department) in the dialog while a report with that dropdown is open → the dropdown shows the new name without a reload.
- Chart: incidents render in the left rail (filters, add/edit/view/close/delete, transfer badge all work); reports list below; clicking View/Edit on a report renders it inline on the right; pills switch between two open reports; a pill's × removes it and falls back to the neighbor.
- Type into a report field, switch pills, switch back — text preserved. Navigate to Overview and back via the patient tab — still preserved. Reload the browser — still preserved. Save Draft → reload — the saved (server) value renders and the draft is gone from `localStorage["fsh.dashboard.reportDrafts.v1"]`. Sign — same clearing.
- Mobile drawer: Administration entry opens the dialog and dismisses the drawer.

- [ ] **Step 4: Report status**

Note any flaky selectors in the Task 5/8/11 specs (accessible names are the biggest risk in a plan written without running the app) and fix them in a follow-up commit rather than leaving the suite red.

---

### Task 13: Docs + changelog (golden rule 10)

**Files:**
- Modify: the separate docs repo (`github.com/fullstackhero/docs`) — dashboard navigation / patient-chart / administration pages.
- Create: a changelog entry under `src/content/docs/changelog/` in that docs repo.

- [ ] **Step 1: Update the docs site**

In the docs repo, update the pages documenting the dashboard's Administration and Patient Chart to describe: Administration is a global dialog opened from the sidebar (no page navigation; whatever is on screen stays put), with `/administration[/:section]` deep links still working via a redirect-to-Overview bridge; admin edits refresh their lookups live everywhere they're displayed; the patient chart is a split-pane — incident rail on the left, persistent report workspace (tab strip + inline editor) on the right; unsaved report typing is kept as a browser-local draft until Save/Sign (per-browser only, no server autosave).

- [ ] **Step 2: Add a changelog entry**

Add a dated entry under `src/content/docs/changelog/` summarizing: global Administration dialog, live lookup refresh, persistent report panel with local drafts.

- [ ] **Step 3: Commit in the docs repo**

```bash
git add src/content/docs
git commit -m "docs: global Administration dialog, live lookup refresh, persistent report panel"
```

> The docs repo is **not checked out** in this workspace (verified — same situation as the previous plan's Task 13). Flag this task to the user as an explicit follow-up rather than skipping it silently; the golden rule requires docs to travel with the change.

---

## Self-Review

**Spec coverage (Part A — Administration as a global dialog):**
- New `administration-dialog-context.tsx` with the spec's exact `AdminDialogView` union + `openHub`/`openSection`/`backToHub`/`close`, no localStorage → Task 1. ✓
- New `administration-dialog-root.tsx` mounted once in `AppShell`, ONE `Dialog`, hub view = extracted grid (no `useParams`/`useNavigate` inside it), section view = registry-backed lazy component + "← Administration" back affordance calling `backToHub()` → Tasks 2-3. ✓
- Reuses `section-registry.ts` and `nav-data.ts`'s `administrationHubItems`/`ALL_ADMINISTRATION_PERMISSIONS` untouched → no task modifies them (verified: only `sidebar.tsx` renders the entry differently). ✓
- Sidebar special-cases the `to === "/administration"` item as a `<button onClick={openHub}>` (all three renders — expanded, collapsed, mobile drawer — via the shared `NavItemLink`), mirroring the `resolvePatientChartLink` precedent → Task 3. ✓
- `routes.tsx`: both admin routes → `AdminDeepLinkOpener` (opens section or hub, then `navigate("/", { replace: true })`); `schedule`/`email-settings` literal routes untouched → Task 4. ✓ `AdminSectionDialog`'s logic kept (absorbed into the root), file deleted → Tasks 2/4. ✓ Command palette verified to have no `/administration` entry — nothing else deep-links.
- Spec's test asks: dialog over an open chart without unmounting it; deep link behavior → Task 5. ✓

**Spec coverage (Part B — live lookup refresh):**
- "Real per-entity audit rather than a blind find-replace" → the 17-row table (built from the actual `invalidateQueries`/`queryKey` call sites) lives in the plan preamble; 10 entities fixed (Tasks 6-7), 7 explicitly verified as already-correct or consumer-less with the reason recorded. ✓
- Spec's "prefix-widen where keys share a prefix" case turned out to already hold everywhere it applies (e.g. patient-document-types' `…,"options"` key); every actual gap is a *different-prefix* key, fixed with explicit second invalidates exactly as the spec's fallback prescribes — no key renames, no broken call sites. ✓
- Spec's test asks: edit a lookup in Administration → an open page's dropdown refreshes without reload → Task 8 (department rename → chart filter combobox). ✓

**Spec coverage (Part C — report panel + drafts):**
- Split-pane per the spec mockup: incident list (existing logic/filters/actions intact) narrows into the left rail; right panel = repositioned pill tab strip + editor content un-wrapped from `Dialog`, empty state ("Select or add a report…") when no report is open → Task 10. ✓ The reports list (Add Report + rows) also lives in the rail — the spec's mockup shows only incidents on the left, but the list must live somewhere reachable and the right panel is reserved for the workspace; flagged as the one layout judgment call the spec didn't dictate.
- `report-editor-dialog.tsx` → `report-editor-panel.tsx`, same data fetching/mutations/field rendering, minus the Dialog wrapper → Task 10 Steps 1-3/5. ✓
- Other chart sub-entity dialogs unaffected (Non-goal) → untouched; the panel's own nested dialogs (Procedures/Import ×2) still modal over it. ✓
- `report-draft-store.ts`: single `fsh.dashboard.reportDrafts.v1` key, `Record<reportId, DraftFields>` mirroring the panel's form state (header + vitals strings + `values` map), hydrate-on-mount (draft wins over server until Save/Sign; never for signed reports), debounce ~500 ms instrumented ONCE via a snapshot effect, cleared on successful Save/Sign → Tasks 9-10. ✓ Plus a flush-on-unmount so pill switches never lose sub-debounce keystrokes.
- Deliberately NOT wired into `patient-workspace-context.tsx` → separate store keyed by `reportId` only. ✓
- No server autosave / no cross-device sync / no admin-page-internals redesign (Non-goals) → nowhere in the plan. ✓
- Spec's tests: type → switch tab → back preserved; navigate away/reload preserved; save clears the draft → Task 11 (`report-drafts.spec.ts`); update specs assuming `role="dialog"` around the report → Task 11 Step 1; specs navigating `/administration/*` expecting a page → Task 4 Step 4. ✓

**Placeholder scan:** No TBD/TODO/ellipsis in any code block; every step is either complete final code or an exact replace-this-with-that pair (anchored against the current file contents as read, including the multi-line vs single-line `invalidate` arrow bodies per page). The intentional precedent-style exception: Task 10 Step 5's callout that the panel's unchanged middle (several hundred lines of fields/signature/review/addendums) is not reproduced because it is not modified; both edited regions around it are shown in full — and Task 10 Step 7 reproduces the Patient Info/Incident cards verbatim inside the new layout block so that replace is unambiguous.

**Type consistency:** `AdminDialogView` (Task 1) is consumed by the root (Task 2: `view.kind` narrowing) and the sidebar (Task 3: `view.kind !== "closed"`) and driven by the bridge (Task 4) — same union throughout. `AdministrationHubGrid({ onSelectSection: (slug: string) => void })` is called with `openSection` (root) — matching `(slug: string) => void`. `ReportDraft` (Task 9) is exactly the panel's snapshot shape (Task 10's `draftSnapshot` `useMemo<ReportDraft>` fails to compile if they drift). `ReportEditorPanel({ patientId, reportId })` defined in Task 10 and mounted with the same two props in `chart.tsx`. The draft-spec's `DRAFTS_KEY` string (Task 11) matches the store's `STORAGE_KEY`; `seedPatientWorkspace`'s seeded shape is the untouched `fsh.dashboard.patientWorkspace.v1` schema.

**Sequencing:** every task builds green on its own — Task 2 keeps the routed hub page alive until Task 4 rewires routes; the rename + chart re-layout share Task 10 (the import break makes them inseparable); Part B fixes (6-7) land before their E2E proof (8); the store (9) lands before its consumer (10) before its specs (11).

**Known risks flagged honestly (not hidden):**
- Playwright accessible-name selectors in Tasks 5/8/11 were written from component source, not a live DOM — the same risk the previous plan carried; each spec task's run step says to fix selectors against real output (new specs have no baseline to preserve).
- Task 4's deep-link bridge lands on Overview; unmocked Overview queries in older specs would hit the dev proxy — the updated `diagnostics.spec.ts` (and new specs) mock `billing/usage`, `billing/subscriptions/me`, and `audits` to stay deterministic.
- Draft-write suppression: if a hydration pass produced zero re-renders (impossible today — `setValues` always receives a fresh object), a stale `suppress` flag could swallow the first keystroke's write; the second keystroke recovers. Documented in Task 10's comments; accepted.
- A hard reload/close within the 500 ms debounce window can lose that last keystroke burst (browser unload runs no React cleanup) — accepted and documented; matches the spec's "debounced write" decision.
- The compact incident rail drops the table header + the explicit "No" transfer badge (badge renders only when `isTransfer`) — a deliberate density trade-off inside the spec's "just narrower, functionality intact" mandate; all filters and row actions are preserved.
- Open item NOT implemented (carried over from the previous plan): staleness handling for a report/incident deleted elsewhere while its pill/tab is open — the panel's fetch simply 404s/renders "Report not found", and a stale draft for a deleted report lingers in localStorage until manually cleared. Flagged as follow-up, not silently dropped.
- Task 13 requires the separate docs repo, which is not checked out here — explicit follow-up flag, same as the previous plan.
