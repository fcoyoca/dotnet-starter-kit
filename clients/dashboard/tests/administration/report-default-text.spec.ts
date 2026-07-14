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

/** Report types are collapsed on arrival — open one to get at its fields. */
async function expandType(page: Page, name: string) {
  await page.getByTestId("default-text-group").filter({ hasText: name }).click();
}

test.describe("report default text", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockAdmin(page);
  });

  test("lists the report types collapsed, and flags fields that carry boilerplate once opened", async ({
    page,
  }) => {
    await page.goto("/administration/report-default-text");

    // The rail opens as just the types — no field is on screen until one is opened.
    await expect(page.getByTestId("default-text-group")).toHaveCount(2);
    const fields = page.getByTestId("default-text-field");
    await expect(fields.filter({ hasText: "Chief Complaint" })).toBeHidden();

    await expandType(page, "Initial Evaluation");
    await expandType(page, "Progress Note");

    await expect(fields).toHaveCount(3);
    // Only Chief Complaint has default text, so only it is marked.
    await expect(fields.filter({ hasText: "Chief Complaint" })).toContainText("Set");
    await expect(fields.filter({ hasText: "Objective" })).not.toContainText("Set");
  });

  test("selecting a field loads its current default text", async ({ page }) => {
    await page.goto("/administration/report-default-text");
    await expandType(page, "Initial Evaluation");

    await page.getByTestId("default-text-field").filter({ hasText: "Chief Complaint" }).click();
    await expect(page.getByTestId("default-text-editor")).toHaveValue("Patient presents with ");

    // Switching fields swaps the editor to that field's text, not the last one's.
    await page.getByTestId("default-text-field").filter({ hasText: "Objective" }).click();
    await expect(page.getByTestId("default-text-editor")).toHaveValue("");
  });

  test("saving PUTs the field back with its new default text", async ({ page }) => {
    await page.goto("/administration/report-default-text");
    await expandType(page, "Initial Evaluation");
    await page.getByTestId("default-text-field").filter({ hasText: "Objective" }).click();

    await page.getByTestId("default-text-editor").fill("Cervical ROM within normal limits.");

    await mockJsonResponse(page, "**/api/v1/administration/report-fields/12", "", { method: "PUT" });
    const put = captureRequest(page, "**/api/v1/administration/report-fields/12");
    // exact: the "Unsaved" badge on a dirty field makes its accessible name a
    // substring match for "Save" otherwise.
    await page.getByRole("button", { name: "Save", exact: true }).click();

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

  test("opening and closing a report type leaves the other types alone", async ({ page }) => {
    await page.goto("/administration/report-default-text");

    const fields = page.getByTestId("default-text-field");
    await expandType(page, "Initial Evaluation");
    await expandType(page, "Progress Note");
    await expect(fields.filter({ hasText: "Chief Complaint" })).toBeVisible();

    await expandType(page, "Initial Evaluation"); // toggles it back shut

    await expect(fields.filter({ hasText: "Chief Complaint" })).toBeHidden();
    // The other type stays open — the accordion is per group, not single-select.
    await expect(fields.filter({ hasText: "Progress" })).toBeVisible();
  });

  test("searching narrows the rail to matching fields", async ({ page }) => {
    await page.goto("/administration/report-default-text");

    await page.getByTestId("default-text-search").fill("chief");

    // A hit opens its group on its own — a match you can't see reads as no match.
    const fields = page.getByTestId("default-text-field");
    await expect(fields).toHaveCount(1);
    await expect(fields.first()).toContainText("Chief Complaint");
    await expect(fields.first()).toBeVisible();
  });

  test("keeps each field's unsaved edit when clicking through fields", async ({ page }) => {
    await page.goto("/administration/report-default-text");
    await expandType(page, "Initial Evaluation");

    const editor = page.getByTestId("default-text-editor");
    await page.getByTestId("default-text-field").filter({ hasText: "Objective" }).click();
    await editor.fill("Cervical ROM within normal limits.");

    // Leaving a field mid-edit must not throw the text away — the rail flags it as
    // unsaved and hands it back when you come back to it.
    await page.getByTestId("default-text-field").filter({ hasText: "Chief Complaint" }).click();
    await expect(editor).toHaveValue("Patient presents with ");
    await expect(
      page.getByTestId("default-text-field").filter({ hasText: "Objective" }),
    ).toContainText("Unsaved");

    await page.getByTestId("default-text-field").filter({ hasText: "Objective" }).click();
    await expect(editor).toHaveValue("Cervical ROM within normal limits.");

    // Discard puts the field back to its saved text.
    await page.getByRole("button", { name: "Discard" }).click();
    await expect(editor).toHaveValue("");
  });

  test("clearing the text sends null, not an empty string", async ({ page }) => {
    await page.goto("/administration/report-default-text");
    await expandType(page, "Initial Evaluation");
    await page.getByTestId("default-text-field").filter({ hasText: "Chief Complaint" }).click();

    await page.getByTestId("default-text-editor").fill("");

    await mockJsonResponse(page, "**/api/v1/administration/report-fields/11", "", { method: "PUT" });
    const put = captureRequest(page, "**/api/v1/administration/report-fields/11");
    // exact: the "Unsaved" badge on a dirty field makes its accessible name a
    // substring match for "Save" otherwise.
    await page.getByRole("button", { name: "Save", exact: true }).click();

    // Blank must round-trip as "no boilerplate" so the create-time prefill skips
    // the field entirely rather than stamping an empty value onto every report.
    const { body } = await put.value();
    expect((body as { defaultText: string | null }).defaultText).toBeNull();
  });
});
