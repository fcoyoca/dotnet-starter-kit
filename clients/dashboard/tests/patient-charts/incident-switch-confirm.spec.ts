// E2E coverage for the incident-switch confirmation gate.
//
// A chart works in one incident at a time, but reports stay open across a
// switch — and a report from a non-active incident is read-only. Switching
// therefore silently locked whatever the user had open. Now every user-initiated
// switch confirms first: proceeding locks the stranded reports (and says so with
// a chart-level banner), cancelling does nothing at all.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Reports.Update",
  "Permissions.Patient.Incidents.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_A = "00000000-0000-0000-0000-0000000c3331";
const INCIDENT_B = "00000000-0000-0000-0000-0000000c3332";
const REPORT_A = "00000000-0000-0000-0000-0000000d4444";
const REPORT_B = "00000000-0000-0000-0000-0000000d5555";

const A_REF = "DOIV May 01, 2026 · DOL Apr 10, 2026";
const B_REF = "DOIV Jun 15, 2026 · DOL Jun 01, 2026";
const REPORT_B_TAB = "Jun 27, 2026";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  referralTypeId: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

function incident(id: string, dateOfInitialVisit: string, dateOfLoss: string) {
  return {
    id,
    patientId: PATIENT_ID,
    incidentTypeId: null,
    departmentId: null,
    dateOfInitialVisit,
    dateOfLoss,
    isClosed: false,
    isTransfer: false,
    isAccident: false,
    accidentType: null,
    accidentState: null,
    patientStatus: "Active",
    diagnosticIds: [],
    createdAtUtc: "2026-05-01T12:00:00Z",
    updatedAtUtc: null,
  };
}

const INC_A = incident(INCIDENT_A, "2026-05-01T12:00:00Z", "2026-04-10T12:00:00Z");
const INC_B = incident(INCIDENT_B, "2026-06-15T12:00:00Z", "2026-06-01T12:00:00Z");

const REPORT_TYPES = [
  { id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true },
  { id: 2, name: "Progress Note", displayOrder: 1, isActive: true },
];
const FIELDS_TYPE_1 = [
  { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
];
const FIELDS_TYPE_2 = [
  { id: 21, reportTypeId: 2, name: "Progress", category: "Subjective", displayOrder: 0, isActive: true },
];

function report(id: string, incidentId: string, reportTypeId: number, reportDate: string) {
  return {
    id,
    incidentId,
    patientId: PATIENT_ID,
    reportTypeId,
    reportDate,
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

const RPT_A = report(REPORT_A, INCIDENT_A, 1, "2026-06-26T12:00:00Z");
const RPT_B = report(REPORT_B, INCIDENT_B, 2, "2026-06-27T12:00:00Z");

async function mockChart(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=1**", FIELDS_TYPE_1);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=2**", FIELDS_TYPE_2);
  await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));

  // LIFO: the broad globs also match the by-id URLs, so the detail mocks are
  // registered after them to win.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INC_A, INC_B]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_A}`, INC_A);
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_B}`, INC_B);

  // The chart's report list only covers the ACTIVE incident (A); the Incidents
  // dialog lists each incident's own reports, so B resolves to its own.
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([RPT_A]));
  await mockJsonResponse(page, `**/api/v1/patient/reports?incidentId=${INCIDENT_B}**`, paged([RPT_B]));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_A, RPT_A);
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_B, RPT_B);
}

function incidentsDialog(page: Page) {
  return page.getByRole("dialog").filter({ hasText: "incidents open for this patient" });
}

function confirmDialog(page: Page) {
  return page.getByTestId("incident-switch-confirm");
}

/** Incident A active with A's report open — a switch to B would strand it. */
async function seedIncidentAWithReportOpen(page: Page) {
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_A,
      openReportIds: [REPORT_A],
      activeReportId: REPORT_A,
    },
  ]);
}

/**
 * Load the chart and dismiss the chooser that auto-opens (two open incidents),
 * so the switches under test are ordinary user-initiated ones rather than the
 * load-time first pick.
 */
async function openChart(page: Page) {
  await page.goto(`/patient-charts/${PATIENT_ID}`);
  const dialog = incidentsDialog(page);
  await dialog.getByRole("button", { name: "Close" }).first().click();
  await expect(dialog).toBeHidden();
}

/** Reopen the chooser from the Incident card's eye button and pick incident B. */
async function selectIncidentBFromChooser(page: Page) {
  await page.getByRole("button", { name: "View incidents" }).click();
  await incidentsDialog(page).getByRole("button", { name: "Select" }).nth(1).click();
}

test.describe("incident switch confirmation", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
  });

  test("cancelling the confirmation changes nothing", async ({ page }) => {
    await seedIncidentAWithReportOpen(page);
    await openChart(page);

    await selectIncidentBFromChooser(page);

    const confirm = confirmDialog(page);
    await expect(confirm).toBeVisible();
    await expect(confirm).toContainText(B_REF);
    await expect(confirm).toContainText("1 open report from the current incident will become read-only");

    await confirm.getByRole("button", { name: "Cancel" }).click();
    await expect(confirm).toBeHidden();

    // The chart is still on incident A, its report still editable, nothing locked.
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(A_REF);
    const panel = page.getByTestId("report-editor-panel");
    await expect(panel.getByTestId("foreign-incident-notice")).toBeHidden();
    await expect(panel.locator("#f-11")).toBeEnabled();
    await expect(page.getByTestId("foreign-reports-lock-banner")).toBeHidden();
  });

  test("confirming switches the incident and locks the stranded report", async ({ page }) => {
    await seedIncidentAWithReportOpen(page);
    await openChart(page);

    await selectIncidentBFromChooser(page);
    await confirmDialog(page).getByRole("button", { name: "Switch incident" }).click();

    // The chart has moved to incident B…
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(B_REF);

    // …and A's report — still open — is now read-only, called out by the banner.
    const banner = page.getByTestId("foreign-reports-lock-banner");
    await expect(banner).toBeVisible();
    await expect(banner).toContainText("1 report belongs to another incident and is read-only.");

    const panel = page.getByTestId("report-editor-panel");
    await expect(panel.getByTestId("foreign-incident-notice")).toBeVisible();
    await expect(panel.locator("#f-11")).toBeDisabled();
    await expect(panel.getByRole("button", { name: "Save Draft" })).toHaveCount(0);
  });

  test("cancelling a switch from the read-only report's own banner leaves it locked", async ({ page }) => {
    // A active; A's report open (editable) and B's report open (foreign).
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_A,
        openReportIds: [REPORT_A, REPORT_B],
        activeReportId: REPORT_B,
      },
    ]);
    await openChart(page);

    const panel = page.getByTestId("report-editor-panel");
    await panel.getByRole("button", { name: "Switch to this incident" }).click();

    const confirm = confirmDialog(page);
    await expect(confirm).toBeVisible();
    await confirm.getByRole("button", { name: "Cancel" }).click();

    // Still on A: B's report stays read-only rather than silently locking A's.
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(A_REF);
    await expect(panel.getByTestId("foreign-incident-notice")).toBeVisible();
    await expect(panel.locator("#f-21")).toBeDisabled();
  });

  test("cancelling a switch requested by opening another incident's report leaves the report unopened", async ({ page }) => {
    await seedIncidentAWithReportOpen(page);
    await openChart(page);

    // Chooser → incident B's card → its report row: this both switches and opens.
    await page.getByRole("button", { name: "View incidents" }).click();
    const dialog = incidentsDialog(page);
    await dialog.getByRole("button", { name: /^Reports \(/ }).nth(1).click();
    await dialog.getByRole("button", { name: /Progress Note/ }).click();

    await confirmDialog(page).getByRole("button", { name: "Cancel" }).click();

    // No switch, and no new report tab — cancelling is a complete no-op.
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(A_REF);
    await expect(page.getByTestId("open-report-tab")).toHaveCount(1);
  });

  test("the chooser that auto-opens on chart load doesn't ask to confirm its first pick", async ({ page }) => {
    // No seeded workspace: a fresh chart, where the chart auto-selects the first
    // incident before the user has had any say. Picking properly from the chooser
    // it pops up isn't "switching away" from a choice the user made.
    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await incidentsDialog(page).getByRole("button", { name: "Select" }).nth(1).click();

    await expect(confirmDialog(page)).toBeHidden();
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(B_REF);
  });
});
