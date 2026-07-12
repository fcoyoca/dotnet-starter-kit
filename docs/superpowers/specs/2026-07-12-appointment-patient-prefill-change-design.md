# Appointment patient pre-fill + "Change" search — Design

**Date:** 2026-07-12
**Area:** `clients/dashboard` — scheduling
**Status:** Approved (design)

## Problem

When a user schedules an appointment while a patient is "current" (open in a
patient chart / the active patient tab), the appointment's patient field should
already be filled in with that patient. Today the field starts blank unless the
user arrives via the chart's "Schedule appointment" button, and even then the
patient is shown inside an always-visible inline typeahead rather than a settled
"this is the patient" state.

BackChart's behavior (the reference, `AppointmentInformation.razor`): the patient
is shown as a **read-only name box**, with a separate **"Search for Patient"**
button that opens a **patient-search popup dialog**. Searching never happens
inline; it always happens in the popup.

## Goals

1. The appointment patient field auto-pre-fills with the current patient when
   creating an appointment.
2. A selected patient renders as a **name label + "Change" button** (the resting
   state), never an inline search box.
3. Clicking **Change** opens a **nested popup patient-search dialog** (BackChart
   parity). Picking a patient collapses back to the name label.

## Non-goals (YAGNI)

- No changes to reserve-time UI (reservations have no patient field).
- No changes to the existing edit-mode "Open patient chart" button.
- No new API endpoints — reuses `searchPatients`.
- No change to the appointment create/update/reschedule payloads.

## Decisions (from brainstorming)

| Decision | Choice |
|---|---|
| Pre-fill source | Chart "Schedule" button (existing) **and** the active patient tab (`useActivePatientTab`) when opening New appointment from the calendar. |
| "Change" search UX | **Nested popup dialog** layered over the appointment dialog. |
| Collapsed "name + Change" scope | Applies **whenever a patient is set** — pre-filled, edited, or just-picked. |

## Architecture

Three focused changes; the `PatientPicker` public interface is unchanged, so the
blast radius in `appointments.tsx` is small.

### 1. New component — `PatientSearchDialog`

`clients/dashboard/src/components/scheduling/patient-search-dialog.tsx`

A self-contained modal that owns the debounced typeahead search + results list
(extracted from today's `PatientPicker` body). It reuses `searchPatients`.

```
Props:
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelect: (patient: PatientListItemDto) => void
```

Behavior:
- Renders inside its own Radix `Dialog` portal, so it stacks above the
  appointment dialog.
- Debounced (250ms) search over `/patient/patients`; results in a listbox.
- Picking a row calls `onSelect(patient)` and closes (`onOpenChange(false)`).
- Search query resets when the dialog closes so each open starts clean.

### 2. `PatientPicker` becomes a collapsed control

`clients/dashboard/src/components/scheduling/patient-picker.tsx`

Same public props: `{ value, initialLabel, onChange, disabled }`. Internals
change — the inline search input/results are removed (moved into the dialog).
Now renders one of two resting states plus the dialog:

- **Patient set (`value` truthy):** read-only chip (name from `label ?? value`,
  DOB when known) · **Change** button · **Clear** button.
  - `Change` → open `PatientSearchDialog`.
  - `Clear` → `onChange(null, null)`.
- **Empty:** a **"Search for patient"** button → open `PatientSearchDialog`.
- `PatientSearchDialog.onSelect(p)` → set local label/selected → `onChange(p.id, p)`.
- When `disabled`, no Change/Clear/Search buttons and the dialog can't open.

`patientLabel(p)` helper stays exported (used elsewhere).

### 3. Pre-fill from the active patient tab — `appointments.tsx`

- Import `useActivePatientTab` from the workspace context.
- In the toolbar **New appointment** `onClick` and in **`onSelectSlot`**, seed the
  create-dialog state with `patientId: activeTab?.patientId ?? null` and
  `patientLabel: activeTab?.patientLabel ?? null`.
- The existing chart router-state pre-fill (`newApptPatientId` /
  `newApptPatientLabel`) is unchanged and still wins when present.
- Pass `disabled={readOnly}` to `PatientPicker` so a rescheduled (read-only)
  appointment can't have its patient swapped — a small correctness fix uncovered
  by consolidating the control.

## Data flow

```
New appointment (calendar)
  → read activePatientTab (or router state from chart)
  → create-dialog state { patientId, patientLabel }
  → PatientPicker shows collapsed chip
  → [Change] → PatientSearchDialog opens (nested portal)
  → onSelect(patient) → onChange(id, patient) → chip updates, dialog closes
```

Reserve-time toggles the whole patient field away (unchanged). Reschedule /
read-only keeps the patient read-only.

## Error / edge handling

- Empty active tab → blank patient field (button-only empty state).
- `activePatientTab.patientLabel` is the full name (no patient code); it is only a
  display placeholder until a record loads — acceptable, matches existing
  `initialLabel` usage.
- Nested dialog focus: Radix traps focus in the search dialog while open and
  returns focus to the appointment dialog on close. Validate during
  implementation that the appointment dialog does not close when the inner dialog
  closes (independent `Dialog.Root`s).

## Testing (Playwright, route-mocked — `tests/scheduling/appointments.spec.ts`)

1. **Pre-fill from active tab:** seed `sessionStorage`
   (`fsh.dashboard.patientWorkspace.v1`) with one open tab + `activePatientId` →
   New appointment → the create dialog shows the collapsed chip with that
   patient's name and a **Change** button, and **no** inline search input.
2. **Change flow:** click **Change** → search dialog appears → type a query
   (mock `/patient/patients` to return a patient) → pick it → the chip updates to
   the picked patient's name and the search dialog closes.
3. **Empty state:** no active tab → New appointment → a **"Search for patient"**
   button is shown (not a search input).

## Files touched

| File | Change |
|---|---|
| `components/scheduling/patient-search-dialog.tsx` | **New** — popup search dialog. |
| `components/scheduling/patient-picker.tsx` | Collapse to name+Change / Search button; open dialog for search. |
| `pages/scheduling/appointments.tsx` | Pre-fill from active tab; pass `disabled={readOnly}`. |
| `tests/scheduling/appointments.spec.ts` | 3 new tests. |
