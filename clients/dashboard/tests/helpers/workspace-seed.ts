import type { Page } from "@playwright/test";

/**
 * Mirrors the persisted shape in src/state/patient-workspace-context.tsx.
 * Duplicated here rather than imported — the Playwright test project has
 * no path-alias config for "@/...", matching the existing convention in
 * auth-seed.ts (SeededUser is its own independent type).
 */
export type SeededPatientTab = {
  patientId: string;
  patientLabel: string;
  activeIncidentId?: string | null;
  openReportIds?: string[];
  activeReportId?: string | null;
};

const STORAGE_KEY = "fsh.dashboard.patientWorkspace.v1";

/**
 * Seed the persistent patient-workspace localStorage key BEFORE React
 * boots, so a test can land directly on a patient's chart — optionally
 * with an incident and/or report dialog already active — without
 * re-driving every click through the UI. Mirrors seedAuthedSession's
 * addInitScript pattern. The last tab in the array becomes the active tab.
 */
export async function seedPatientWorkspace(page: Page, tabs: SeededPatientTab[]): Promise<void> {
  const openTabs = tabs.map((t) => ({
    patientId: t.patientId,
    patientLabel: t.patientLabel,
    activeIncidentId: t.activeIncidentId ?? null,
    openReportIds: t.openReportIds ?? [],
    activeReportId: t.activeReportId ?? null,
  }));
  const state = {
    openTabs,
    activePatientId: openTabs[openTabs.length - 1]?.patientId ?? null,
  };

  await page.addInitScript(
    ({ key, value }) => {
      localStorage.setItem(key, value);
    },
    { key: STORAGE_KEY, value: JSON.stringify(state) },
  );
}
