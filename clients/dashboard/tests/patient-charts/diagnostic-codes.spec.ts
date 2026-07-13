// E2E coverage for the Diagnostic Codes dialog + Plan-field affordance (route-mocked):
// report-editor Plan-field "Procedures Performed" button (per-field, not header) →
// Edit Dx Codes → DX Search → add code → confirm-No → Save PUT payload; dupe guard toast.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.SuperBills.View",
  "Permissions.Patient.SuperBills.Manage",
  "Permissions.Patient.Incidents.View",
  "Permissions.Patient.Incidents.Update",
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Reports.Update",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000b2222";
const REPORT_ID = "00000000-0000-0000-0000-0000000c3333";
const DX_ID = "00000000-0000-0000-0000-0000000d4444"; // existing custom dx on the incident
const CAT_ID = "00000000-0000-0000-0000-0000000e5555";
const ENSURED_ID = "00000000-0000-0000-0000-0000000f6666"; // guid the ensure endpoint mints
const GLOBAL_DX_ID = 500; // int id in the global ICD catalog
const PLAN_FIELD_ID = 24;

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
  dateOfLoss: "2026-01-10",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [DX_ID],
  createdAtUtc: "2026-01-10T08:00:00Z",
  updatedAtUtc: null,
};

const REPORT_DETAIL = {
  id: REPORT_ID,
  incidentId: INCIDENT_ID,
  patientId: PATIENT_ID,
  reportTypeId: 1,
  reportDate: "2026-01-12",
  version: 1,
  providerId: null,
  clinicId: null,
  appointmentId: null,
  isNoShow: false,
  vitals: {
    heightInches: null, weightLbs: null, bmi: null,
    systolic: null, diastolic: null, pulse: null, temperatureF: null,
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
  createdAtUtc: "2026-01-12T08:00:00Z",
  updatedAtUtc: null,
};

const REPORT_FIELDS = [
  { id: PLAN_FIELD_ID, reportTypeId: 1, name: "Plan", category: "Plan", displayOrder: 2, isActive: true },
  // A non-Plan field proves the affordance is per-field, not on every row.
  { id: 30, reportTypeId: 1, name: "Subjective", category: "General", displayOrder: 1, isActive: true },
];

const CUSTOM_DX = paged([
  {
    id: DX_ID,
    code: "M99.01",
    description: "Segmental dysfunction of cervical region",
    longDescription: null,
    isChiropractic: true,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

const GLOBAL_DX = paged([
  {
    id: GLOBAL_DX_ID,
    code: "M99.03",
    description: "Segmental dysfunction of lumbar region",
    longDescription: null,
    codeSourceId: 7,
    codeSourceName: "ICD-10-CM",
    isChiropractic: true,
    isBillable: true,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

async function mockReportEditorLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, `**/api/v1/patient/reports/${REPORT_ID}`, REPORT_DETAIL);
  await mockJsonResponse(page, `**/api/v1/patient/reports/${REPORT_ID}/procedures`, {
    id: null, reportId: REPORT_ID, isBilled: false, billedDateUtc: null, procedures: [],
  });
  await mockJsonResponse(page, "**/api/v1/administration/report-fields**", REPORT_FIELDS);
  await mockJsonResponse(page, "**/api/v1/administration/providers**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));

  // LIFO: the broad incidents glob also matches the detail URL, so the
  // detail-specific mock must be registered AFTER it (most recent wins).
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_ID}`, INCIDENT);

  // Same LIFO caveat: the custom-diagnostics list glob also matches /ensure.
  await mockJsonResponse(page, "**/api/v1/administration/custom-diagnostics**", CUSTOM_DX);
  // apiFetch expects a JSON string body for the ensured guid, and
  // mockJsonResponse sends raw strings as-is — hence the JSON.stringify.
  await mockJsonResponse(
    page,
    "**/api/v1/administration/custom-diagnostics/ensure",
    JSON.stringify(ENSURED_ID),
  );

  await mockJsonResponse(page, "**/api/v1/administration/diagnostic-categories**", paged([
    { id: CAT_ID, name: "Spine", isActive: true, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
  ]));
  await mockJsonResponse(page, "**/api/v1/administration/diagnostics**", GLOBAL_DX);

  // ProceduresPerformedDialog side queries (unused in these flows).
  await mockJsonResponse(page, "**/api/v1/administration/insurance-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/procedure-categories**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/procedure-codes**", paged([]));
}

/** Drive the report editor to an open Diagnostic Codes dialog (hydrated). */
async function openDiagnosticCodesDialog(page: Page) {
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

  const planButton = page.getByRole("button", { name: "Procedures Performed" });
  await expect(planButton).toHaveCount(1);
  await planButton.click();

  const procDialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
  await expect(procDialog).toBeVisible();
  await procDialog.getByRole("button", { name: "Edit Dx Codes" }).click();

  const dxDialog = page.getByRole("dialog").filter({ hasText: "Diagnostic Codes" });
  await expect(dxDialog).toBeVisible();
  // Hydrated when the incident's existing dx row is on screen.
  await expect(dxDialog.getByText("M99.01")).toBeVisible();
  return dxDialog;
}

/** Switch to DX Search mode, run a search, and add the mocked global code once. */
async function searchAndAdd(page: Page, dxDialog: ReturnType<Page["locator"]>) {
  await dxDialog.getByRole("button", { name: "DX Search" }).click();
  await dxDialog.getByPlaceholder("Code or description…").fill("M99.03");
  await dxDialog.getByRole("button", { name: "Search", exact: true }).click();
  await dxDialog.getByRole("button", { name: "Add M99.03" }).click();
}

test.describe("diagnostic codes dialog", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockReportEditorLookups(page);
  });

  test("Plan-field affordance → Edit Dx Codes → search, add, confirm-No, save PUT payload", async ({ page }) => {
    let putBody: unknown = null;
    // Registered after the incident mocks so it wins the LIFO match for /diagnostics.
    await page.route(`**/api/v1/patient/incidents/${INCIDENT_ID}/diagnostics`, async (route) => {
      putBody = route.request().postDataJSON();
      await route.fulfill({ status: 204, body: "" });
    });

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

    // Exactly one Procedures Performed button, and it lives in the Plan
    // field's block (not the page header, not the Subjective field).
    const planButton = page.getByRole("button", { name: "Procedures Performed" });
    await expect(planButton).toHaveCount(1);
    await expect(
      page.locator(`div:has(> textarea#f-${PLAN_FIELD_ID})`).getByRole("button", { name: "Procedures Performed" }),
    ).toBeVisible();

    await planButton.click();
    const procDialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
    await procDialog.getByRole("button", { name: "Edit Dx Codes" }).click();

    const dxDialog = page.getByRole("dialog").filter({ hasText: "Diagnostic Codes" });
    await expect(dxDialog.getByText("M99.01")).toBeVisible();

    await searchAndAdd(page, dxDialog);

    // The add-to-problems confirm must appear; answer No (no problem created).
    const confirm = page.getByRole("dialog").filter({ hasText: "Add Dx Code to Problems" });
    await expect(confirm).toBeVisible();
    await confirm.getByRole("button", { name: "No" }).click();
    await expect(confirm).toBeHidden();

    await dxDialog.getByRole("button", { name: "Save" }).click();

    await expect.poll(() => putBody).not.toBeNull();
    const body = putBody as { incidentId: string; diagnosticIds: string[] };
    expect(body.incidentId).toBe(INCIDENT_ID);
    // Existing custom dx passes through untouched; the new global code was
    // ensured into ENSURED_ID first.
    expect(body.diagnosticIds).toEqual([DX_ID, ENSURED_ID]);
  });

  test("adding the same code twice shows the dupe guard toast", async ({ page }) => {
    const dxDialog = await openDiagnosticCodesDialog(page);

    await searchAndAdd(page, dxDialog);
    const confirm = page.getByRole("dialog").filter({ hasText: "Add Dx Code to Problems" });
    await confirm.getByRole("button", { name: "No" }).click();
    await expect(confirm).toBeHidden();

    await dxDialog.getByRole("button", { name: "Add M99.03" }).click();

    await expect(page.getByText("Item already in the list.")).toBeVisible();
  });
});
