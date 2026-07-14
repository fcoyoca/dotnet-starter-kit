// E2E coverage for the Patient Reports flow (route-mocked): the report editor
// (draft fields + vitals + Save Draft), a signed report rendering read-only
// with the addendum composer, the peer-review "Request Review" action, and the
// reports panel on the chart page.
//
// Gotcha: GET getReport and PUT updateReport share the URL
// `**/api/v1/patient/reports/{id}`. As in patients.spec.ts, let the unfiltered
// GET mock satisfy the load, register a `{ method: "PUT" }` mock for the save,
// and capture the body via page.waitForRequest.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const REPORT_PERMS = {
  view: "Permissions.Patient.Reports.View",
  create: "Permissions.Patient.Reports.Create",
  update: "Permissions.Patient.Reports.Update",
  sign: "Permissions.Patient.Reports.Sign",
  review: "Permissions.Patient.Reports.Review",
  delete: "Permissions.Patient.Reports.Delete",
};
const ALL_REPORT_PERMS = Object.values(REPORT_PERMS);

async function grantPermissions(page: Page, perms: readonly string[]): Promise<void> {
  await mockJsonResponse(page, "**/api/v1/identity/permissions", perms);
}

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3333";
const REPORT_ID = "00000000-0000-0000-0000-0000000d4444";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: {
    firstName: "Alice",
    middleInitial: "Q",
    lastName: "Vance",
    dateOfBirth: "1990-04-12",
    gender: "F",
  },
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

const REPORT_FIELDS = [
  { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
  { id: 12, reportTypeId: 1, name: "Exam Findings", category: "Objective", displayOrder: 1, isActive: true },
  // Vitals only render for templates that include the Clinical Exam category (legacy
  // rcID 8) — present here so the vitals tests exercise a vitals-capable type.
  { id: 13, reportTypeId: 1, name: "Comments", category: "Clinical Exam", displayOrder: 2, isActive: true },
];

const PROVIDERS = paged([
  { id: "prov-1", firstName: "Greg", lastName: "House", prefix: "Dr.", suffix: "MD", isActive: true },
]);
const CLINICS = paged([{ id: "clinic-1", name: "Downtown Clinic", isActive: true }]);

function draftReport() {
  return {
    id: REPORT_ID,
    incidentId: INCIDENT_ID,
    patientId: PATIENT_ID,
    reportTypeId: 1,
    reportDate: "2026-06-26T00:00:00Z",
    version: 1,
    providerId: null,
    clinicId: null,
    isNoShow: false,
    vitals: {
      heightInches: null,
      weightLbs: null,
      bmi: null,
      systolic: null,
      diastolic: null,
      pulse: null,
      temperatureF: null,
    },
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

function signedReport(overrides: Record<string, unknown> = {}) {
  return {
    ...draftReport(),
    workflowStatus: "Signed",
    isSigned: true,
    signedByUserId: "u-test-1",
    signedByName: "Alice Nguyen",
    signedOnUtc: "2026-06-26T09:00:00Z",
    signatureImagePath: "signatures/prov-1.png",
    signatureImageUrl: "https://api.example.com/files/signatures/prov-1.png",
    providerId: "prov-1",
    fieldValues: [{ reportFieldId: 11, text: "Lower back pain" }],
    ...overrides,
  };
}

async function mockEditorLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", REPORT_FIELDS);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", PROVIDERS);
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", CLINICS);
  // The editor's Associated Problems panel loads the patient's problems.
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
  // The report editor now renders as a dialog over the chart page, so the
  // chart's own queries (incidents list + its filter option lookups) need
  // mocking too — the old bare report route didn't require these.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

/** Seed the workspace so the chart page opens with the report already
 *  active in the right-hand panel, then navigate to the chart. */
async function gotoReportPanel(page: Page): Promise<void> {
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
}

test.describe("patient reports — editor", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await grantPermissions(page, ALL_REPORT_PERMS);
    await mockEditorLookups(page);
  });

  test("draft report renders header, vitals, field sections and action bar", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, draftReport());

    await gotoReportPanel(page);

    await expect(
      page.getByTestId("report-editor-panel").getByText("Alice Q Vance", { exact: true }),
    ).toBeVisible();
    // The header names the report's type, not the bare word "Report".
    await expect(page.getByTestId("report-type-line")).toHaveText("Initial Evaluation · Jun 26, 2026");
    await expect(page.getByText("Vitals")).toBeVisible();
    // Field sections come from listReportFields, grouped by category.
    await expect(page.getByText("Subjective")).toBeVisible();
    await expect(page.getByText("Objective")).toBeVisible();
    await expect(page.getByText("Chief Complaint")).toBeVisible();
    await expect(page.getByRole("button", { name: /save draft/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /sign report/i })).toBeVisible();
  });

  test("Save Draft sends field values + vitals in the PUT body", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, draftReport());

    await gotoReportPanel(page);
    await expect(page.getByText("Chief Complaint")).toBeVisible();

    await page.locator("#f-11").fill("Lower back pain for 3 weeks");
    await page.locator("#v-height").fill("70");
    await page.locator("#v-weight").fill("180");

    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, '""', { method: "PUT" });
    const putRequest = page.waitForRequest(
      (req) => req.url().includes(`/api/v1/patient/reports/${REPORT_ID}`) && req.method() === "PUT",
    );
    await page.getByRole("button", { name: /save draft/i }).click();

    const body = (await putRequest).postDataJSON();
    expect(body.fieldValues).toEqual([{ reportFieldId: 11, text: "Lower back pain for 3 weeks" }]);
    expect(body.vitals.heightInches).toBe(70);
    expect(body.vitals.weightLbs).toBe(180);
    // BMI auto-computed from height+weight: 180/(70^2)*703 ≈ 25.8
    expect(body.vitals.bmi).toBeCloseTo(25.8, 1);
  });

  test("vitals section is hidden when the report type has no Clinical Exam category", async ({ page }) => {
    // SOAP-only template (like Daily Visit) — no Clinical Exam category, so no vitals.
    await mockJsonResponse(page, "**/api/v1/administration/report-fields**", [
      { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
      { id: 12, reportTypeId: 1, name: "Exam Findings", category: "Objective", displayOrder: 1, isActive: true },
    ]);
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, draftReport());

    await gotoReportPanel(page);
    await expect(page.getByText("Chief Complaint")).toBeVisible();

    await expect(page.getByText("Vitals")).toHaveCount(0);
    await expect(page.locator("#v-height")).toHaveCount(0);

    // Saving a vitals-less type always PUTs null vitals.
    await page.locator("#f-11").fill("Routine visit");
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, '""', { method: "PUT" });
    const putRequest = page.waitForRequest(
      (req) => req.url().includes(`/api/v1/patient/reports/${REPORT_ID}`) && req.method() === "PUT",
    );
    await page.getByRole("button", { name: /save draft/i }).click();

    const body = (await putRequest).postDataJSON();
    expect(body.vitals).toEqual({
      heightInches: null,
      weightLbs: null,
      bmi: null,
      systolic: null,
      diastolic: null,
      pulse: null,
      temperatureF: null,
    });
  });

  test("signed report renders read-only with signature + addendum composer", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, signedReport());

    await gotoReportPanel(page);

    await expect(page.getByText(/signed by/i)).toBeVisible();
    await expect(page.getByRole("img", { name: /^signature$/i })).toBeVisible();
    // Draft action bar is gone once signed.
    await expect(page.getByRole("button", { name: /save draft/i })).toHaveCount(0);
    // Field values render read-only.
    await expect(page.locator("#f-11")).toBeDisabled();

    // Addendum composer posts to /addendums.
    await mockJsonResponse(
      page,
      "**/api/v1/patient/reports/" + REPORT_ID + "/addendums",
      '"new-addendum-id"',
      { method: "POST" },
    );
    const postRequest = page.waitForRequest(
      (req) => req.url().includes(`/reports/${REPORT_ID}/addendums`) && req.method() === "POST",
    );
    await page.getByPlaceholder("Add an addendum…").fill("Patient improving.");
    await page.getByRole("button", { name: /add addendum/i }).click();

    const body = (await postRequest).postDataJSON();
    expect(body.text).toBe("Patient improving.");
  });

  test("create-new-macro from a field posts to the Administration macros API", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, draftReport());
    // Both the field-name and general macro lookups hit /macros — return empty.
    await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
    // The create dialog's "Useable by" picker lists users.
    await mockJsonResponse(page, "**/api/v1/identity/users/search**", paged([]));

    await gotoReportPanel(page);
    await expect(page.getByText("Chief Complaint")).toBeVisible();

    // Open the first field's Macros dialog, then swap it to the create form.
    await page.getByRole("button", { name: "Macro", exact: true }).first().click();
    await page.getByRole("button", { name: /create new macro/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /create new macro/i })).toBeVisible();
    await dialog.getByLabel("Macro Name").fill("Normal Exam");
    await dialog.getByLabel("Macro Text").fill("Patient in no acute distress.");

    // POST mock (method-filtered) sits over the GET list mock.
    await mockJsonResponse(page, "**/api/v1/administration/macros**", '"new-macro-id"', {
      method: "POST",
    });
    const postRequest = page.waitForRequest(
      (req) => req.url().includes("/api/v1/administration/macros") && req.method() === "POST",
    );
    await dialog.getByRole("button", { name: /save macro/i }).click();

    const body = (await postRequest).postDataJSON();
    expect(body.name).toBe("Normal Exam");
    expect(body.text).toBe("Patient in no acute distress.");
    // "available to all fields" unchecked → scoped to this field (id 11).
    expect(body.reportFieldId).toBe(11);
    // "allow everyone" checked by default → shared (null owner).
    expect(body.useableByUserId).toBeNull();
  });

  test("Import Dx Codes appends associated diagnoses on Clinical Impression", async ({ page }) => {
    // Report type 1 with a Clinical Impression field + one associated problem.
    await mockJsonResponse(page, "**/api/v1/administration/report-fields**", [
      { id: 13, reportTypeId: 1, name: "Clinical Impression", category: "Clinical Impression", displayOrder: 0, isActive: true },
    ]);
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, {
      ...draftReport(),
      associatedProblemIds: ["prob-1"],
    });
    await mockJsonResponse(
      page,
      "**/api/v1/patient/problems**",
      paged([
        {
          id: "prob-1",
          patientId: PATIENT_ID,
          incidentId: null,
          diagnosticId: 79,
          diagnosticCode: "M99.01",
          diagnosticDescription: "Segmental dysfunction, cervical",
          diagnosisDate: null,
          status: "Active",
          notes: null,
          isMedicalAlert: false,
          createdByName: null,
          createdAtUtc: "2026-01-01T00:00:00Z",
          updatedByName: null,
          updatedAtUtc: null,
        },
      ]),
    );

    await gotoReportPanel(page);
    await expect(page.getByText("Clinical Impression").first()).toBeVisible();

    await page.getByRole("button", { name: /import dx codes/i }).click();

    await expect(page.locator("#f-13")).toHaveValue(/M99\.01 - Segmental dysfunction, cervical/);
  });

  test("signed report exposes Request Review and posts the reviewer", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + REPORT_ID, signedReport());

    await gotoReportPanel(page);
    await expect(page.getByText("Peer Review")).toBeVisible();

    // Pick a reviewer from the provider combobox.
    await page.getByLabel("Reviewer", { exact: true }).click();
    await page.getByRole("menuitemradio", { name: /house/i }).click();

    await mockJsonResponse(
      page,
      "**/api/v1/patient/reports/" + REPORT_ID + "/request-review",
      '""',
      { method: "PUT" },
    );
    const putRequest = page.waitForRequest(
      (req) => req.url().includes(`/reports/${REPORT_ID}/request-review`) && req.method() === "PUT",
    );
    await page.getByRole("button", { name: /request review/i }).click();

    const body = (await putRequest).postDataJSON();
    expect(body.reviewerProviderId).toBe("prov-1");
  });
});
