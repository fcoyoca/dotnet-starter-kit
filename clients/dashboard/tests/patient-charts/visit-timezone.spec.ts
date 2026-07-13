// E2E coverage for the chart's Last/Next visit rows rendering the appointment's
// UTC start in the *owning clinic's* timezone (route-mocked) — the same way the
// scheduler does. A UTC instant must read as the clinic wall-clock time for every
// viewer, regardless of the browser's timezone. Uses America/Phoenix (fixed UTC-7,
// no DST) so the expected label is deterministic year-round.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PERMS = ["Permissions.Patient.Incidents.View", "Permissions.Scheduling.Appointments.View"];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a2222";
const CLINIC_ID = "00000000-0000-0000-0000-0000000b1111";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-55501",
  isActive: true,
  demographics: { firstName: "Marco", middleInitial: "T", lastName: "Reyes", dateOfBirth: "1985-02-01", gender: "M" },
  referralTypeId: null,
  lastVisitDate: null,
  nextVisitDate: null,
  // Both derived visits belong to the Phoenix clinic (UTC-7). 15:00Z → 8:00 AM,
  // 22:30Z → 3:30 PM — proving the UTC instant is offset into the clinic zone.
  lastVisitAppointment: { appointmentId: "appt-last", clinicId: CLINIC_ID, startUtc: "2026-06-20T15:00:00Z" },
  nextVisitAppointment: { appointmentId: "appt-next", clinicId: CLINIC_ID, startUtc: "2026-07-15T22:30:00Z" },
};

const CLINICS = paged([
  {
    id: CLINIC_ID,
    code: "PHX",
    name: "Phoenix Clinic",
    address1: "1 Desert Rd",
    city: "Phoenix",
    state: "AZ",
    zip: "85001",
    timeZoneId: "America/Phoenix",
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
  },
]);

async function mockChart(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", CLINICS);
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

test.describe("chart last/next visit renders in the clinic's timezone", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
  });

  test("shows the clinic wall-clock time, not raw UTC or browser-local", async ({ page }) => {
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // 2026-06-20T15:00:00Z in America/Phoenix (UTC-7) = 8:00 AM.
    await expect(page.getByRole("button", { name: "Jun 20, 2026 · 8:00 AM" })).toBeVisible();
    // 2026-07-15T22:30:00Z in America/Phoenix (UTC-7) = 3:30 PM.
    await expect(page.getByRole("button", { name: "Jul 15, 2026 · 3:30 PM" })).toBeVisible();
  });
});
