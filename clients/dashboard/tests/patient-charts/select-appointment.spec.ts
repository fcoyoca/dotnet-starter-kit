// E2E coverage for the "Select Appointment" step before creating a report
// (route-mocked). Adding a report from the chart opens the Select Appointment
// dialog; picking an appointment stamps its date/clinic/provider/appointmentId
// onto the POST, while "Manually Enter Date" posts a bare date with no link.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PERMS = [
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Reports.Create",
  "Permissions.Patient.Incidents.View",
  "Permissions.Scheduling.Appointments.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3333";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  referralTypeId: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const INCIDENT = {
  id: INCIDENT_ID,
  patientId: PATIENT_ID,
  incidentTypeId: null,
  departmentId: null,
  dateOfInitialVisit: null,
  dateOfLoss: "2026-06-01",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [],
  createdAtUtc: "2026-06-01T00:00:00Z",
  updatedAtUtc: null,
};

const REPORT_TYPES = [{ id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true }];

const PROVIDERS = paged([
  { id: "prov-1", firstName: "Greg", lastName: "House", prefix: "Dr.", suffix: "MD", isActive: true },
]);

const APPOINTMENT = {
  id: "appt-1",
  clinicId: "clinic-1",
  providerId: "prov-1",
  patientId: PATIENT_ID,
  appointmentTypeId: null,
  startUtc: "2026-06-20T15:00:00Z",
  endUtc: "2026-06-20T15:30:00Z",
  notes: null,
  status: "Scheduled",
  cancelled: false,
  noShow: false,
  isReservation: false,
  reservationTitle: null,
};

async function mockChart(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", PROVIDERS);
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

async function openAppointmentDialog(page: Page) {
  await page.goto(`/patient-charts/${PATIENT_ID}`);
  await page.getByRole("button", { name: "Add Report" }).click();
  await page.getByRole("menuitem", { name: "Initial Evaluation" }).click();
  return page.getByRole("dialog").filter({ hasText: "Select Appointment" });
}

test.describe("select appointment before creating a report", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
  });

  test("picking an appointment stamps its date/clinic/provider/appointmentId on the POST", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/scheduling/appointments/by-patient/**", [APPOINTMENT]);
    await mockJsonResponse(page, "**/api/v1/patient/reports", '"new-report-id"', { method: "POST" });

    const dialog = await openAppointmentDialog(page);
    await expect(dialog.getByRole("heading", { name: "Select Appointment" })).toBeVisible();

    const postRequest = page.waitForRequest(
      (req) => req.url().endsWith("/api/v1/patient/reports") && req.method() === "POST",
    );
    await dialog.getByRole("button", { name: /Dr\. Greg House/ }).click();
    await dialog.getByRole("button", { name: "Use This Appointment" }).click();

    const body = (await postRequest).postDataJSON();
    expect(body.reportTypeId).toBe(1);
    expect(body.appointmentId).toBe("appt-1");
    expect(body.reportDate).toBe("2026-06-20");
    expect(body.clinicId).toBe("clinic-1");
    expect(body.providerId).toBe("prov-1");
  });

  test("Manually Enter Date posts a bare date with no appointment link", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/scheduling/appointments/by-patient/**", [APPOINTMENT]);
    await mockJsonResponse(page, "**/api/v1/patient/reports", '"new-report-id"', { method: "POST" });

    const dialog = await openAppointmentDialog(page);
    await dialog.getByRole("button", { name: "Manually Enter Date" }).click();

    await dialog.locator("#appt-manual-date").fill("2026-06-25");

    const postRequest = page.waitForRequest(
      (req) => req.url().endsWith("/api/v1/patient/reports") && req.method() === "POST",
    );
    await dialog.getByRole("button", { name: "Use This Date" }).click();

    const body = (await postRequest).postDataJSON();
    expect(body.reportDate).toBe("2026-06-25");
    expect(body.appointmentId).toBeNull();
    expect(body.clinicId).toBeNull();
    expect(body.providerId).toBeNull();
  });
});
