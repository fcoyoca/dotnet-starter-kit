// E2E coverage for the Dx Codes line in the open report's header.
//
// The dx codes on a report are the ones recorded on the report's INCIDENT (the
// set the Diagnostic Codes dialog edits) — not the report's associated problems.
// Legacy BackChart prints them on every report, directly under the DOIV/DOL line,
// so a clinician writing the note can see what the visit is being coded against
// without leaving the report. These specs pin that line.

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
const INCIDENT_C = "00000000-0000-0000-0000-0000000c3333";
const REPORT_A = "00000000-0000-0000-0000-0000000d4444";
const REPORT_B = "00000000-0000-0000-0000-0000000d5555";
const REPORT_C = "00000000-0000-0000-0000-0000000d6666";

const REPORT_B_TAB = "Initial Evaluation · Jun 27, 2026";

// The tenant's custom diagnostics, keyed by the ids an incident stores.
const DX = {
  d1: { id: "d1", code: "M54.5", description: "Low back pain", longDescription: "Low back pain, unspecified", isChiropractic: true, isActive: true, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
  d2: { id: "d2", code: "M99.01", description: "Segmental dysfunction, cervical", longDescription: null, isChiropractic: true, isActive: true, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
  d3: { id: "d3", code: "S13.4XXA", description: "Sprain of cervical spine", longDescription: null, isChiropractic: true, isActive: true, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
} as const;

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  referralTypeId: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

function incident(id: string, diagnosticIds: string[]) {
  return {
    id,
    patientId: PATIENT_ID,
    incidentTypeId: null,
    departmentId: null,
    dateOfInitialVisit: "2026-05-01T12:00:00Z",
    dateOfLoss: "2026-04-10T12:00:00Z",
    isClosed: false,
    isTransfer: false,
    isAccident: false,
    accidentType: null,
    accidentState: null,
    patientStatus: "Active",
    diagnosticIds,
    createdAtUtc: "2026-05-01T12:00:00Z",
    updatedAtUtc: null,
  };
}

// A carries two dx, B a different one, C none at all.
const INC_A = incident(INCIDENT_A, ["d1", "d2"]);
const INC_B = incident(INCIDENT_B, ["d3"]);
const INC_C = incident(INCIDENT_C, []);

const REPORT_TYPES = [{ id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true }];
const FIELDS_TYPE_1 = [
  { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
];

function report(id: string, incidentId: string, reportDate: string) {
  return {
    id,
    incidentId,
    patientId: PATIENT_ID,
    reportTypeId: 1,
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

const RPT_A = report(REPORT_A, INCIDENT_A, "2026-06-26T12:00:00Z");
const RPT_B = report(REPORT_B, INCIDENT_B, "2026-06-27T12:00:00Z");
const RPT_C = report(REPORT_C, INCIDENT_C, "2026-06-28T12:00:00Z");

async function mockChart(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", FIELDS_TYPE_1);
  await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));

  // Resolve only the ids actually asked for, so a header showing the wrong
  // incident's codes can't pass by echoing the whole catalogue back.
  await page.route("**/api/v1/administration/custom-diagnostics**", async (route) => {
    const ids = new URL(route.request().url()).searchParams.getAll("ids");
    const items = ids.map((id) => DX[id as keyof typeof DX]).filter(Boolean);
    await route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(paged(items)),
    });
  });

  // LIFO: the broad globs also match the by-id URLs, so the detail mocks are
  // registered after them to win.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INC_A, INC_B, INC_C]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_A}`, INC_A);
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_B}`, INC_B);
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_C}`, INC_C);

  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([RPT_A]));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_A, RPT_A);
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_B, RPT_B);
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_C, RPT_C);
}

/** Load the chart, dismissing the Incidents chooser that auto-opens on a
 *  multi-open-incident patient (it's modal, so the chart is inert behind it). */
async function openChart(page: Page) {
  await page.goto(`/patient-charts/${PATIENT_ID}`);
  const dialog = page.getByRole("dialog").filter({ hasText: "incidents open for this patient" });
  await dialog.getByRole("button", { name: "Close" }).first().click();
  await expect(dialog).toBeHidden();
}

test.describe("report header Dx codes", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
  });

  test("the open report lists its incident's dx codes under the DOIV line", async ({ page }) => {
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_A,
        openReportIds: [REPORT_A],
        activeReportId: REPORT_A,
      },
    ]);
    await openChart(page);

    const dx = page.getByTestId("report-editor-panel").getByTestId("report-dx-codes");
    await expect(dx).toContainText("M54.5");
    await expect(dx).toContainText("M99.01");
    // The description rides along as the code's tooltip, as in BackChart.
    await expect(dx.getByText("M54.5")).toHaveAttribute("title", /Low back pain/);
  });

  test("the codes follow the report's own incident, not the chart's active one", async ({ page }) => {
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_A,
        openReportIds: [REPORT_A, REPORT_B],
        activeReportId: REPORT_A,
      },
    ]);
    await openChart(page);

    const dx = page.getByTestId("report-editor-panel").getByTestId("report-dx-codes");
    await expect(dx).toContainText("M54.5");

    // Report B is filed under incident B — its header must show B's dx set even
    // though the chart is still working in incident A.
    await page.getByRole("button", { name: REPORT_B_TAB, exact: true }).click();
    await expect(dx).toContainText("S13.4XXA");
    await expect(dx).not.toContainText("M54.5");
  });

  test("an incident with no dx codes still shows the line, with a placeholder", async ({ page }) => {
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_C,
        openReportIds: [REPORT_C],
        activeReportId: REPORT_C,
      },
    ]);
    await openChart(page);

    // Present but empty beats absent: a missing line reads as "not loaded yet".
    await expect(
      page.getByTestId("report-editor-panel").getByTestId("report-dx-codes"),
    ).toContainText("—");
  });
});
