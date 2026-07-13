// E2E coverage for the patient info dialog (opened from the patient chart's
// "Edit patient info" button) — including the Guardian section's isMinor
// gating and the Next of Kin → Relation Role Code coupling — and the
// not-found state. List-page coverage lives in tests/patient-charts/search.spec.ts
// (search) and the create-patient dialog tests.
//
// Gotcha: getPatientById (GET) and updatePatient (PUT) hit the identical
// URL `**/api/v1/patient/patients/{id}` — see tests/settings/profile.spec.ts
// for the same shape. Don't use captureRequest on that URL (it intercepts
// every method); instead let the beforeEach's unfiltered GET mock satisfy
// the initial load, register a `{ method: "PUT" }`-filtered mock for the
// save, and capture the body via page.waitForRequest.
//
// The editor now lives inside a dialog opened from the patient chart page
// (`/patient-charts/:id`) rather than a standalone `/patients/:id` route —
// every test below navigates to the chart first, then opens the dialog via
// the "Edit patient info" button. That means each `beforeEach` also needs
// the chart's own supporting mocks (incidents, departments, incident-types,
// clinics) in addition to the administration lookups the section-edit
// dialogs query.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// ─── Administration lookup fixtures ──────────────────────────────────────

const RACE_LOOKUP = [
  { id: 1, name: "White", isActive: true },
  { id: 2, name: "Black or African American", isActive: true },
];
const ETHNICITY_LOOKUP = [
  { id: 1, name: "Hispanic or Latino", isActive: true },
  { id: 2, name: "Not Hispanic or Latino", isActive: true },
];
const LANGUAGE_LOOKUP = [
  { id: 1, name: "English", isActive: true },
  { id: 2, name: "Spanish", isActive: true },
];
const SMOKING_STATUS_LOOKUP = [
  { id: 1, name: "Never smoker", isActive: true },
  { id: 2, name: "Former smoker", isActive: true },
];
const CONTACT_METHOD_LOOKUP = [
  { id: 1, name: "Phone", isActive: true },
  { id: 2, name: "Email", isActive: true },
];
const REFERRAL_TYPE_LOOKUP = [
  { id: 1, name: "Physician referral", isActive: true },
  { id: 2, name: "Self-referral", isActive: true },
];

async function mockAdministrationLookups(page: Parameters<typeof mockJsonResponse>[0]) {
  await mockJsonResponse(page, "**/api/v1/administration/races**", RACE_LOOKUP);
  await mockJsonResponse(page, "**/api/v1/administration/ethnicities**", ETHNICITY_LOOKUP);
  await mockJsonResponse(page, "**/api/v1/administration/languages**", LANGUAGE_LOOKUP);
  await mockJsonResponse(page, "**/api/v1/administration/smoking-statuses**", SMOKING_STATUS_LOOKUP);
  await mockJsonResponse(page, "**/api/v1/administration/preferred-contact-methods**", CONTACT_METHOD_LOOKUP);
  await mockJsonResponse(page, "**/api/v1/administration/referral-types**", REFERRAL_TYPE_LOOKUP);
}

/**
 * The patient chart page's own supporting queries — none of these are
 * about patients per se, but the chart won't render without them (and an
 * unmocked `administration/clinics` leaks to a real dev backend on
 * localhost:7030 via useClinicTimeZones(), which can log the test session
 * out mid-test). Mirrors mockChartLookups in tests/patient-charts/problems.spec.ts.
 */
async function mockChartSupportingLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
}

/**
 * Navigate to the patient's chart, open the info dialog via the "Edit
 * patient info" button, and return the dialog locator (scoped by the
 * Demographics section heading, which only the info dialog contains).
 *
 * Generous timeout on the button's visibility (mirrors the "medical-alert
 * banner" test in tests/patient-charts/problems.spec.ts): this is the
 * chart's first paint after goto, which under heavy local test parallelism
 * (many concurrent Vite/Chromium workers cold-compiling the same large
 * lazy chunk) can take longer than the default 10s action timeout.
 */
async function openInfoDialog(page: Page, id: string) {
  await page.goto(`/patient-charts/${id}`);
  const editButton = page.getByRole("button", { name: /edit patient info/i });
  await expect(editButton).toBeVisible({ timeout: 20_000 });
  await editButton.click();
  return page.getByRole("dialog").filter({ hasText: "Demographics" });
}

// ─── Fixtures ────────────────────────────────────────────────────────────

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
  referralTypeId: null,
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
  referralTypeId: null,
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

// ─── Detail ──────────────────────────────────────────────────────────────

test.describe("patient info dialog — detail", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockAdministrationLookups(page);
    await mockChartSupportingLookups(page);
  });

  test("loads an adult patient: name, next of kin, no Guardian section", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, PATIENT_ADULT);

    const dialog = await openInfoDialog(page, PATIENT_ADULT_ID);
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: "Alice Q Vance" })).toBeVisible();

    // Scoped from `page` (not the filtered `dialog` locator) — nesting a
    // `.filter()`-derived locator as the base of a `has:`-based `.locator()`
    // silently resolves to nothing. The section is unique on the page while
    // the dialog is open, so this is unambiguous.
    const kinSection = page.locator("section", {
      has: page.getByRole("heading", { name: "Next of kin" }),
    });
    await expect(kinSection.getByText("Spouse")).toBeVisible();
    await expect(kinSection.getByText("SPS")).toBeVisible();

    await expect(dialog.getByRole("heading", { name: "Guardian" })).toHaveCount(0);
  });

  test("loads a minor patient: shows the Guardian section", async ({ page }) => {
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_MINOR_ID}`, PATIENT_MINOR);

    const dialog = await openInfoDialog(page, PATIENT_MINOR_ID);
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: "Casey Lee" })).toBeVisible();
    // exact: true — "Minor" is a substring of the Guardian section's own
    // description ("...recorded as a minor."), which would otherwise match too.
    await expect(dialog.getByText("Minor", { exact: true })).toBeVisible();

    const guardianSection = page.locator("section", {
      has: page.getByRole("heading", { name: "Guardian" }),
    });
    await expect(guardianSection).toBeVisible();
    await expect(guardianSection.getByText("Dana Lee")).toBeVisible();
  });

  test("shows the not-found panel on the chart card when the patient lookup returns null", async ({
    page,
  }) => {
    // The chart card and the info dialog share the same ["patients", id]
    // GET, so there's no clean way to give the chart a valid patient while
    // making only the dialog's lookup 404 — instead assert the chart
    // card's own not-found path (same "Patient not found." text the old
    // page's not-found panel asserted, just rendered by the card instead
    // of a full page). With no patient, the "Edit patient info" button
    // never renders, so there's nothing to open a dialog on.
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, null);

    await page.goto(`/patient-charts/${PATIENT_ADULT_ID}`);

    // Generous timeout — see openInfoDialog's comment above.
    await expect(page.getByText("Patient not found.")).toBeVisible({ timeout: 20_000 });
    await expect(page.getByRole("button", { name: /edit patient info/i })).toHaveCount(0);
  });
});

// ─── Next of kin — Relation / Relation Role Code coupling ─────────────────

test.describe("patient info dialog — next of kin relation/role-code coupling", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockAdministrationLookups(page);
    await mockChartSupportingLookups(page);
    // Unfiltered: satisfies the initial GET. The PUT-specific mock the test
    // registers later wins for the save and falls back to this one for GET.
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, PATIENT_NO_KIN);
  });

  test("selecting a relation sets the role code and saves both together", async ({ page }) => {
    const infoDialog = await openInfoDialog(page, PATIENT_ADULT_ID);
    await expect(infoDialog.getByRole("heading", { name: "Alice Q Vance" })).toBeVisible();

    const kinSection = page.locator("section", {
      has: page.getByRole("heading", { name: "Next of kin" }),
    });
    await kinSection.getByRole("button", { name: /edit/i }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Edit next of kin" });
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

// ─── Edit sections — dialog pre-fill + full-replace merge ────────────────

test.describe("patient info dialog — edit section dialogs", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockAdministrationLookups(page);
    await mockChartSupportingLookups(page);
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ADULT_ID}`, PATIENT_ADULT);
  });

  test("Demographics dialog opens pre-filled with current values", async ({ page }) => {
    const infoDialog = await openInfoDialog(page, PATIENT_ADULT_ID);
    await expect(infoDialog.getByRole("heading", { name: "Alice Q Vance" })).toBeVisible();

    const section = page.locator("section", {
      has: page.getByRole("heading", { name: "Demographics" }),
    });
    await section.getByRole("button", { name: /edit/i }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Edit demographics" });
    await expect(dialog.getByRole("heading", { name: /edit demographics/i })).toBeVisible();
    await expect(dialog.getByLabel("First name")).toHaveValue("Alice");
    await expect(dialog.getByLabel("Last name")).toHaveValue("Vance");
  });

  test("Demographics dialog save sends full-replace payload preserving untouched sections", async ({
    page,
  }) => {
    const infoDialog = await openInfoDialog(page, PATIENT_ADULT_ID);
    await expect(infoDialog.getByRole("heading", { name: "Alice Q Vance" })).toBeVisible();

    const section = page.locator("section", {
      has: page.getByRole("heading", { name: "Demographics" }),
    });
    await section.getByRole("button", { name: /edit/i }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Edit demographics" });
    // Change only first name.
    await dialog.getByLabel("First name").fill("Alicia");

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
    // Changed field.
    expect(body.firstName).toBe("Alicia");
    // Untouched contact field still present (full-replace merge).
    expect(body.address1).toBe("123 Main St");
    // Untouched next-of-kin still present.
    expect(body.nextOfKinRelation).toBe("Spouse");
  });

  test("Contact dialog opens pre-filled with current address", async ({ page }) => {
    const infoDialog = await openInfoDialog(page, PATIENT_ADULT_ID);
    await expect(infoDialog.getByRole("heading", { name: "Alice Q Vance" })).toBeVisible();

    const section = page.locator("section", {
      has: page.getByRole("heading", { name: "Contact" }),
    });
    await section.getByRole("button", { name: /edit/i }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Edit contact info" });
    await expect(dialog.getByRole("heading", { name: /edit contact/i })).toBeVisible();
    await expect(dialog.getByLabel("Address line 1")).toHaveValue("123 Main St");
    await expect(dialog.getByLabel("City")).toHaveValue("Springfield");
  });

  test("Contact dialog save preserves demographics in the PUT body", async ({ page }) => {
    const infoDialog = await openInfoDialog(page, PATIENT_ADULT_ID);
    await expect(infoDialog.getByRole("heading", { name: "Alice Q Vance" })).toBeVisible();

    const section = page.locator("section", {
      has: page.getByRole("heading", { name: "Contact" }),
    });
    await section.getByRole("button", { name: /edit/i }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Edit contact info" });
    await dialog.getByLabel("City").fill("Shelbyville");

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
    // Changed field.
    expect(body.city).toBe("Shelbyville");
    // Demographics preserved in the full-replace payload.
    expect(body.firstName).toBe("Alice");
    expect(body.lastName).toBe("Vance");
  });
});
