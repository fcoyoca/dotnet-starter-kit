// E2E for the chart page's upper-right "Search for Patient" / "Add New Patient"
// actions (BackChart parity): both pull another patient into the chart
// workspace without leaving the chart — search picks an existing patient and
// opens it as a second tab; Add New Patient registers one from a dialog.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PERMS = [
  "Permissions.Patient.Patients.View",
  "Permissions.Patient.Patients.Create",
  "Permissions.Patient.Incidents.View",
];

const ALICE_ID = "00000000-0000-0000-0000-0000000a1111";
const BEN_ID = "00000000-0000-0000-0000-0000000a2222";

function detail(id: string, firstName: string, lastName: string, patientCode: string) {
  return {
    id,
    patientCode,
    isActive: true,
    demographics: {
      firstName,
      middleInitial: null,
      lastName,
      dateOfBirth: "1990-04-12",
      gender: "F",
    },
    referralTypeId: null,
    lastVisitDate: null,
    nextVisitDate: null,
  };
}

function listItem(id: string, firstName: string, lastName: string, patientCode: string) {
  return {
    id,
    patientCode,
    firstName,
    lastName,
    middleInitial: null,
    dateOfBirth: "1990-04-12",
    gender: "F",
    isActive: true,
    lastVisitDate: null,
    createdAtUtc: "2026-01-10T10:00:00Z",
    updatedAtUtc: null,
  };
}

async function mockChart(page: Page) {
  // LIFO: the search glob also matches the detail URLs, so the detail mocks
  // must be registered AFTER it to win.
  await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([listItem(BEN_ID, "Ben", "Ortiz", "P-20456")]));
  await mockJsonResponse(page, `**/api/v1/patient/patients/${ALICE_ID}`, detail(ALICE_ID, "Alice", "Vance", "P-10293"));
  await mockJsonResponse(page, `**/api/v1/patient/patients/${BEN_ID}`, detail(BEN_ID, "Ben", "Ortiz", "P-20456"));
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
}

test.describe("patient chart — patient switcher", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
  });

  test("searching for a patient opens their chart as a second tab", async ({ page }) => {
    await page.goto(`/patient-charts/${ALICE_ID}`);
    await expect(page.getByRole("tab", { name: /Alice Vance/ })).toBeVisible();

    await page.getByRole("button", { name: "Search for Patient" }).click();
    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: "Search for Patient" })).toBeVisible();

    await dialog.getByLabel("Search patients").fill("Ortiz");
    await dialog.getByRole("button", { name: /open chart for ben ortiz/i }).click();

    await expect(page).toHaveURL(new RegExp(`/patient-charts/${BEN_ID}$`));
    // Both patients stay open in the workspace tab strip.
    await expect(page.getByRole("tab", { name: /Alice Vance/ })).toBeVisible();
    await expect(page.getByRole("tab", { name: /Ben Ortiz/ })).toHaveAttribute(
      "aria-selected",
      "true",
    );
  });

  test("Add New Patient opens the register dialog from the chart", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients/next-code-preview", {
      preview: "P-100007",
    });
    await page.goto(`/patient-charts/${ALICE_ID}`);

    await page.getByRole("button", { name: "Add New Patient" }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /register a patient/i })).toBeVisible();
    await expect(dialog.getByLabel("Patient code")).toHaveValue("P-100007");
  });
});
