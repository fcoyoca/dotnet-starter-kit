// E2E coverage for the Report Default Text admin screen.
//
// Each report field can carry boilerplate that a NEW report starts it with
// (legacy BackChart's rfDefaultText, edited on its own admin screen). The
// backend stamps it on at report-creation time; this screen is where it's set.
// Without it the column exists but nothing can ever fill it.

import { expect, test, type Page } from "@playwright/test";
import { captureRequest, mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks } from "../helpers/shell-mocks";

const PERMS = [
  "Permissions.Administration.ReportTemplates.View",
  "Permissions.Administration.ReportTemplates.Update",
];

const REPORT_TYPES = [
  { id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true },
  { id: 2, name: "Progress Note", displayOrder: 1, isActive: true },
];

const FIELDS_TYPE_1 = [
  {
    id: 11,
    reportTypeId: 1,
    name: "Chief Complaint",
    category: "Subjective",
    displayOrder: 0,
    isActive: true,
    defaultText: "Patient presents with ",
  },
  {
    id: 12,
    reportTypeId: 1,
    name: "Objective",
    category: "Objective",
    displayOrder: 1,
    isActive: true,
    defaultText: null,
  },
];

const FIELDS_TYPE_2 = [
  {
    id: 21,
    reportTypeId: 2,
    name: "Progress",
    category: "Subjective",
    displayOrder: 0,
    isActive: true,
    defaultText: null,
  },
];

async function mockAdmin(page: Page) {
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=1**", FIELDS_TYPE_1);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=2**", FIELDS_TYPE_2);
}

test.describe("report default text", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockAdmin(page);
  });

  test("lists every report type's fields and flags the ones that carry boilerplate", async ({ page }) => {
    await page.goto("/administration/report-default-text");

    await expect(page.getByText("Initial Evaluation")).toBeVisible();
    await expect(page.getByText("Progress Note")).toBeVisible();

    const fields = page.getByTestId("default-text-field");
    await expect(fields).toHaveCount(3);
    // Only Chief Complaint has default text, so only it is marked.
    await expect(fields.filter({ hasText: "Chief Complaint" })).toContainText("Set");
    await expect(fields.filter({ hasText: "Objective" })).not.toContainText("Set");
  });

  test("selecting a field loads its current default text", async ({ page }) => {
    await page.goto("/administration/report-default-text");

    await page.getByTestId("default-text-field").filter({ hasText: "Chief Complaint" }).click();
    await expect(page.getByTestId("default-text-editor")).toHaveValue("Patient presents with ");

    // Switching fields swaps the editor to that field's text, not the last one's.
    await page.getByTestId("default-text-field").filter({ hasText: "Objective" }).click();
    await expect(page.getByTestId("default-text-editor")).toHaveValue("");
  });

  test("saving PUTs the field back with its new default text", async ({ page }) => {
    await page.goto("/administration/report-default-text");
    await page.getByTestId("default-text-field").filter({ hasText: "Objective" }).click();

    await page.getByTestId("default-text-editor").fill("Cervical ROM within normal limits.");

    await mockJsonResponse(page, "**/api/v1/administration/report-fields/12", "", { method: "PUT" });
    const put = captureRequest(page, "**/api/v1/administration/report-fields/12");
    await page.getByRole("button", { name: "Save" }).click();

    const { body } = await put.value();
    // The rest of the field round-trips untouched — this screen owns only the text.
    expect(body).toMatchObject({
      id: 12,
      name: "Objective",
      category: "Objective",
      displayOrder: 1,
      isActive: true,
      defaultText: "Cervical ROM within normal limits.",
    });
  });

  test("clearing the text sends null, not an empty string", async ({ page }) => {
    await page.goto("/administration/report-default-text");
    await page.getByTestId("default-text-field").filter({ hasText: "Chief Complaint" }).click();

    await page.getByTestId("default-text-editor").fill("");

    await mockJsonResponse(page, "**/api/v1/administration/report-fields/11", "", { method: "PUT" });
    const put = captureRequest(page, "**/api/v1/administration/report-fields/11");
    await page.getByRole("button", { name: "Save" }).click();

    // Blank must round-trip as "no boilerplate" so the create-time prefill skips
    // the field entirely rather than stamping an empty value onto every report.
    const { body } = await put.value();
    expect((body as { defaultText: string | null }).defaultText).toBeNull();
  });
});
