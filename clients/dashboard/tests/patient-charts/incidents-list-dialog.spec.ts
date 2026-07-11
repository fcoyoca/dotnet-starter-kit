// E2E coverage for the "Incidents" dialog (route-mocked) — BackChart parity:
// when a patient chart first loads with more than one OPEN incident, the
// dialog pops up so the user picks which incident to load; with one (or no)
// open incident it stays closed. It also opens from the Incident card's
// view (eye) button and its "DOIV:" row, and each incident card exposes a
// collapsible "Reports (n)" list.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PERMS = [
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Incidents.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

function incident(id: string, dateOfInitialVisit: string, isClosed = false) {
  return {
    id,
    patientId: PATIENT_ID,
    incidentTypeId: null,
    departmentId: null,
    dateOfInitialVisit,
    dateOfLoss: "2026-05-01",
    isClosed,
    isTransfer: false,
    isAccident: false,
    accidentType: null,
    accidentState: null,
    patientStatus: "Active",
    diagnosticIds: [],
    createdAtUtc: "2026-05-01T00:00:00Z",
    updatedAtUtc: null,
  };
}

const INCIDENT_A = incident("00000000-0000-0000-0000-0000000c3331", "2026-05-01T12:00:00Z");
const INCIDENT_B = incident("00000000-0000-0000-0000-0000000c3332", "2026-06-15T12:00:00Z");

const REPORT = {
  id: "00000000-0000-0000-0000-0000000e5555",
  incidentId: INCIDENT_A.id,
  patientId: PATIENT_ID,
  reportTypeId: 1,
  reportDate: "2026-06-20",
  version: 1,
  providerId: null,
  clinicId: null,
  isNoShow: false,
  workflowStatus: "Draft",
  isSigned: false,
  signedByName: null,
  signedOnUtc: null,
  createdAtUtc: "2026-06-20T00:00:00Z",
  updatedAtUtc: null,
};

const REPORT_TYPES = [{ id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true }];

async function mockChart(page: Page, incidents: unknown[]) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged(incidents));
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([REPORT]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

function incidentsDialog(page: Page) {
  return page.getByRole("dialog").filter({ hasText: "incidents open for this patient" });
}

test.describe("incidents dialog", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
  });

  test("pops up on chart load with more than one open incident; Select loads that incident", async ({ page }) => {
    await mockChart(page, [INCIDENT_A, INCIDENT_B]);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    const dialog = incidentsDialog(page);
    await expect(dialog.getByRole("heading", { name: "Incidents" })).toBeVisible();
    await expect(dialog).toContainText("There are 2 incidents open for this patient.");
    await expect(dialog.getByRole("button", { name: "Select" })).toHaveCount(2);

    // Select the second incident → dialog closes, its DOIV lands on the card.
    await dialog.getByRole("button", { name: "Select" }).nth(1).click();
    await expect(dialog).toBeHidden();
    await expect(page.getByRole("button", { name: /DOIV: Jun 15, 2026/ })).toBeVisible();
  });

  test("stays closed with a single open incident; opens from the eye button with reports list", async ({ page }) => {
    await mockChart(page, [INCIDENT_A, incident("00000000-0000-0000-0000-0000000c3333", "2026-04-01T12:00:00Z", true)]);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Chart is rendered (incident loaded onto the card) and no auto-popup —
    // only one of the two incidents is open.
    await expect(page.getByRole("button", { name: /DOIV: May 01, 2026/ })).toBeVisible();
    await expect(incidentsDialog(page)).toBeHidden();

    // Eye button → Incidents dialog, listing only the open incident.
    await page.getByRole("button", { name: "View incidents" }).click();
    const dialog = incidentsDialog(page);
    await expect(dialog).toContainText("There are 1 incidents open for this patient.");
    await expect(dialog.getByRole("button", { name: "Select" })).toHaveCount(1);

    // Reports (n) expands into the incident's report rows.
    await dialog.getByRole("button", { name: "Reports (1)" }).click();
    await expect(dialog.getByRole("button", { name: /Initial Evaluation/ })).toBeVisible();

    // Close, then reopen from the card's "DOIV:" row.
    // Two "Close" affordances exist (footer button + the dialog's X icon).
    await dialog.getByRole("button", { name: "Close" }).first().click();
    await expect(dialog).toBeHidden();
    await page.getByRole("button", { name: /DOIV: May 01, 2026/ }).click();
    await expect(dialog).toBeVisible();
  });
});
