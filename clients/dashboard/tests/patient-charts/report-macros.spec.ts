// E2E coverage for the macro picker on a report field.
//
// Legacy BackChart opens a Macros DIALOG holding a working copy of the field's
// text beside the macro list: you pick macros into that staging field, edit the
// result, and only "Complete" writes it back to the report. Picking a macro is
// therefore reversible and composable — you can insert several, fix the wording,
// or back out entirely. Our first port skipped the staging field and spliced the
// macro straight into the live report field, with no way to preview or cancel.
// These specs pin the staging behaviour.

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
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3331";
const REPORT_ID = "00000000-0000-0000-0000-0000000d4444";

const FIELD_ID = 11;
/** What the report field already holds when the chart loads. */
const EXISTING = "Patient reports neck pain.";

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
  dateOfInitialVisit: "2026-05-01T12:00:00Z",
  dateOfLoss: "2026-04-10T12:00:00Z",
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

const REPORT_TYPES = [{ id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true }];
const FIELDS = [
  { id: FIELD_ID, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
];

function macro(id: string, name: string, text: string, reportFieldId: number | null) {
  return {
    id,
    name,
    text,
    reportFieldId,
    reportFieldName: reportFieldId == null ? null : "Chief Complaint",
    reportCategory: null,
    useableByUserId: null,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  };
}

const MACRO_FIELD = macro("m1", "Normal Exam", "Cervical ROM within normal limits.", FIELD_ID);
const MACRO_GENERAL = macro("m2", "Signature Block", "Reviewed with patient.", null);

const REPORT = {
  id: REPORT_ID,
  incidentId: INCIDENT_ID,
  patientId: PATIENT_ID,
  reportTypeId: 1,
  reportDate: "2026-06-26T12:00:00Z",
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
  fieldValues: [{ reportFieldId: FIELD_ID, text: EXISTING }],
  addendums: [],
  associatedProblemIds: [],
  createdAtUtc: "2026-06-26T08:00:00Z",
  updatedAtUtc: null,
};

async function mockChart(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", FIELDS);
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/custom-diagnostics**", paged([]));

  // The picker asks twice — the field's own bucket and the "All (General)" one.
  await page.route("**/api/v1/administration/macros?**", async (route) => {
    const general = new URL(route.request().url()).searchParams.get("general") === "true";
    await route.fulfill({
      status: 200,
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(paged(general ? [MACRO_GENERAL] : [MACRO_FIELD])),
    });
  });

  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_ID}`, INCIDENT);
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([REPORT]));
  await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, REPORT);
}

async function openChart(page: Page) {
  await seedPatientWorkspace(page, [
    {
      patientId: PATIENT_ID,
      patientLabel: "Alice Q Vance",
      activeIncidentId: INCIDENT_ID,
      openReportIds: [REPORT_ID],
      activeReportId: REPORT_ID,
    },
  ]);
  await page.goto(`/patient-charts/${PATIENT_ID}`);
  await expect(page.getByTestId("report-editor-panel")).toBeVisible();
  // Macros act on the editable field, so unlock the note into the editor first.
  await enterReportEdit(page);
}

/** The live report field the macro eventually lands in. */
const reportField = (page: Page) => page.locator(`#f-${FIELD_ID}`);
/** The dialog's working copy of that field's text. */
const stagingField = (page: Page) => page.getByTestId("macro-staging-text");

async function openMacros(page: Page) {
  await page.getByRole("button", { name: "Macro", exact: true }).click();
  await expect(page.getByRole("dialog", { name: "Macros" })).toBeVisible();
}

test.describe("report field macros", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
  });

  test("the dialog opens on a working copy of the field's text, beside the macro list", async ({ page }) => {
    await openChart(page);
    await expect(reportField(page)).toHaveValue(EXISTING);

    await openMacros(page);

    // The staging field starts as the report field's current text — the macro is
    // composed against what's already written, not into a void.
    await expect(stagingField(page)).toHaveValue(EXISTING);
    // Both buckets are listed: the field's own macros and the general ones.
    await expect(page.getByRole("button", { name: /Normal Exam/ })).toBeVisible();
    await expect(page.getByRole("button", { name: /Signature Block/ })).toBeVisible();
  });

  test("picking a macro stages it — the report field is untouched until Complete", async ({ page }) => {
    await openChart(page);
    await openMacros(page);

    await page.getByRole("button", { name: /Normal Exam/ }).click();

    // Staged, appended to the existing text — on the SAME line. A macro continues
    // the sentence the clinician is writing; it does not start a new paragraph.
    await expect(stagingField(page)).toHaveValue(EXISTING + MACRO_FIELD.text);
    // …but the report itself has not changed yet.
    await expect(reportField(page)).toHaveValue(EXISTING);

    // Several macros can be composed into the one staged edit before committing.
    await page.getByRole("button", { name: /Signature Block/ }).click();
    await expect(stagingField(page)).toHaveValue(
      EXISTING + MACRO_FIELD.text + MACRO_GENERAL.text,
    );

    await page.getByRole("button", { name: "Complete" }).click();
    await expect(page.getByRole("dialog", { name: "Macros" })).toBeHidden();

    // Complete is what writes the staged text back to the field.
    await expect(reportField(page)).toHaveValue(
      EXISTING + MACRO_FIELD.text + MACRO_GENERAL.text,
    );
  });

  test("a macro inserts at the caret, splitting the line rather than breaking it", async ({ page }) => {
    await openChart(page);
    await openMacros(page);

    // Put the caret right after "Patient reports " and insert there.
    const caret = "Patient reports ".length;
    await stagingField(page).click();
    await stagingField(page).evaluate((el: HTMLTextAreaElement, at: number) => {
      el.setSelectionRange(at, at);
    }, caret);

    await page.getByRole("button", { name: /Normal Exam/ }).click();

    await expect(stagingField(page)).toHaveValue(
      EXISTING.slice(0, caret) + MACRO_FIELD.text + EXISTING.slice(caret),
    );
  });

  test("the staged text is editable before it is committed", async ({ page }) => {
    await openChart(page);
    await openMacros(page);

    await page.getByRole("button", { name: /Normal Exam/ }).click();
    await stagingField(page).fill("Rewritten by hand.");
    await page.getByRole("button", { name: "Complete" }).click();

    await expect(reportField(page)).toHaveValue("Rewritten by hand.");
  });

  test("closing the dialog discards the staged macro", async ({ page }) => {
    await openChart(page);
    await openMacros(page);

    await page.getByRole("button", { name: /Normal Exam/ }).click();
    await expect(stagingField(page)).toHaveValue(new RegExp(MACRO_FIELD.text));

    await page.getByRole("button", { name: "Cancel" }).click();
    await expect(page.getByRole("dialog", { name: "Macros" })).toBeHidden();

    // Nothing was committed — the field is exactly as it was.
    await expect(reportField(page)).toHaveValue(EXISTING);

    // And re-opening starts from the field again, not from the discarded draft.
    await openMacros(page);
    await expect(stagingField(page)).toHaveValue(EXISTING);
  });
});
