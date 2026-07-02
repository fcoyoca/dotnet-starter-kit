// E2E for the search-first Patient Chart page: prompt before search,
// results after a query, row → chart navigation, and + New → chart.
import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const ALICE = {
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

test.describe("patient chart — search-first", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("shows the prompt before any search and no patient rows", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await page.goto("/patient-charts");

    await expect(
      page.getByRole("heading", { name: "Patient Chart", level: 1 }),
    ).toBeVisible();
    await expect(page.getByText(/search for a patient to open their chart/i)).toBeVisible();
    // Default (no filter active) must NOT list patients.
    await expect(page.getByText("Alice Q Vance")).toHaveCount(0);
  });

  test("typing a search shows matching rows", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await page.goto("/patient-charts");

    await page.getByPlaceholder(/search by name or patient code/i).fill("Vance");
    await expect(page.getByText("Alice Q Vance").last()).toBeVisible();
    await expect(page.getByText("P-10293").last()).toBeVisible();
  });

  test("clicking a result navigates to the patient chart", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await page.goto("/patient-charts");

    await page.getByPlaceholder(/search by name or patient code/i).fill("Vance");
    await page.getByRole("link", { name: /open chart for alice q vance/i }).first().click();

    await expect(page).toHaveURL(new RegExp(`/patient-charts/${ALICE.id}$`));
  });

  test("opens the Register a patient dialog with its key fields", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await mockJsonResponse(page, "**/api/v1/patient/patients/next-code-preview", { preview: "P-100007" });

    await page.goto("/patient-charts");
    await page.getByRole("button", { name: /new patient/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /register a patient/i })).toBeVisible();
    const codeField = dialog.getByLabel("Patient code");
    await expect(codeField).toHaveValue("P-100007");
    await expect(codeField).toBeDisabled();
    await expect(dialog.getByLabel("First name")).toBeVisible();
    await expect(dialog.getByLabel("Last name")).toBeVisible();
    await expect(dialog.getByLabel("Gender")).toBeVisible();
    await expect(dialog.getByLabel("Marital status")).toBeVisible();
  });
});
