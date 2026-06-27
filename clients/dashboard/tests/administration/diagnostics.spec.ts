// E2E coverage for the Diagnostic Details admin page (route-mocked): the ICD
// catalog list + source column, and the create dialog (code + ICD-10 source → POST).

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const DIAG_PERMS = [
  "Permissions.Administration.Diagnostics.View",
  "Permissions.Administration.Diagnostics.Create",
  "Permissions.Administration.Diagnostics.Update",
  "Permissions.Administration.Diagnostics.Delete",
];

const CODE_SOURCES = [
  { id: 7, name: "ICD-10-CM", isActive: true },
  { id: 1, name: "CPT", isActive: true },
];

const DIAG = {
  id: 1,
  code: "A00",
  description: "Cholera",
  longDescription: "Cholera",
  codeSourceId: 7,
  codeSourceName: "ICD-10-CM",
  isChiropractic: false,
  isBillable: false,
  isActive: true,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: null,
};

async function mockLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/administration/code-sources**", CODE_SOURCES);
}

test.describe("administration — diagnostic details", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", DIAG_PERMS);
    await mockLookups(page);
  });

  test("renders the ICD catalog list with code + source", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/administration/diagnostics**", paged([DIAG]));

    await page.goto("/administration/diagnostics");

    await expect(page.getByRole("heading", { name: "Diagnostic Details", level: 1 })).toBeVisible();
    await expect(page.getByText("A00").last()).toBeVisible();
    await expect(page.getByText("Cholera").last()).toBeVisible();
    await expect(page.getByText("ICD-10-CM").last()).toBeVisible();
  });

  test("create dialog posts a new diagnostic with the ICD-10 source", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/administration/diagnostics**", paged([DIAG]));

    await page.goto("/administration/diagnostics");
    await page.getByRole("button", { name: /new diagnostic/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /add a diagnostic/i })).toBeVisible();
    await dialog.locator("#dx-code").fill("M54.5");
    await dialog.locator("#dx-description").fill("Low back pain");

    await mockJsonResponse(page, "**/api/v1/administration/diagnostics", "123", { method: "POST" });
    const postRequest = page.waitForRequest(
      (req) => req.url().endsWith("/api/v1/administration/diagnostics") && req.method() === "POST",
    );
    await dialog.getByRole("button", { name: "Add diagnostic" }).click();

    const body = (await postRequest).postDataJSON();
    expect(body.code).toBe("M54.5");
    expect(body.description).toBe("Low back pain");
    expect(body.codeSourceId).toBe(7);
  });
});
