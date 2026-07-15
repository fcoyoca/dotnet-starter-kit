// E2E coverage for the incident DOIV/DOL labels on the chart.
//
// A chart can hold reports from several incidents open at once, and the chart's
// report list only ever covers the ACTIVE incident. So a report opened under
// incident A and then viewed while incident B is active had nothing on screen
// tying it back to A. These specs pin the labels that fix that: the chart's
// identity line, each open-report tab, and the open report's own header.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";
import { enterReportEdit } from "../helpers/report-editor";

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

// Midday-UTC instants: the browser runs in a positive-offset zone here, and a
// bare "2026-05-01" would render as the previous day in a negative one.
const A_REF = "DOIV May 01, 2026 · DOL Apr 10, 2026";
const B_REF = "DOIV Jun 15, 2026 · DOL Jun 01, 2026";
// Pills are labelled "<type name> · <date>" (legacy parity).
const REPORT_A_TAB = "Initial Evaluation · Jun 26, 2026";
const REPORT_B_TAB = "Progress Note · Jun 27, 2026";

/** The marker on a tab whose report belongs to some OTHER incident. */
const FOREIGN = "Belongs to a different incident than the one selected";

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
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));

  // LIFO: the broad globs also match the by-id URLs, so the detail mocks are
  // registered after them to win.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INC_A, INC_B]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_A}`, INC_A);
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_B}`, INC_B);

  // The chart's report LIST is scoped to the active incident (A), so B's report
  // is deliberately absent from it — the tab for B has to resolve on its own.
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([RPT_A]));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_A, RPT_A);
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_B, RPT_B);
}

/**
 * Load the chart and dismiss the Incidents chooser, which auto-opens because
 * this patient has two OPEN incidents. It's a modal, so the chart behind it is
 * aria-hidden and invisible to role queries until it's closed.
 */
async function openChart(page: Page) {
  await page.goto(`/patient-charts/${PATIENT_ID}`);
  const dialog = page.getByRole("dialog").filter({ hasText: "incidents open for this patient" });
  await dialog.getByRole("button", { name: "Close" }).first().click();
  await expect(dialog).toBeHidden();
}

/** Incident A active, with reports from BOTH incidents open — the mix-up case. */
async function seedTwoIncidentsOpen(page: Page) {
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_A,
      openReportIds: [REPORT_A, REPORT_B],
      activeReportId: REPORT_A,
    },
  ]);
}

test.describe("incident DOIV/DOL labels", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
    await seedTwoIncidentsOpen(page);
  });

  test("the chart identity line and the Incident card carry the active incident's DOIV + DOL", async ({ page }) => {
    await openChart(page);

    // Beside the patient name.
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(A_REF);
    // The Incident card used to show DOIV alone.
    await expect(page.getByRole("button", { name: /DOIV: May 01, 2026/ })).toContainText("DOL: Apr 10, 2026");
  });

  test("each open-report tab is labelled with its OWN incident, and a foreign one is flagged", async ({ page }) => {
    await openChart(page);

    const tabs = page.getByTestId("open-report-tab");
    await expect(tabs).toHaveCount(2);

    const tabA = tabs.filter({ hasText: REPORT_A_TAB });
    const tabB = tabs.filter({ hasText: REPORT_B_TAB });

    // B is absent from the active incident's report list, so before this it
    // rendered as the bare word "Report" with no incident context at all.
    await expect(tabA).toContainText(A_REF);
    await expect(tabB).toContainText(B_REF);

    // Only B belongs to a different incident than the active one.
    await expect(tabB.getByLabel(FOREIGN)).toBeVisible();
    await expect(tabA.getByLabel(FOREIGN)).toHaveCount(0);
  });

  test("the whole open-report tab selects it, not just the date text", async ({ page }) => {
    await openChart(page);
    const panel = page.getByTestId("report-editor-panel");
    await expect(panel.getByTestId("report-incident-ref")).toHaveText(A_REF);

    // Click the tab's DOIV/DOL line — outside the date, inside the pill. When
    // only the date was clickable this did nothing, which read as a disabled tab.
    await page.getByTestId("open-report-tab").filter({ hasText: REPORT_B_TAB }).getByText(B_REF).click();
    await expect(panel.getByTestId("report-incident-ref")).toHaveText(B_REF);
  });

  test("the open report's header names the incident the report is filed under", async ({ page }) => {
    await openChart(page);

    const panel = page.getByTestId("report-editor-panel");
    await expect(panel.getByTestId("report-incident-ref")).toHaveText(A_REF);

    // Switch to the report from the OTHER incident: the header must follow the
    // report, not the chart's active incident (which stays A).
    await page.getByRole("button", { name: REPORT_B_TAB, exact: true }).click();
    await expect(panel.getByTestId("report-incident-ref")).toHaveText(B_REF);

    // The chart's own identity line still shows the active incident, A.
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(A_REF);
  });

  test("a report from a non-active incident is read-only — no saving or signing", async ({ page }) => {
    await openChart(page);
    const panel = page.getByTestId("report-editor-panel");

    // The active report (A) is on the active incident: editable, so it offers an
    // Edit affordance and — once unlocked — a live form with Save.
    await expect(panel.getByTestId("foreign-incident-notice")).toBeHidden();
    await expect(panel.getByTestId("report-edit-button")).toBeVisible();
    await enterReportEdit(page);
    await expect(panel.getByRole("button", { name: "Save Draft" })).toBeVisible();
    await expect(panel.locator("#f-11")).toBeEnabled();

    // Switch to B's report — filed under an incident the chart isn't in.
    await page.getByRole("button", { name: REPORT_B_TAB, exact: true }).click();
    await expect(panel.getByTestId("foreign-incident-notice")).toBeVisible();

    // It's read-only: no Edit affordance, and no saving or signing.
    await expect(panel.getByTestId("report-edit-button")).toHaveCount(0);
    await expect(panel.getByRole("button", { name: "Save Draft" })).toHaveCount(0);
    await expect(panel.getByRole("button", { name: "Sign Report" })).toHaveCount(0);
  });

  test("switching to the report's own incident unlocks it", async ({ page }) => {
    await openChart(page);
    const panel = page.getByTestId("report-editor-panel");

    await page.getByRole("button", { name: REPORT_B_TAB, exact: true }).click();
    await expect(panel.getByTestId("foreign-incident-notice")).toBeVisible();

    // The switch locks report A (open on the current incident), so it's confirmed
    // first — see incident-switch-confirm.spec.ts.
    await panel.getByRole("button", { name: "Switch to this incident" }).click();
    await page
      .getByTestId("incident-switch-confirm")
      .getByRole("button", { name: "Switch incident" })
      .click();

    // B is now the chart's active incident: the report is editable again, the
    // notice is gone, and the chart's identity line has followed.
    await expect(panel.getByTestId("foreign-incident-notice")).toBeHidden();
    await enterReportEdit(page);
    await expect(panel.locator("#f-21")).toBeEnabled();
    await expect(panel.getByRole("button", { name: "Save Draft" })).toBeVisible();
    await expect(page.getByTestId("chart-incident-ref")).toHaveText(B_REF);
  });
});
