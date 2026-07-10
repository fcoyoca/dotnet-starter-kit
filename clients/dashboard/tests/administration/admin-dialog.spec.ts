// E2E coverage for Part A: Administration as a globally-triggered dialog.
// The key interaction: opening Administration from the sidebar while a
// patient chart is open does NOT navigate — the chart stays mounted
// underneath, untouched. Deep links open the dialog over Overview.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.Incidents.View",
  "Permissions.Administration.Clinics.View",
  "Permissions.Administration.Providers.View",
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
  },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const CLINIC = {
  id: "clinic-1",
  code: "MAIN",
  name: "Main Clinic",
  address1: "123 Main St",
  address2: null,
  city: "Springfield",
  state: "IL",
  zip: "62704",
  phone: null,
  timeZoneId: "UTC",
  isActive: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: null,
};

async function mockChartLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

async function mockOverviewLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/billing/usage**", []);
  await mockJsonResponse(page, "**/api/v1/billing/subscriptions/me**", { plan: "Scale", status: "Active" });
  await mockJsonResponse(page, "**/api/v1/audits**", paged([]));
}

test.describe("administration global dialog", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([CLINIC]));
  });

  test("sidebar button opens the dialog over an open chart without navigating", async ({ page }) => {
    await seedPatientWorkspace(page, [{ patientId: PATIENT_ID, patientLabel: "Alice Q Vance" }]);
    await mockChartLookups(page);

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await expect(page.getByText("Alice Q Vance").first()).toBeVisible();

    // The Administration entry lives inside the Clinic Setup accordion —
    // expand it first (closed accordion panels are aria-hidden).
    await page.getByRole("button", { name: "Clinic Setup" }).click();
    await page.getByRole("button", { name: "Administration", exact: true }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Administration", level: 1 })).toBeVisible();
    // No navigation happened — the chart URL is untouched.
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${PATIENT_ID}$`));

    // Hub → section → back-to-hub, all inside the ONE dialog.
    await dialog.getByRole("button", { name: "Clinics" }).click();
    await expect(dialog.getByRole("heading", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByText("Main Clinic").last()).toBeVisible();
    await dialog.getByRole("button", { name: "Administration", exact: true }).click();
    await expect(dialog.getByRole("button", { name: "Clinics" })).toBeVisible();

    // Close — the chart is exactly where it was.
    await dialog.getByRole("button", { name: "Close" }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);
    await expect(page.getByText("Alice Q Vance").first()).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/patient-charts/${PATIENT_ID}$`));
  });

  test("a deep link opens the section dialog and lands on Overview", async ({ page }) => {
    await mockOverviewLookups(page);

    await page.goto("/administration/clinics");

    await expect(page).toHaveURL("/");
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByText("Main Clinic").last()).toBeVisible();
    // The back-to-hub affordance is present in section view.
    await expect(dialog.getByRole("button", { name: "Administration", exact: true })).toBeVisible();
  });

  test("hub cards are permission-gated", async ({ page }) => {
    await mockOverviewLookups(page);

    await page.goto("/administration");

    await expect(page).toHaveURL("/");
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("button", { name: "Clinics" })).toBeVisible();
    await expect(dialog.getByRole("button", { name: "Providers" })).toBeVisible();
    // No Departments.View grant — its card must not render.
    await expect(dialog.getByRole("button", { name: "Departments" })).toHaveCount(0);
  });
});
