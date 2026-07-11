# Persistent Patient Tabs + Administration Dialogs — Design

**Date:** 2026-07-09
**Status:** Draft (design)
**Area:** dashboard (`clients/dashboard`) — cross-cutting: `AppShell`, Patient Chart, Administration

## Problem

BackChart-FE (legacy Blazor Server app) keeps a patient's chart, active incident, and any
open reports **persistent for the whole session**: opening a patient adds an accordion card
that survives navigating elsewhere in the app and coming back; the only way to lose it is an
explicit close (`×`). It does this via scoped DI state plus a `localStorage`-backed ID tree
(`BackChartPatientBasicInfo`/`BackChartPatientBasicInfo2`) that rehydrates on every page load
(`PatientChartIndex.razor.OnInitializedAsync`).

`clinic-solution-app`'s Patient Chart has no equivalent. `/patient-charts/:patientId`
(`chart.tsx`) owns its active incident and every sub-entity's open/closed flag as local
`useState`, so navigating to another section of the app and back tears the whole thing down —
the user has to re-search and re-open the patient, re-pick the incident, and re-open whatever
report they were on. The report editor makes this worse: unlike every other chart sub-entity
(problems, allergies, medications, notes, documents — all dialogs launched from `chart.tsx`),
it's a **full-page route** (`/patient-charts/:patientId/reports/:reportId`), so opening a
report is itself a navigation that can strand the user away from the rest of the chart.

Separately, Administration/clinic-setup (`/administration/*`, 17 sections) is 17 independent
full-page routes. Opening "Diagnostic Categories" to add a category while charting a patient
means fully navigating away from `/patient-charts/:patientId` — today that already loses chart
state since there's no persistence; even after Part A below fixes persistence, a full-page nav
away is still an unnecessary detour for what's a quick CRUD action.

## Decisions

1. **Tab UX:** horizontal tab strip in `AppShell`, one tab per open patient (browser-tab /
   editor-tab style) — not a BackChart-style accordion, not a side dock. Only rendered when at
   least one patient tab is open.
2. **Persistence:** `localStorage`, versioned key, rehydrated on `AppShell` mount — same
   mechanism BackChart-FE uses, adapted to a single React context instead of two ID-tree keys.
3. **Report editor:** converts from a routed page to a dialog, matching every other chart
   sub-entity. Only one report renders at a time per patient tab; multiple can be tracked as
   "open" and switched between via a small pill list (no nested tabs-within-tabs).
4. **"Patient Chart" menu entry:** when a patient tab is already active, clicking the sidebar
   entry jumps straight to that patient's chart instead of the search page — this is the
   concrete "avoid more clicks" behavior the redesign is for.
5. **Administration:** single **Administration hub** page listing all 17 sections; clicking a
   section opens its existing CRUD UI in a dialog layered over whatever page is behind it (so
   an open patient tab strip stays mounted underneath). Existing `/administration/:section`
   deep links are kept and route to the same dialog content, not a bare page.

## Design — Part A: Persistent Patient Tabs

New context, mounted in `AppShell` (above `<Outlet/>`, so it isn't torn down by route changes):

```ts
// src/state/patient-workspace-context.tsx
type OpenPatientTab = {
  patientId: string;
  patientLabel: string;        // cached name/code, avoids a refetch just to render the tab
  activeIncidentId: string | null;
  openReportIds: string[];     // reports considered "open" for this patient
  activeReportId: string | null;
};
type WorkspaceState = {
  openTabs: OpenPatientTab[];
  activePatientId: string | null;
};
```

- Persisted to `localStorage` under a single versioned key (e.g. `bc.patientWorkspace.v1`),
  written on every mutation, read once on `AppShell` mount.
- Actions exposed via context: `openPatient(id, label)`, `closePatient(id)`,
  `setActivePatient(id)`, `setActiveIncident(patientId, incidentId)`,
  `openReport(patientId, reportId)`, `closeReport(patientId, reportId)`,
  `setActiveReport(patientId, reportId)`.
- `chart.tsx` stops owning `activeIncidentId` as local `useState`; it reads/writes through the
  context instead, keyed off the `patientId` route param. The heavy incident/report *data*
  stays in React Query exactly as it is today — only the "which one is active" pointer moves
  into the persisted context.
- New `PatientTabStrip` component renders under `Topbar` in `AppShell`, only when
  `openTabs.length > 0`. Clicking a tab navigates to `/patient-charts/:patientId` and calls
  `setActivePatient`; the `×` calls `closePatient` (also purges it from `localStorage`) and,
  if it was the active tab, falls back to the next open tab or `/patient-charts` (search page).
- Sidebar "Patient Chart" link: when `activePatientId` is set, resolves to
  `/patient-charts/:activePatientId`; otherwise falls back to `/patient-charts` (search page).
  This is the change that satisfies "when user navigated to patient menu the active selected
  patient with reports should [be] open[ed] to avoid more clicks."

## Design — Part B: Report editor → dialog

- `report-editor.tsx` becomes `report-editor-dialog.tsx`, opened from `chart.tsx` keyed by
  `activeReportId` from the workspace context (same trigger points that currently call
  `navigate(`/patient-charts/${patientId}/reports/${reportId}`)`, e.g. `chart.tsx:267,878,885`).
- The `/patient-charts/:patientId/reports/:reportId` route is dropped from `routes.tsx`. If
  anything currently deep-links to it (check notifications/email links before removing), add a
  redirect: `/patient-charts/:patientId?report=:reportId` → chart page opens the dialog on
  mount from the query param.
- `openReportIds` lets a user have more than one report "open" per patient without losing
  place; a small pill list in the chart view switches `activeReportId` between them. Closing
  the dialog does **not** remove it from `openReportIds` — only an explicit close action does
  (mirrors BackChart-FE: navigating away ≠ closing).

## Design — Part C: Administration hub + dialog

- New route `/administration` (currently a redirect to `/administration/clinics`) becomes an
  `AdministrationHub` page: a grid of the 17 existing sections, sourced from the same
  `sections` data already in `nav-data.ts` (single source of truth — no duplicated labels/
  icons/permission strings).
- New `AdminSectionDialog` — a generic dialog shell that lazy-loads and renders the *existing*
  page component (`ClinicsPage`, `DepartmentsPage`, etc.) inside `DialogContent`. Every admin
  page is already a self-contained list+CRUD unit on shared `EntityListCard`/`Dialog`
  primitives (see `clinics.tsx:60-64` `EditorState` pattern), so this should be a thin wrapper,
  not a rewrite of each page.
- Existing `/administration/:section` routes stay (bookmarks/deep links keep working) but
  render the same dialog content over whatever's mounted behind them instead of replacing the
  whole page — so a patient tab strip open underneath survives navigating to an admin section.
- Sidebar "Clinic Setup" section collapses to a single "Administration" entry pointing at the
  hub, mirroring the same simplification already done for Patients
  (`2026-06-30-patient-chart-search-design.md`). Per-section permission checks move from
  gating nav items to gating which hub cards render.

## Non-goals

- No change to admin CRUD business logic — purely a presentation wrapper around existing pages.
- No nested tabs-within-tabs for reports (single active report per patient tab + pill switcher).
- No server-persisted "last open" state — `localStorage` only, per-device (same limitation
  BackChart-FE has with its own `localStorage` approach).
- No hard technical cap on open patient tabs in this design pass — a soft cap + "close all"
  affordance is a follow-up if it proves necessary in practice.

## Affected files (preliminary — will firm up during task planning)

Frontend (`clients/dashboard/src/`):
- new: `state/patient-workspace-context.tsx`
- new: `components/layout/patient-tab-strip.tsx`
- new: `pages/administration/hub.tsx`
- new: `pages/administration/admin-section-dialog.tsx`
- new: `pages/patient-charts/report-editor-dialog.tsx` (replaces `report-editor.tsx` as a route)
- edit: `components/layout/app-shell.tsx` (mount workspace provider + tab strip)
- edit: `components/layout/sidebar.tsx` (Patient Chart link resolves to active tab)
- edit: `components/layout/nav-data.ts` (collapse Clinic Setup → single Administration entry)
- edit: `pages/patient-charts/chart.tsx` (read/write active incident + reports via context;
  open report dialog instead of `navigate()`)
- edit: `routes.tsx` (drop/redirect the report-editor route; wrap admin section routes to
  render dialog content over the current page)

Tests:
- Dashboard: Playwright coverage for tab persistence (open patient → navigate away → navigate
  back → chart still shows same patient/incident/report), explicit close removing a tab, and
  the Administration hub → dialog → CRUD flow with an open patient tab still visible/intact
  underneath.

## Open items to resolve during task planning

- Whether anything currently deep-links to `/patient-charts/:patientId/reports/:reportId`
  (notifications, emails, audit trail links) — determines whether the redirect/query-param
  fallback in Part B is required or can be skipped.
- Exact visual design of the tab strip and admin hub cards (component-level, not architectural
  — a quick UI pass before task 1).
- Whether `openReportIds`/tab state needs any staleness handling (e.g. a report deleted by
  another user while its tab is open).
