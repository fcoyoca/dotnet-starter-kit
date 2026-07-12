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
  demographics: {
    firstName: "Alice",
    middleInitial: "Q",
    lastName: "Vance",
    dateOfBirth: "1990-04-12",
    gender: "F",
    maritalStatus: null,
    isMinor: false,
    raceId: null,
    ethnicityId: null,
    languageId: null,
    smokingStatusId: null,
    smokingStartDate: null,
    smokingEndDate: null,
    medicalAlertNotes: null,
  },
  contact: {
    address1: null, address2: null, city: null, state: null, zipCode: null,
    phone: null, phoneExtension: null, cellPhone: null, email: null, preferredContactMethodId: null,
  },
  phi: { ssnMasked: null },
  employment: null,
  guardian: null,
  nextOfKin: null,
  insurance: null,
  hasNoKnownProblems: false,
  hasNoKnownMedications: false,
  hasNoKnownAllergies: false,
  receivesEmailReminders: false,
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: null,
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
  // useClinicTimeZones() (chart page) fetches this for the Last/Next visit
  // rows' timezone lookup — unmocked, it falls through to any real backend
  // sharing this dev machine and can 401 → log the test session out.
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
  // Administration lookups the Demographics/Contact edit dialogs query
  // (mirrors mockAdministrationLookups in tests/patients/patients.spec.ts).
  // These return flat arrays, not paged(...).
  await mockJsonResponse(page, "**/api/v1/administration/races**", []);
  await mockJsonResponse(page, "**/api/v1/administration/ethnicities**", []);
  await mockJsonResponse(page, "**/api/v1/administration/languages**", []);
  await mockJsonResponse(page, "**/api/v1/administration/smoking-statuses**", []);
  await mockJsonResponse(page, "**/api/v1/administration/preferred-contact-methods**", []);
  await mockJsonResponse(page, "**/api/v1/administration/referral-types**", []);
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

    // Medical alerts surface on the chart page itself. Generous timeout: this
    // is the chart's first paint after goto, which under heavy local test
    // parallelism (many concurrent Vite/Chromium workers) can take a few
    // seconds longer than the default 5s.
    await expect(page.getByText("Medical Alerts")).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText("M99.01").first()).toBeVisible();

    // Patient Info card exposes an Edit button (opens the info dialog).
    await expect(page.getByRole("button", { name: /edit patient info/i })).toBeVisible();

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

  test("editing demographics in the info dialog updates the chart card", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Card shows the current name. Scoped to the Patient Info card's name
    // <p> (unique "leading-tight" class) — the tab strip and, once the info
    // dialog is open, the Demographics dialog description both also contain
    // the patient's name as plain text, so a bare getByText is ambiguous.
    // Generous timeout for the same first-paint-under-load reason as the
    // "medical-alert banner" test above.
    const cardName = page.locator("p.leading-tight");
    await expect(cardName).toHaveText("Alice Q Vance", { timeout: 15_000 });

    // Open the info dialog, then the Demographics section dialog (nested).
    await page.getByRole("button", { name: /edit patient info/i }).click();
    const infoDialog = page.getByRole("dialog").filter({ hasText: "Demographics" });
    await infoDialog
      .locator("section", { has: page.getByRole("heading", { name: "Demographics" }) })
      .getByRole("button", { name: /edit/i })
      .click();

    const editDialog = page.getByRole("dialog").filter({ hasText: "Edit demographics" });
    await editDialog.getByLabel("First name").fill("Alicia");

    // After the PUT succeeds, the invalidated GET refetch must return the new name.
    const UPDATED = { ...PATIENT, demographics: { ...PATIENT.demographics, firstName: "Alicia" } };
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ID}`, '""', { method: "PUT" });
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ID}`, UPDATED);

    await editDialog.getByRole("button", { name: /save changes/i }).click();

    // Chart card reflects the change without a navigation/reload.
    await expect(cardName).toHaveText("Alicia Q Vance");
  });
});
