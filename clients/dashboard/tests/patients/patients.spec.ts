// E2E coverage for the Patients pages: list + create dialog, detail page
// (including the Guardian section's isMinor gating and the Next of
// Kin → Relation Role Code coupling), and the not-found state.
//
// Gotcha: getPatientById (GET) and updatePatient (PUT) hit the identical
// URL `**/api/v1/patient/patients/{id}` — see tests/settings/profile.spec.ts
// for the same shape. Don't use captureRequest on that URL (it intercepts
// every method); instead let the beforeEach's unfiltered GET mock satisfy
// the initial load, register a `{ method: "PUT" }`-filtered mock for the
// save, and capture the body via page.waitForRequest.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// ─── Fixtures ────────────────────────────────────────────────────────────

const PATIENT_LIST_ALICE = {
  id: "00000000-0000-0000-0000-0000000a1111",
  patientCode: "P-10293",
  firstName: "Alice",
  lastName: "Vance",
  middleInitial: "Q",
  dateOfBirth: "1990-04-12",
  gender: "F",
  email: "alice.vance@example.com",
  phone: "555-0101",
  isActive: true,
  lastVisitDate: "2026-05-01",
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: null,
};

const PATIENT_ADULT_ID = "00000000-0000-0000-0000-0000000a1111";

// Adult, non-minor patient with a next-of-kin relation already on file —
// used for the detail-rendering and "Guardian hidden" assertions.
const PATIENT_ADULT = {
  id: PATIENT_ADULT_ID,
  patientCode: "P-10293",
  isActive: true,
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: "2026-05-01T08:00:00Z",
  demographics: {
    firstName: "Alice",
    lastName: "Vance",
    middleInitial: "Q",
    dateOfBirth: "1990-04-12",
    gender: "F",
    maritalStatus: "M",
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
    address1: "123 Main St",
    address2: null,
    city: "Springfield",
    state: "IL",
    zipCode: "62704",
    phone: "555-0101",
    phoneExtension: null,
    cellPhone: "555-0102",
    email: "alice.vance@example.com",
    preferredContactMethodId: null,
  },
  phi: { ssnMasked: "***-**-1234" },
  employment: null,
  guardian: null,
  nextOfKin: {
    firstName: "Bob",
    lastName: "Vance",
    phone: "555-0199",
    relation: "Spouse",
    relationRoleCode: "SPS",
  },
  insurance: null,
  hasNoKnownProblems: true,
  hasNoKnownMedications: false,
  hasNoKnownAllergies: true,
  receivesEmailReminders: true,
  lastVisitDate: "2026-05-01",
  nextVisitDate: null,
};

// Minor patient with a guardian on file — used for the "Guardian shown" case.
const PATIENT_MINOR_ID = "00000000-0000-0000-0000-0000000b2222";
const PATIENT_MINOR = {
  id: PATIENT_MINOR_ID,
  patientCode: "P-20011",
  isActive: true,
  createdAtUtc: "2026-02-15T09:00:00Z",
  updatedAtUtc: null,
  demographics: {
    firstName: "Casey",
    lastName: "Lee",
    middleInitial: null,
    dateOfBirth: "2015-09-01",
    gender: "UN",
    maritalStatus: null,
    isMinor: true,
    raceId: null,
    ethnicityId: null,
    languageId: null,
    smokingStatusId: null,
    smokingStartDate: null,
    smokingEndDate: null,
    medicalAlertNotes: null,
  },
  contact: {
    address1: null,
    address2: null,
    city: null,
    state: null,
    zipCode: null,
    phone: null,
    phoneExtension: null,
    cellPhone: null,
    email: null,
    preferredContactMethodId: null,
  },
  phi: { ssnMasked: null },
  employment: null,
  guardian: {
    firstName: "Dana",
    lastName: "Lee",
    middleInitial: null,
    dateOfBirth: "1985-03-20",
    gender: "F",
    maritalStatus: "M",
    address1: null,
    address2: null,
    city: null,
    state: null,
    zipCode: null,
    phone: "555-0250",
    cellPhone: null,
    employerName: null,
    employerAddress1: null,
    employerAddress2: null,
    employerCity: null,
    employerState: null,
    employerZipCode: null,
  },
  nextOfKin: null,
  insurance: null,
  hasNoKnownProblems: false,
  hasNoKnownMedications: false,
  hasNoKnownAllergies: false,
  receivesEmailReminders: false,
  lastVisitDate: null,
  nextVisitDate: null,
};

// Same patient as PATIENT_ADULT but with no next-of-kin yet, so the
// coupling test starts from a clean Combobox.
const PATIENT_NO_KIN = { ...PATIENT_ADULT, nextOfKin: null };

// ─── List ────────────────────────────────────────────────────────────────

test.describe("patients — list", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("renders the heading and a patient row from the mock data", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([PATIENT_LIST_ALICE]));

    await page.goto("/patients");

    await expect(page.getByRole("heading", { name: "Patients", level: 1 })).toBeVisible();
    // Mobile card renders first in the DOM, desktop row last.
    await expect(page.getByText("Alice Q Vance").last()).toBeVisible();
    await expect(page.getByText("P-10293").last()).toBeVisible();
  });

  test("shows the empty state when no patients match", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([]));

    await page.goto("/patients");

    await expect(page.getByRole("heading", { name: "No patients yet", level: 2 })).toBeVisible();
    await expect(page.getByText(/register the first patient to get started/i)).toBeVisible();
  });

  test("opens the Register a patient dialog with its key fields", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([PATIENT_LIST_ALICE]));

    await page.goto("/patients");
    await page.getByRole("button", { name: /new patient/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /register a patient/i })).toBeVisible();
    await expect(dialog.getByLabel("Patient code")).toBeVisible();
    await expect(dialog.getByLabel("First name")).toBeVisible();
    await expect(dialog.getByLabel("Last name")).toBeVisible();
    await expect(dialog.getByLabel("Gender")).toBeVisible();
    await expect(dialog.getByLabel("Marital status")).toBeVisible();
  });
});

// ─── Detail ──────────────────────────────────────────────────────────────

test.describe("patients/:patientId — detail", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("loads an adult patient: back link, name, next of kin, no Guardian section", async ({
    page,
  }) => {
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, PATIENT_ADULT);

    await page.goto(`/patients/${PATIENT_ADULT_ID}`);

    await expect(page.getByRole("heading", { name: "Alice Q Vance", level: 1 })).toBeVisible();
    await expect(page.getByRole("link", { name: /back to patients/i })).toBeVisible();

    const kinSection = page.locator("section", {
      has: page.getByRole("heading", { name: "Next of kin" }),
    });
    await expect(kinSection.getByText("Spouse")).toBeVisible();
    await expect(kinSection.getByText("SPS")).toBeVisible();

    await expect(page.getByRole("heading", { name: "Guardian" })).toHaveCount(0);
  });

  test("loads a minor patient: shows the Guardian section", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_MINOR_ID}`, PATIENT_MINOR);

    await page.goto(`/patients/${PATIENT_MINOR_ID}`);

    await expect(page.getByRole("heading", { name: "Casey Lee", level: 1 })).toBeVisible();
    // exact: true — "Minor" is a substring of the Guardian section's own
    // description ("...recorded as a minor."), which would otherwise match too.
    await expect(page.getByText("Minor", { exact: true })).toBeVisible();

    const guardianSection = page.locator("section", {
      has: page.getByRole("heading", { name: "Guardian" }),
    });
    await expect(guardianSection).toBeVisible();
    await expect(guardianSection.getByText("Dana Lee")).toBeVisible();
  });

  test("shows the not-found panel when the patient lookup returns null", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, null);

    await page.goto(`/patients/${PATIENT_ADULT_ID}`);

    await expect(page.getByRole("heading", { name: /patient not found/i })).toBeVisible();
  });
});

// ─── Next of kin — Relation / Relation Role Code coupling ─────────────────

test.describe("patients/:patientId — next of kin relation/role-code coupling", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    // Unfiltered: satisfies the initial GET. The PUT-specific mock the test
    // registers later wins for the save and falls back to this one for GET.
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, PATIENT_NO_KIN);
  });

  test("selecting a relation sets the role code and saves both together", async ({ page }) => {
    await page.goto(`/patients/${PATIENT_ADULT_ID}`);
    await expect(page.getByRole("heading", { name: "Alice Q Vance", level: 1 })).toBeVisible();

    const kinSection = page.locator("section", {
      has: page.getByRole("heading", { name: "Next of kin" }),
    });
    await kinSection.getByRole("button", { name: /edit/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /edit next of kin/i })).toBeVisible();

    // Relation role code starts empty and read-only.
    const roleCode = dialog.getByLabel("Relation role code");
    await expect(roleCode).toHaveValue("");
    await expect(roleCode).toBeDisabled();

    await dialog.getByLabel("Relation", { exact: true }).click();
    await page.getByRole("menuitemradio", { name: "Spouse", exact: true }).click();

    // Selecting the relation derives the role code automatically.
    await expect(roleCode).toHaveValue("SPS");

    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, '""', {
      method: "PUT",
    });
    const putRequest = page.waitForRequest(
      (req) =>
        req.url().includes(`/api/v1/patient/patients/${PATIENT_ADULT_ID}`) &&
        req.method() === "PUT",
    );
    await dialog.getByRole("button", { name: /save changes/i }).click();

    const body = (await putRequest).postDataJSON();
    expect(body.nextOfKinRelation).toBe("Spouse");
    expect(body.nextOfKinRelationRoleCode).toBe("SPS");
  });
});
