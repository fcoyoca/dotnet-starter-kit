import { expect, type Locator, type Page } from "@playwright/test";

/**
 * The report editor opens read-only — the report is presented as a clinical
 * note first, and only turns into an editable form when the clinician clicks
 * "Edit" (mirrors BackChart). Tests that exercise the FORM must unlock it first;
 * this clicks Edit and waits for the editor's action bar to confirm it's open.
 *
 * Pass a scope when the panel isn't the default report-editor-panel (e.g. a
 * dialog-hosted editor); otherwise it targets the panel by test id.
 */
export async function enterReportEdit(page: Page, scope?: Locator): Promise<void> {
  const root = scope ?? page.getByTestId("report-editor-panel");
  await root.getByTestId("report-edit-button").click();
  // The Close button lives in the sticky editor action bar — present only while
  // the editor is open.
  await expect(root.getByRole("button", { name: "Close" })).toBeVisible();
}
