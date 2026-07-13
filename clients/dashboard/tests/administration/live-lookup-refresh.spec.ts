// E2E coverage for Part B: editing a lookup record inside the global
// Administration dialog refreshes the same data where it's rendered
// elsewhere — here, the chart's Department filter combobox — without a
// manual reload. Exercises the departments invalidation fix
// (["administration.departmentOptions"]) through the real UI.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.Incidents.View",
  "Permissions.Administration.Departments.View",
  "Permissions.Administration.Departments.Update",
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
  referralTypeId: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const DEPT = {
  id: "dept-1",
  name: "Physio",
  displayOrder: 0,
  isActive: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: null,
};

async function mockChartLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
}

test.describe("live lookup refresh", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChartLookups(page);
    await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([DEPT]));
  });

  test("renaming a department in the admin dialog updates the chart's filter options live", async ({
    page,
  }) => {
    await seedPatientWorkspace(page, [{ patientId: PATIENT_ID, patientLabel: "Alice Q Vance" }]);

    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Baseline: the Department filter combobox offers "Physio".
    await page.locator("#filter-dept").click();
    await expect(page.getByRole("menuitemradio", { name: "Physio" })).toBeVisible();
    await page.keyboard.press("Escape");

    // Open the Administration dialog over the chart, drill into Departments.
    await page.getByRole("button", { name: "Clinic Setup" }).click();
    await page.getByRole("button", { name: "Administration", exact: true }).click();
    const adminDialog = page.getByRole("dialog");
    await adminDialog.getByRole("button", { name: "Departments" }).click();
    // Anchor on the desktop row's edit button — the mobile card renders a
    // hidden duplicate of the name text at desktop viewport.
    await expect(adminDialog.getByRole("button", { name: "Edit Physio" })).toBeVisible();

    // Re-register the GET mock FIRST (Playwright matches most-recent-first)
    // so the invalidation-triggered refetch sees the renamed department,
    // then register the method-filtered PUT mock on top.
    await mockJsonResponse(
      page,
      "**/api/v1/administration/departments**",
      paged([{ ...DEPT, name: "Physiotherapy", updatedAtUtc: "2026-07-10T00:00:00Z" }]),
    );
    // String bodies are sent raw — JSON-encode the returned id so the
    // client's res.json() succeeds.
    await mockJsonResponse(page, "**/api/v1/administration/departments/" + DEPT.id, JSON.stringify(DEPT.id), {
      method: "PUT",
    });

    // Edit "Physio" → "Physiotherapy" (row edit button → editor dialog).
    await adminDialog.getByRole("button", { name: "Edit Physio" }).click();
    const editDialog = page.getByRole("dialog"); // top-most modal; admin dialog is aria-hidden behind it
    await editDialog.locator("#dept-name").fill("Physiotherapy");
    await editDialog.getByRole("button", { name: "Save changes" }).click();

    // Close the Administration dialog (its content refreshed too).
    await expect(
      page.getByRole("dialog").getByRole("button", { name: "Edit Physiotherapy" }),
    ).toBeVisible();
    await page.getByRole("dialog").getByRole("button", { name: "Close" }).click();
    await expect(page.getByRole("dialog")).toHaveCount(0);

    // The chart's Department filter now offers the renamed value — no reload.
    await page.locator("#filter-dept").click();
    await expect(page.getByRole("menuitemradio", { name: "Physiotherapy" })).toBeVisible();
    await expect(page.getByRole("menuitemradio", { name: "Physio", exact: true })).toHaveCount(0);
  });
});
