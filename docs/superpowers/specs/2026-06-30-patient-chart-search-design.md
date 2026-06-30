# Patient Chart Search — Design

**Date:** 2026-06-30
**Status:** Approved (design)
**Area:** dashboard (`clients/dashboard`) + Patient module (`src/Modules/Patient`)

## Problem

The dashboard's **Patient Chart** page (`/patient-charts`) dumps every active patient
as a paged list whose only purpose is to pick one and open their clinical chart. This
duplicates the **Patients** demographics list and scales poorly. BackChart solved the
same problem with a dedicated **Patient Search** dialog (filters + results, nothing shown
until you search).

We want to modernize that pattern into this app: turn the Patient Chart entry point into a
**search-first** experience with filters, instead of an up-front list of all patients.

## Decisions (from brainstorming)

1. **Menu:** **One** menu item — **Patient Chart**. The standalone `Patients` menu item and
   its `/patients` list page are removed. The chart becomes the single patient hub.
2. **Demographics access:** The demographics detail page (`/patients/:patientId`) stays, but
   is reached from the chart via an **Edit patient info** affordance on the Patient Info card
   — not from a top-level menu. Demographics editing is *not* folded into the chart (no tab).
3. **Filters:** Free-text (name/code) + Status (All/Active/Inactive) + **Provider** + **Clinic**.
4. **Provider/Clinic semantics:** "patients who have a clinical **report** authored for that
   provider/clinic." This stays entirely inside the Patient module (reports carry
   `ProviderId`/`ClinicId`/`PatientId`), so no cross-module coupling and no boundary-rule break.
5. **Form factor:** a search-first **page** (it's a menu→page navigation), not a modal.

## UX flow

```
Patient Chart  (/patient-charts)
┌─────────────────────────────────────────────────────────┐
│ [ Search name or code… ]   Provider:[All▾] Clinic:[All▾] │
│ Status: (All) Active Inactive            [Clear] [+ New]  │
├─────────────────────────────────────────────────────────┤
│  before search → "Search for a patient to open their     │
│                   chart, or register a new one."          │
│  after search  → results table                           │
│   Last Name │ First Name │ Code │ DOB │ Last Visit │  →   │
│   row click → /patient-charts/:patientId (existing chart) │
└─────────────────────────────────────────────────────────┘
```

- Free-text search is debounced (250ms), matching the existing list/picker pattern.
- Provider / Clinic / Status apply immediately.
- Results are **only** fetched/shown when at least one of `search`, `providerId`, `clinicId`,
  or a non-default status filter is active. An empty/default state shows the prompt — this is
  what removes the "list all patients" behavior.
- Row click navigates to the existing `/patient-charts/:patientId` chart.
- **+ New** opens the "Register a patient" dialog. On success, navigate straight to the new
  patient's chart (`/patient-charts/:newId`).
- The chart's **Patient Info** card gains an **Edit patient info** link → `/patients/:patientId`
  (the existing demographics detail page). That detail page's "Back" target and after-delete
  redirect are repointed at the chart hub (see frontend changes).

## Backend changes (Patient module)

Extend the existing SearchPatients slice — no new endpoint, no migration.

- `Modules.Patient.Contracts/v1/Patients/SearchPatientsQuery.cs`: add
  `Guid? ProviderId = null` and `Guid? ClinicId = null`.
- `Features/v1/Patients/SearchPatients/SearchPatientsQueryHandler.cs`: when set, add an
  in-module `EXISTS` filter against `PatientReports`:
  ```csharp
  if (query.ProviderId is { } prov)
      q = q.Where(p => dbContext.PatientReports.Any(r =>
          r.PatientId == p.Id && !r.IsDeleted && r.ProviderId == prov));

  if (query.ClinicId is { } clinic)
      q = q.Where(p => dbContext.PatientReports.Any(r =>
          r.PatientId == p.Id && !r.IsDeleted && r.ClinicId == clinic));
  ```
- `Features/v1/Patients/SearchPatients/SearchPatientsEndpoint.cs`: bind the two new
  query-string params (`providerId`, `clinicId`).
- Validator: existing paginated-query validator stays; no new rules required (the two
  filters are optional GUIDs).
- Conventions: handler remains `public sealed`, returns `ValueTask<T>`, `ConfigureAwait(false)`
  on every await, `CancellationToken` propagated. No cross-module reference; no schema change.

## Frontend changes (dashboard)

- `src/api/patients.ts`: add `providerId?: string` / `clinicId?: string` to
  `SearchPatientsParams`; set them on the query string when present.
- `src/pages/patient-charts/list.tsx`: rewrite from "list everything" to the search-first
  page described above.
  - Provider/Clinic dropdowns reuse the existing Administration option sources already used
    by the chart/scheduling pages (clinic + provider options). If options are unavailable,
    the dropdowns degrade to "All" only.
  - Gate the query `enabled` flag on "has at least one active filter."
  - Results table columns: Last Name, First Name, Code, DOB, Last Visit (BackChart parity,
    within the data the list DTO already returns).
- **Remove the standalone Patients list:** delete `src/pages/patients/list.tsx`, its lazy
  import and `{ path: "patients" }` route in `routes.tsx`, and the `Patients` item in
  `components/layout/nav-data.ts` (collapse the section to the single Patient Chart entry).
  The `patients/:patientId` route is kept.
- Before deleting `list.tsx`, extract its **Register a patient** dialog into a shared
  component (`src/pages/patients/create-patient-dialog.tsx`) with an `onCreated(id)` callback.
  The search page uses it and navigates to `/patient-charts/:id` on success.
- **Add the Edit-patient-info affordance** on the chart's Patient Info card
  (`src/pages/patient-charts/chart.tsx`) → `Link` to `/patients/:patientId`.
- **Repoint the orphaned `/patients` references** in `src/pages/patients/patient-detail.tsx`:
  the `EntityDetailBack`/"Back to patients" links → the patient's chart
  (`/patient-charts/:patientId`); the `onDeleted` redirect → `/patient-charts` (search page).

## Edge cases

- **Empty/default filters:** no fetch, prompt shown (the core behavior change).
- **No provider/clinic option data:** dropdowns show only "All".
- **Provider/Clinic meaning:** a small helper tooltip clarifies "patients with a report for
  this provider/clinic" so it isn't read as "all of a doctor's patients."
- **Soft-deleted reports:** excluded from the EXISTS filter (`!r.IsDeleted`).

## Non-goals

- No A–Z quick-jump row.
- No "Open Incident" filter.
- No inline demographics tab on the chart; the `/patients/:patientId` detail page stays as-is
  (only its back/redirect targets change).
- No new backend endpoint and no database migration.

## Affected files (summary)

Backend:
- `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/SearchPatientsQuery.cs`
- `src/Modules/Patient/Modules.Patient/Features/v1/Patients/SearchPatients/SearchPatientsQueryHandler.cs`
- `src/Modules/Patient/Modules.Patient/Features/v1/Patients/SearchPatients/SearchPatientsEndpoint.cs`

Frontend:
- `clients/dashboard/src/api/patients.ts` (add provider/clinic params)
- `clients/dashboard/src/pages/patient-charts/list.tsx` (rewrite → search-first page)
- `clients/dashboard/src/pages/patient-charts/chart.tsx` (add Edit-patient-info link)
- `clients/dashboard/src/pages/patients/create-patient-dialog.tsx` (new, shared dialog)
- `clients/dashboard/src/pages/patients/list.tsx` (deleted)
- `clients/dashboard/src/pages/patients/patient-detail.tsx` (repoint back/delete targets)
- `clients/dashboard/src/routes.tsx` (drop `patients` list route + lazy import)
- `clients/dashboard/src/components/layout/nav-data.ts` (single Patient Chart entry)

Tests:
- Patient module: search-handler unit/integration coverage for the provider/clinic filters.
- Dashboard: Playwright route-mocked coverage for the search page (prompt → results → open chart, + New → chart).
