// E2E coverage for the Patient Problem List (route-mocked): the problem panel on
// the chart page (rows + medical-alert banner) and the Add Problem dialog
// (DX search → pick → POST). Mirrors the patterns in patients.spec.ts.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PROBLEM_PERMS = [
  "Permissions.Patient.Problems.View",
  "Permissions.Patient.Problems.Create",
  "Permissions.Patient.Problems.Update",
  "Permissions.Patient.Problems.Delete",
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

const PROBLEM_ALERT = {
  id: "00000000-0000-0000-0000-0000000e5555",
  patientId: PATIENT_ID,
  incidentId: null,
  diagnosticId: 79,
  diagnosticCode: "M99.01",
  diagnosticDescription: "Segmental dysfunction of cervical region",
  diagnosisDate: "2026-01-18T00:00:00Z",
  status: "Active",
  notes: null,
  isMedicalAlert: true,
  createdByName: "Alice Nguyen",
  createdAtUtc: "2026-01-18T08:00:00Z",
  updatedByName: null,
  updatedAtUtc: null,
};

const ICD_DX = paged([
  {
    id: 1,
    code: "M54.5",
    description: "Low back pain",
    longDescription: "Low back pain",
    codeSourceId: 7,
    codeSourceName: "ICD-10-CM",
    isChiropractic: false,
    isBillable: true,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

async function mockChartLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/diagnostics**", ICD_DX);
}

test.describe("patient problem list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PROBLEM_PERMS);
    await mockChartLookups(page);
  });

  test("medical-alert banner + Problem List button open the problem list", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([PROBLEM_ALERT]));

    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Medical alerts surface on the chart page itself.
    await expect(page.getByText("Medical Alerts")).toBeVisible();
    await expect(page.getByText("M99.01").first()).toBeVisible();

    // Patient Info card links out to the demographics edit page.
    await expect(page.getByRole("link", { name: /edit patient info/i })).toHaveAttribute(
      "href",
      new RegExp(`/patients/${PATIENT_ID}$`),
    );

    // The list lives behind a Problem List button (BackChart chart-card pattern).
    await page.getByRole("button", { name: "Problem List" }).click();
    const dialog = page.getByRole("dialog").filter({ hasText: "Show resolved" });
    await expect(dialog.getByRole("heading", { name: "Problem List" })).toBeVisible();
    await expect(dialog.getByText("M99.01")).toBeVisible();
  });

  test("Add Problem dialog searches a DX code and posts the problem", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await page.getByRole("button", { name: "Problem List" }).click();

    const listDialog = page.getByRole("dialog").filter({ hasText: "Show resolved" });
    await listDialog.getByRole("button", { name: "Add Problem" }).click();

    const addDialog = page.getByRole("dialog").filter({ hasText: "Flag as medical alert" });
    await expect(addDialog.getByRole("heading", { name: "Add Problem" })).toBeVisible();

    await addDialog.getByPlaceholder(/search by code or description/i).fill("M54");
    await addDialog.getByRole("button", { name: /M54\.5/ }).click();
    await expect(addDialog.getByText("M54.5")).toBeVisible();

    await mockJsonResponse(page, "**/api/v1/patient/problems", '"new-problem-id"', { method: "POST" });
    const postRequest = page.waitForRequest(
      (req) => req.url().endsWith("/api/v1/patient/problems") && req.method() === "POST",
    );
    await addDialog.getByRole("button", { name: "Add Problem" }).click();

    const body = (await postRequest).postDataJSON();
    expect(body.patientId).toBe(PATIENT_ID);
    expect(body.diagnosticId).toBe(1);
    expect(body.diagnosticCode).toBe("M54.5");
    expect(body.status).toBe("Active");
  });
});
