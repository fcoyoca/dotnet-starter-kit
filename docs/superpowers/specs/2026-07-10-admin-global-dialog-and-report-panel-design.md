# Administration as a Global Dialog + Report Panel + Live Lookup Refresh — Design

**Date:** 2026-07-10
**Status:** Draft (design)
**Area:** dashboard (`clients/dashboard`) — follow-up to `2026-07-09-persistent-patient-tabs-design.md`
**Supersedes:** parts of Tasks 5 and 8-10 of `docs/superpowers/plans/2026-07-09-persistent-patient-tabs.md` (already implemented — this is a refinement, not a throwaway; see "What's reused" below)

## Problem

The first round of persistent-tabs work (Tasks 1-10, committed on `clinic-app`) got two things partially right and one thing not yet addressed:

1. **Administration still interrupts whatever you were doing.** Tasks 8-10 made each Administration section open as a dialog — but the *hub* (the grid of 17 sections) is still a routed page at `/administration`. Navigating there from an open patient chart unmounts the chart (React Router swaps the single `AppShell` `<Outlet/>`), so opening Administration mid-edit loses your place, exactly the friction the whole redesign was meant to remove.
2. **Admin edits don't propagate live.** Editing something in Administration (e.g. a Diagnostic Category) doesn't refresh the same data where it's displayed elsewhere (e.g. a category dropdown inside an open report) — the user has to reload.
3. **Reports are a modal dialog, not a persistent workspace.** BackChart-FE's actual UX keeps a report's fields on screen (accordion, never truly "closed" by navigation) so unsaved typing survives switching around. Our dialog-based report editor (Task 5) is much closer to BackChart than the old full-page route was, but it's still a mount/unmount boundary: switching between two open reports, or leaving the chart and coming back, can lose in-progress unsaved field edits.

## Decisions (user-confirmed)

1. **Administration becomes a globally-triggered dialog, not a route.** Clicking "Administration" in the sidebar opens a dialog *without navigating* — whatever page was behind it (patient chart, patient editor, anything) stays mounted, untouched, exactly as it was. `/administration` and `/administration/:section` deep links still work (open the right dialog on load) but are no longer the *only* way in, and no longer own page-level real estate.
2. **Admin mutations invalidate the shared lookup query keys**, not just their own list view, so anything else currently rendering that data (a dropdown in an open report, a picker in a dialog) refreshes automatically.
3. **Reports move from a modal dialog to a persistent split-pane panel** (chosen layout: incident list narrows to a left rail; a right-side panel hosts open report tabs + the active report's fields inline — see mockup below).
4. **Unsaved report edits persist locally (browser-only), not to the server.** Matches BackChart's actual behavior (it also never auto-saved to the server) — a per-report draft written to `localStorage`, read back on mount, cleared once the user explicitly saves/signs.

## Part A — Administration as a global dialog

### What's reused from Tasks 7-10 (not thrown away)
- `section-registry.ts` (`ADMIN_SECTION_COMPONENTS`, `ADMIN_HUB_SECTIONS`) — unchanged, still the single source of truth for which page backs which slug.
- `nav-data.ts`'s `administrationHubItems` / `ALL_ADMINISTRATION_PERMISSIONS` — unchanged.
- `admin-section-dialog.tsx`'s lazy-load-and-render-inside-`DialogContent` logic — kept, just no longer owns its own top-level route mount; becomes the "section" view of one shared dialog root.

### What changes
- New `clients/dashboard/src/state/administration-dialog-context.tsx`: a small context (no `localStorage` — this is ephemeral UI state, not something that should survive a browser restart) holding:
  ```ts
  type AdminDialogView = { kind: "closed" } | { kind: "hub" } | { kind: "section"; slug: string };
  ```
  actions `openHub()`, `openSection(slug)`, `backToHub()`, `close()`.
- New `clients/dashboard/src/pages/administration/administration-dialog-root.tsx`: mounted once in `AppShell` (sibling to `CommandPaletteRoot`/`MobileNavRoot`, inside `PatientWorkspaceProvider` so nesting order doesn't matter — this context is independent). Renders ONE `Dialog`:
  - `kind: "hub"` → the 17-card grid (`hub.tsx`'s grid, extracted from being a page into a pure content component — no more `useParams`/`useNavigate` inside it).
  - `kind: "section"` → the existing `AdminSectionDialog` content, plus a small "← Administration" back affordance that calls `backToHub()` instead of closing.
- `nav-data.ts`'s Administration entry stops being a real navigable `to` for click purposes; `sidebar.tsx` special-cases it (mirroring how it already special-cases the Patient Chart entry's `to` via `resolvePatientChartLink`): when rendering the item whose `to === "/administration"`, render a `<button onClick={openHub}>` instead of a `<Link>`.
- `routes.tsx`: `/administration` and `/administration/:section` become a tiny bridge component (`AdminDeepLinkOpener`) — on mount it calls `openSection(section)` or `openHub()` via the context, then `navigate("/", { replace: true })`. A fresh deep link lands on Overview with the right dialog already open on top — there's no "previous page" to preserve for a cold load, so Overview is the reasonable default underneath.

## Part B — Live lookup refresh

Audit every Administration entity's query keys: the admin CRUD page's own list query, and any "options" hook consumed elsewhere (dropdowns in `chart.tsx`, `report-editor-dialog.tsx` fields, `diagnostic-codes-dialog.tsx`, `select-appointment-dialog.tsx`, import-allergies/medications dialogs, etc.). Where an entity's list and options queries already share a stable top-level key prefix (e.g. `["administration", "clinics", ...]`), fix is mechanical: change each admin mutation's `onSuccess` from `invalidateQueries({queryKey: ["administration","clinics","list", params]})` (exact-match) to `invalidateQueries({queryKey: ["administration","clinics"]})` (prefix match — React Query invalidates every query whose key starts with this array), so both the list view AND any options-hook query sharing that prefix refetch together. Where an entity's options hook uses a genuinely different, unrelated key, add a second explicit `invalidateQueries` call for that key rather than forcing an artificial rename (don't break unrelated call sites just to unify naming).

This needs a real per-entity audit (17 sections) rather than a blind find-replace — some entities may already do this correctly, some may not have an "options" consumer elsewhere at all (nothing to fix), some may have a naming mismatch worth correcting at the source instead of papering over with a second invalidate call.

## Part C — Report panel (replaces the Task 5 modal)

### Layout
```
┌────────────────────┬──────────────────────────────────┐
│ Incidents           │ [Daily Visit ×][Re-Exam ×] +      │
│ ──────────────────  │──────────────────────────────────│
│ ▸ 06/20 Daily Visit │                                    │
│ ▸ 06/15 Re-Exam     │  Chief Complaint                   │
│ ▸ 06/10 Initial     │  [textarea.....]                   │
│                     │                                    │
│                     │  Assessment                        │
│                     │  [textarea.....]                   │
│                     │                                    │
│                     │  [Save Draft] [Sign Report]        │
└────────────────────┴──────────────────────────────────┘
```
- Incident list narrows to a left rail (existing incident-list logic/filters stay, just re-laid-out into less horizontal width).
- Right panel is the report workspace: a tab strip across the top for `openReportIds` (reusing/repositioning the pill row already built in Task 5) + the active report's full editor content below it, un-wrapped from `Dialog`/`DialogContent`.
- `report-editor-dialog.tsx` → `report-editor-panel.tsx`: same data-fetching/mutations, same field rendering, minus the `Dialog` wrapper — becomes a plain panel component rendered inline in `chart.tsx`'s right column.
- Other chart sub-entity dialogs (problems, allergies, medications, notes, documents, export, procedures) are **unaffected** — they stay as on-demand dialogs exactly as today; only the report editor moves into the persistent panel, since it's the one entity BackChart keeps genuinely "always there" while you work.
- When no report is open for the active incident, the right panel shows an empty state ("Select or add a report").

### Draft persistence
- New `clients/dashboard/src/state/report-draft-store.ts` (or a hook `useReportDraft(reportId)`): a single `localStorage` key `fsh.dashboard.reportDrafts.v1` holding `Record<reportId, DraftFields>` (field-id → value map, mirroring whatever shape `report-editor-panel.tsx`'s existing local form state already uses for fields/vitals/macros).
- On mount, `report-editor-panel.tsx` checks for a draft for its `reportId`; if present, initializes form state from the draft (falling back to the server-fetched `report` otherwise).
- On every field-state change (the existing single form-state setter, not per-input — instrument once, not N times), debounce-write (e.g. 500ms) the current form values into the draft store under this `reportId`.
- On a successful Save/Sign mutation, clear this report's entry from the draft store (the server now holds the authoritative value; keeping a stale draft around would shadow future edits incorrectly).
- This is deliberately **not** wired into the patient-workspace context from Part A of the prior spec — drafts are keyed purely by `reportId` and don't need to know which patient tab they belong to, so a separate small store keeps `patient-workspace-context.tsx` from growing an unrelated responsibility.

## Non-goals
- No server-side autosave/draft endpoint (user confirmed: local-only, matching BackChart).
- No change to the other chart sub-entity dialogs' presentation (problems/allergies/medications/notes/documents/etc. stay as-is).
- No redesign of individual admin CRUD pages' internals — Part A is purely about *how they're launched* (dialog trigger, not route), not what's inside them.
- No cross-device/cross-browser draft sync (localStorage is per-browser, same limitation as the patient-workspace tabs).

## Affected files (preliminary)

Frontend (`clients/dashboard/src/`):
- new: `state/administration-dialog-context.tsx`
- new: `pages/administration/administration-dialog-root.tsx`
- modify: `pages/administration/hub.tsx` (page → pure grid content component, or split into `hub.tsx` content + thin route residue for `AdminDeepLinkOpener`)
- modify: `pages/administration/admin-section-dialog.tsx` (adjust to compose under the shared root; add "back to hub" affordance)
- modify: `components/layout/app-shell.tsx` (mount the new provider + root)
- modify: `components/layout/sidebar.tsx` (Administration entry renders as a button, not a Link)
- modify: `routes.tsx` (`/administration`, `/administration/:section` → `AdminDeepLinkOpener` bridge instead of rendering a page)
- new: `state/report-draft-store.ts`
- rename+modify: `pages/patient-charts/report-editor-dialog.tsx` → `report-editor-panel.tsx` (drop Dialog wrapper, add draft read/write)
- modify: `pages/patient-charts/chart.tsx` (two-column layout; mount the panel inline instead of a conditional dialog)
- modify: up to 17 admin pages' mutation `onSuccess` handlers (Part B audit) — exact list determined during task planning, not all 17 necessarily need a change
- modify: `pages/patient-charts/report-search-dialog.tsx` (still calls `openReport` — confirm no changes needed beyond what Task 5 already did, since it just sets workspace state, doesn't care whether the consumer renders a dialog or a panel)

Tests:
- Update `tests/administration/hub.spec.ts` (if Task 11 already created it before this refinement landed) and any spec that navigates directly to `/administration/*` expecting a full page, to instead assert the dialog opens without unmounting whatever was on screen.
- New coverage: opening Administration while a patient chart is open, editing a record, closing the dialog, confirming the chart is untouched/still on the same incident/report.
- New coverage: editing a lookup value (e.g. a diagnostic category name) in Administration and confirming an open report's dropdown reflects the new value without a manual reload.
- Update/replace report-dialog specs (Task 6, Task 11) for the new panel layout (no more `role="dialog"` around the report; it's inline panel content now).
- New coverage: type into a report field, switch to another open report tab, switch back — text preserved. Navigate away from the chart entirely and back — text still preserved (via the localStorage draft). Save the report — draft cleared, subsequent reload shows the saved (not draft) value.
