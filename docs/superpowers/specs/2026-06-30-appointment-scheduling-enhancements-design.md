# Appointment Scheduling Enhancements — Design

**Date:** 2026-06-30
**Branch:** clinic-app
**Status:** Approved — implementing

## Goal

Bring the dashboard Appointments surface ([clients/dashboard/src/pages/scheduling/appointments.tsx](../../../clients/dashboard/src/pages/scheduling/appointments.tsx))
to parity with BackChart-FE's scheduler for five capabilities that are currently missing or limited:

1. **Recurring reserved time** — reserve a slot on a repeating schedule (weekdays + end date), not just one day.
2. **Reschedule** — move an appointment by creating a replacement and linking the original (BackChart parity).
3. **Confirm** — record that the patient confirmed the appointment.
4. **Patient chart shortcut** — jump from an appointment straight to the patient's chart.
5. **Hover tooltip** — calendar events show a rich tooltip matching BackChart-FE's format.

No changes to `src/BuildingBlocks`.

## Design decisions (locked)

- **Recurrence model:** weekdays + end date, **materialized** — one `Appointment` row per occurrence, grouped by a shared `ReservationSeriesId`. Bounded span (≤ 1 year) to cap row count. Per-provider.
- **Reschedule:** **new + link** — create a new appointment at the new time copying patient/type/notes/clinic; mark the original with `RescheduledToAppointmentId`. The original stays for history, rendered muted/struck and read-only.
- **Confirm:** a nullable `ConfirmedAtUtc` timestamp (no reminder integration — the dashboard has no reminders module yet).

## Backend — Scheduling module

### Domain ([Appointment.cs](../../../src/Modules/Scheduling/Modules.Scheduling/Domain/Appointment.cs))

Add three fields (all nullable, additive):

| Field | Type | Meaning |
|---|---|---|
| `ConfirmedAtUtc` | `DateTime?` | When the patient confirmed. Null = unconfirmed. |
| `RescheduledToAppointmentId` | `Guid?` | Replacement appointment id. Non-null = this slot was rescheduled away (read-only). |
| `ReservationSeriesId` | `Guid?` | Groups materialized recurring-reserve blocks for series operations. |

New methods: `Confirm()` (sets `ConfirmedAtUtc = DateTime.UtcNow`), `MarkRescheduled(Guid newId)`.
`Create` gains optional `reservationSeriesId`. EF config maps the columns + index on `ReservationSeriesId`.
DTO gains the three fields.

### Migration

One migration `AppointmentSchedulingEnhancements` in `src/Host/FSH.Starter.Migrations.PostgreSQL` (Scheduling folder),
adding the three nullable columns and the series index.

### Endpoints (all under `api/v1/scheduling`, follow existing lifecycle pattern)

| Verb | Route | Command | Permission |
|---|---|---|---|
| POST | `/appointments/{id}/confirm` | `ConfirmAppointmentCommand(id)` | Appointments.Update |
| POST | `/appointments/{id}/reschedule` | `RescheduleAppointmentCommand(id, providerId, startUtc, endUtc)` → new Guid | Appointments.Update |
| POST | `/appointments/reserve-recurring` | `CreateRecurringReservationCommand(...)` → count | Appointments.Create |
| DELETE | `/appointments/series/{seriesId}` | `DeleteReservationSeriesCommand(seriesId)` | Appointments.Delete |

- **Confirm** — load, `Confirm()`, save, notify `"confirmed"`.
- **Reschedule** — load original; create new `Appointment` (copy clinic/provider override/patient/type/notes) at new time; `original.MarkRescheduled(new.Id)`; save both; notify `"rescheduled"`; return new id. Reject if already rescheduled/cancelled.
- **Recurring reserve** — input `{ clinicId, providerId, title, notes, occurrences[] }` where each occurrence is a `{ startUtc, endUtc }` pair. The **client** expands weekdays + end date into occurrences (it already owns all clinic-local→UTC conversion via `zonedWallToUtc`, keeping timezone logic in one place); the handler assigns a shared `ReservationSeriesId`, bulk-adds, notifies `"created"`, returns count. Validator: ≥1 occurrence, ≤ 366 occurrences, each end > start, title required.
- **Delete series** — soft-delete every appointment with that `ReservationSeriesId`.

Each command gets a `{Command}Validator` (Architecture.Tests requirement). Register the four endpoints in `SchedulingModule.MapEndpoints()`.

## Frontend — dashboard

### API client ([scheduling.ts](../../../clients/dashboard/src/api/scheduling.ts))

Add `confirmedAtUtc`, `rescheduledToAppointmentId`, `reservationSeriesId` to `AppointmentDto`; add
`confirmAppointment`, `rescheduleAppointment`, `createRecurringReservation`, `deleteReservationSeries`; extend
`AppointmentChangedEvent` action union with `"confirmed" | "rescheduled"`.

### Appointment dialog ([appointments.tsx](../../../clients/dashboard/src/pages/scheduling/appointments.tsx))

- **Reserve time** section gains a **Repeat** toggle → reveals weekday checkboxes (Su–Sa) + **End date**. Submitting a
  repeating reservation calls `createRecurringReservation`; non-repeating keeps the current single-create path.
- **Confirm** button added to the lifecycle row (non-reservation). Hidden once confirmed; a "Confirmed <date>" line shows instead.
- **Reschedule** button flips the edit dialog into reschedule mode (pick provider/date/time; header "Reschedule appointment");
  Save calls `rescheduleAppointment`. Hidden for reservations and already-rescheduled/cancelled appointments.
- **Open patient chart** button (when `patientId` present) → navigates to `/patient-charts/:patientId`.
- Deleting a reserve block with a `reservationSeriesId` prompts **Delete this occurrence** vs **Delete entire series**.

### Calendar event tooltip

Custom event component renders a hover tooltip mirroring BackChart-FE's
[AppointmentTooltip.razor](../../../BackChart-FE/BackChart/Shared/Template/Schedule/AppointmentTooltip.razor):

- Reservation: title + `Notes: <notes|None>`.
- Appointment: status banners in order — `!! CONFIRMED !!` (+ `Confirmed: <date>`), `!! RESCHEDULED !!`,
  `!! NO SHOW !!`, `!! CANCELLED !!`, `!! CHECKED IN !!` / `!! CHECKED OUT !!` — then subject, `start – end` time,
  `Type: <name>` (when typed), `Notes: <notes|None>`.

Rescheduled/cancelled/no-show events also get muted styling on the calendar (existing `eventColor` already covers
cancelled/no-show; add a rescheduled color + strike).

## Out of scope

Appointment reminders / SMS confirmation, editing a single occurrence's recurrence rule, drag-to-reschedule on the calendar.

## Testing

Backend: Architecture.Tests must stay green (handler/validator pairing). Manual: build backend + dashboard.
Playwright route-mocked coverage for the new dialog flows is a follow-up (existing `appointments.spec.ts` stays green).
