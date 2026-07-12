// E2E coverage for Part C's draft persistence: unsaved report typing
// survives switching between open report tabs (unmount flush) and a full
// reload (debounced sessionStorage write), and a successful Save clears the
// draft so it can't shadow the server copy.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const REPORT_PERMS = [
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Reports.Update",
  "Permissions.Patient.Incidents.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3333";
const REPORT_1 = "00000000-0000-0000-0000-0000000d4444"; // Initial Evaluation (type 1, field 11)
const REPORT_2 = "00000000-0000-0000-0000-0000000d5555"; // Progress Note (type 2, field 21)

const DRAFTS_KEY = "fsh.dashboard.reportDrafts.v1";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const INCIDENT = {
  id: INCIDENT_ID,
  patientId: PATIENT_ID,
  incidentTypeId: null,
  departmentId: null,
  dateOfInitialVisit: null,
  dateOfLoss: "2026-06-20",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [],
  createdAtUtc: "2026-06-20T08:00:00Z",
  updatedAtUtc: null,
};

const REPORT_TYPES = [
  { id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true },
  { id: 2, name: "Progress Note", displayOrder: 1, isActive: true },
];

// Per-type field sets so each report's textarea has a distinct id.
const FIELDS_TYPE_1 = [
  { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
];
const FIELDS_TYPE_2 = [
  { id: 21, reportTypeId: 2, name: "Progress", category: "Subjective", displayOrder: 0, isActive: true },
];

function report(id: string, reportTypeId: number) {
  return {
    id,
    incidentId: INCIDENT_ID,
    patientId: PATIENT_ID,
    reportTypeId,
    reportDate: "2026-06-26T00:00:00Z",
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

async function mockLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  // The panel queries report-fields?reportTypeId=N — route on the param.
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=1**", FIELDS_TYPE_1);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=2**", FIELDS_TYPE_2);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  await mockJsonResponse(
    page,
    "**/api/v1/patient/reports**",
    paged([report(REPORT_1, 1), report(REPORT_2, 2)]),
  );
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_1, report(REPORT_1, 1));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_2, report(REPORT_2, 2));
}

async function seedBothReportsOpen(page: Page) {
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_ID,
      openReportIds: [REPORT_1, REPORT_2],
      activeReportId: REPORT_1,
    },
  ]);
}

/** The parsed draft map from sessionStorage (empty object when unset). */
async function readDraftMap(page: Page): Promise<Record<string, unknown>> {
  return page.evaluate((key) => {
    const raw = sessionStorage.getItem(key);
    return raw ? (JSON.parse(raw) as Record<string, unknown>) : {};
  }, DRAFTS_KEY);
}

test.describe("report drafts", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", REPORT_PERMS);
    await mockLookups(page);
  });

  test("typing survives switching between open report tabs", async ({ page }) => {
    await seedBothReportsOpen(page);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    await page.locator("#f-11").fill("unsaved chief complaint text");

    // Switch to the second open report (unmounts the panel → flush write).
    await page.getByRole("button", { name: "Progress Note", exact: true }).click();
    await expect(page.locator("#f-21")).toBeVisible();
    await expect(page.locator("#f-21")).toHaveValue("");

    // Switch back — the draft (not the empty server copy) hydrates.
    await page.getByRole("button", { name: "Initial Evaluation", exact: true }).click();
    await expect(page.locator("#f-11")).toHaveValue("unsaved chief complaint text");
  });

  test("typing survives a full reload via the debounced sessionStorage write", async ({ page }) => {
    await seedBothReportsOpen(page);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    await page.locator("#f-11").fill("text that must survive a reload");

    // Wait for the 500ms debounce to land in sessionStorage (a hard
    // navigation skips React cleanup, so the flush-on-unmount can't help).
    await expect
      .poll(async () => {
        const map = await readDraftMap(page);
        return map[REPORT_1] != null;
      })
      .toBe(true);

    await page.reload();

    await expect(page.locator("#f-11")).toHaveValue("text that must survive a reload");
  });

  test("a successful Save clears the report's draft", async ({ page }) => {
    await seedBothReportsOpen(page);
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    await page.locator("#f-11").fill("about to be saved");
    await expect
      .poll(async () => {
        const map = await readDraftMap(page);
        return map[REPORT_1] != null;
      })
      .toBe(true);

    // Method-filtered PUT mock layers over the GET detail mock.
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_1, '""', { method: "PUT" });
    await page.getByRole("button", { name: /save draft/i }).click();
    await expect(page.getByText("Report saved.")).toBeVisible();

    await expect
      .poll(async () => {
        const map = await readDraftMap(page);
        return map[REPORT_1] == null;
      })
      .toBe(true);
  });
});
