# Patient Chart — incident-switch confirmation gate

**Date:** 2026-07-13
**Area:** `clients/dashboard` — Patient Chart
**Status:** Approved design, ready for planning

## Problem

A patient chart works in one incident at a time (`activeIncidentId`), but reports stay open across
incident switches. Today a switch is instantaneous and silent: reports from the previous incident
remain open, quietly flip to read-only, and are marked only by a small red triangle on their pill.
The user is never told the switch will lock what they had open, and never gets a chance to back out.

## Goal

Switching to a different incident while a chart already has another incident loaded must ask for
confirmation first. Proceeding leaves the stranded reports open but read-only, with the locked state
made obvious. Cancelling is a complete no-op — no incident change, nothing opened, nothing closed.

## What already exists (do not rebuild)

- `report-editor-panel.tsx` already derives `isForeignIncident` (report's incident ≠ chart's active
  incident) and folds it into `readOnly = isSigned || !canUpdate || isForeignIncident`, disables all
  inputs, hides Save/Sign, suppresses draft writes, and guards `onSave`/`onSign` directly.
- The panel already shows a per-report "Read-only — a different incident is selected" banner with a
  "Switch to this incident" button.
- The chart's open-report pills already flag foreign reports with a red `AlertTriangle` and an
  `IncidentRef` (DOIV/DOL) label.

The read-only *enforcement* is therefore already correct. This work adds the **confirmation gate**
before the switch and a **chart-level lock banner** so the locked state is visible without hovering
a triangle.

## Design

### 1. `IncidentSwitchGuard` — new file `clients/dashboard/src/pages/patient-charts/incident-switch-guard.tsx`

A context provider mounted inside `PatientChartDetailPage`, wrapping the chart body. It owns the
pending-switch state and renders the confirmation dialog. Keeping it here (rather than in
`patient-workspace-context`) leaves that context pure serializable state and avoids re-rendering
every workspace consumer (sidebar, tab strip) on pending-switch changes.

**Props**

| Prop | Type | Why |
|---|---|---|
| `patientId` | `string` | The chart's patient; passed through to `setActiveIncident`. |
| `openReports` | `{ id: string; incidentId: string \| null }[]` | The chart already derives this in `openReportTabs`; passing it avoids a second fetch. Used to count what a switch would strand. |
| `incidents` | `PatientIncidentListItemDto[]` | Resolves the target incident's DOIV/DOL for the dialog copy. |

**Exposed API** (via `useIncidentSwitch()`)

```ts
requestIncidentSwitch(
  incidentId: string,
  opts?: { skipConfirm?: boolean; onProceed?: () => void },
): void
```

Behavior:

1. `incidentId === activeIncidentId` → run `onProceed`, no prompt (not a switch).
2. `activeIncidentId == null` **or** `opts.skipConfirm` → `setActiveIncident` immediately, then run
   `onProceed`. No prompt.
3. Otherwise → stash `{ incidentId, onProceed }` and open the dialog.
   - **Confirm** → `setActiveIncident(patientId, incidentId)`, then `onProceed?.()`, then clear the
     pending state.
   - **Cancel** → clear the pending state. No incident change, `onProceed` never runs.

The auto-select effect in `chart.tsx` (select the first incident when none is active; clear when the
active one no longer exists) keeps calling `setActiveIncident` **directly**. It is system-driven
repair, not a user switch, and must never prompt.

### 2. Entry points — all three route through the guard

| Call site | Wiring |
|---|---|
| `IncidentsListDialog` → `onSelect` (chart.tsx) | `requestIncidentSwitch(id, { skipConfirm: chooserOpenedAutomatically })` |
| `IncidentsListDialog` → `onOpenReport` (chart.tsx) | `requestIncidentSwitch(id, { onProceed: () => openReport(patientId, reportId) })` — cancelling also leaves the report unopened |
| `ReportEditorPanel` → "Switch to this incident" (foreign-incident banner) | Consumes `useIncidentSwitch()` directly (it is a descendant of the provider) instead of calling `setActiveIncident` |

**The "first pick is free" rule.** On chart load the auto-select effect picks `incidents[0]`, and if
the patient has more than one open incident the chooser pops up so the user picks properly. That
first pick must not prompt — the auto-selection was not a user choice. The chart already tracks the
auto-open in `incidentsPromptedForRef`; promote it to state (`chooserOpenedAutomatically`) so
`onSelect` can pass `skipConfirm: true` for exactly that interaction. The flag is cleared when the
dialog closes, so any later user-initiated open of the chooser prompts normally.

### 3. Chart-level lock banner

Rendered in the right-hand report workspace, pinned directly above the open-report pill strip,
whenever at least one open report is foreign:

> ⚠ **N report(s) belong to another incident and are read-only.** Close them, or switch back to their
> incident to edit.

`data-testid="foreign-reports-lock-banner"`. Uses the same destructive-tone styling as the existing
per-report banner. The per-report banner inside `ReportEditorPanel` stays unchanged.

### 4. Confirmation dialog

Built ad-hoc from the Radix `Dialog` primitives (the project has no shared `ConfirmDialog`; see
`RestoreConfirmDialog` in `pages/system/trash.tsx` for the established shape). Lives inside
`incident-switch-guard.tsx`. `data-testid="incident-switch-confirm"`.

- **Title:** "Switch to a different incident?"
- **Body:** names the target incident via `IncidentRef` (DOIV/DOL), then the consequence:
  - With open reports: "N open report(s) from the current incident will become read-only. They stay
    open — you can switch back at any time."
  - With none open: "No reports are open."
- **Buttons:** Cancel · Switch incident.

## Testing

New route-mocked Playwright spec `clients/dashboard/tests/patient-charts/incident-switch-confirm.spec.ts`:

1. Selecting a different incident from the Incidents dialog with a report open shows the confirm
   dialog; **Cancel** leaves the active incident unchanged, the report still editable, no lock banner.
2. **Switch incident** applies the switch: the chart's `IncidentRef` shows the new incident, the
   stale report's pill is flagged foreign, its editor is read-only, and the lock banner appears.
3. The chooser auto-opened on chart load does **not** prompt on its first pick.
4. The panel's "Switch to this incident" button **does** prompt.

## Non-goals

- No backend change. This is entirely a `clients/dashboard` concern.
- Auto-closing stranded reports on switch — they stay open, read-only, by design.
- Changing how read-only is enforced inside `ReportEditorPanel`.
