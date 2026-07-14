// E2E coverage for the open-report tab strip once it overflows.
//
// The strip used to wrap: past ~10 open reports the pills spilled onto a second
// and third line, growing the strip downward and squeezing the report editor.
// It now stays exactly one row — the pills scroll horizontally between a pair of
// chevron buttons that appear only when there is something to scroll to.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";
import { seedPatientWorkspace } from "../helpers/workspace-seed";

const PERMS = [
  "Permissions.Patient.Reports.View",
  "Permissions.Patient.Reports.Update",
  "Permissions.Patient.Incidents.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000c3331";

/** Enough reports to overflow the strip at any realistic viewport width. */
const REPORT_COUNT = 12;
const REPORT_IDS = Array.from(
  { length: REPORT_COUNT },
  (_, i) => `00000000-0000-0000-0000-0000000d${String(4000 + i).padStart(4, "0")}`,
);

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  referralTypeId: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const INCIDENT = {
  id: INCIDENT_ID,
  patientId: PATIENT_ID,
  incidentTypeId: null,
  departmentId: null,
  dateOfInitialVisit: "2026-05-01T12:00:00Z",
  dateOfLoss: "2026-04-10T12:00:00Z",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [],
  createdAtUtc: "2026-05-01T12:00:00Z",
  updatedAtUtc: null,
};

const REPORT_TYPES = [{ id: 1, name: "Initial Evaluation", displayOrder: 0, isActive: true }];
const FIELDS_TYPE_1 = [
  { id: 11, reportTypeId: 1, name: "Chief Complaint", category: "Subjective", displayOrder: 0, isActive: true },
];

function report(id: string, dayOfMonth: number) {
  return {
    id,
    incidentId: INCIDENT_ID,
    patientId: PATIENT_ID,
    reportTypeId: 1,
    reportDate: `2026-06-${String(dayOfMonth).padStart(2, "0")}T12:00:00Z`,
    version: 1,
    providerId: null,
    clinicId: null,
    isNoShow: false,
    vitals: { heightInches: null, weightLbs: null, bmi: null, systolic: null, diastolic: null, pulse: null, temperatureF: null },
    workflowStatus: "Draft",
    isSigned: false,
    signedByUserId: null,
    signedByName: null,
    signedOnUtc: null,
    signatureImagePath: null,
    signatureImageUrl: null,
    reviewRequestedByUserId: null,
    reviewRequestedOnUtc: null,
    reviewerProviderId: null,
    reviewSignedByUserId: null,
    reviewSignedByName: null,
    reviewSignedOnUtc: null,
    reviewSignatureImagePath: null,
    reviewSignatureImageUrl: null,
    fieldValues: [],
    addendums: [],
    associatedProblemIds: [],
    createdAtUtc: "2026-06-01T08:00:00Z",
    updatedAtUtc: null,
  };
}

const REPORTS = REPORT_IDS.map((id, i) => report(id, i + 1));

async function mockChart(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", REPORT_TYPES);
  await mockJsonResponse(page, "**/api/v1/administration/report-fields?reportTypeId=1**", FIELDS_TYPE_1);
  await mockJsonResponse(page, "**/api/v1/administration/macros**", paged([]));
  await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));

  // LIFO: the by-id mocks are registered after the broad globs so they win.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([INCIDENT]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_ID}`, INCIDENT);

  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged(REPORTS));
  for (const r of REPORTS) {
    await mockJsonResponse(page, "**/api/v1/patient/reports/" + r.id, r);
  }
}

/** The strip's horizontal scroller — the element the chevrons drive. */
const scroller = (page: Page) => page.getByRole("tablist", { name: "Open reports" });

const scrollLeftOf = (page: Page) => scroller(page).evaluate((el) => Math.round(el.scrollLeft));

/**
 * Chevron clicks animate; wait for the strip to come to rest before asserting.
 * Sampled frame-by-frame inside the page — polling it over the wire is at the
 * mercy of round-trip jitter once the worker pool is saturated.
 */
async function settle(page: Page) {
  await scroller(page).evaluate(
    (el) =>
      new Promise<void>((resolve) => {
        let last = el.scrollLeft;
        let restingFrames = 0;
        const tick = () => {
          if (el.scrollLeft === last) restingFrames++;
          else {
            restingFrames = 0;
            last = el.scrollLeft;
          }
          if (restingFrames >= 5) resolve();
          else requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
      }),
  );
}

test.describe("open-report tab strip overflow", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
    await mockChart(page);
    await seedPatientWorkspace(page, [
      {
        patientId: PATIENT_ID,
        patientLabel: "Alice Q Vance",
        activeIncidentId: INCIDENT_ID,
        openReportIds: REPORT_IDS,
        activeReportId: REPORT_IDS[0],
      },
    ]);
    await page.goto(`/patient-charts/${PATIENT_ID}`);
    // A dozen open reports means a dozen report fetches on top of the chart's
    // own; under a full worker pool that first paint outruns the default 5s.
    await expect(page.getByTestId("open-report-tab")).toHaveCount(REPORT_COUNT, { timeout: 15_000 });
  });

  test("twelve open reports stay on a single row instead of wrapping", async ({ page }) => {
    const tops = await page
      .getByTestId("open-report-tab")
      .evaluateAll((tabs) => tabs.map((t) => Math.round(t.getBoundingClientRect().top)));

    // Wrapping would put the later pills on their own line(s), i.e. a second top.
    expect(new Set(tops).size).toBe(1);
  });

  test("the chevrons scroll the strip, and each disables at its end", async ({ page }) => {
    const left = page.getByTestId("scroll-tabs-left");
    const right = page.getByTestId("scroll-tabs-right");

    // Parked at the start: nothing to the left yet.
    await expect(left).toBeDisabled();
    await expect(right).toBeEnabled();

    await right.click();
    await settle(page);
    expect(await scrollLeftOf(page)).toBeGreaterThan(0);
    await expect(left).toBeEnabled();

    // Drive it to the far end — the right chevron has to give out there.
    for (let i = 0; i < REPORT_COUNT && !(await right.isDisabled()); i++) {
      await right.click();
      await settle(page);
    }
    await expect(right).toBeDisabled();
    await expect(left).toBeEnabled();

    // ...and back, symmetrically.
    for (let i = 0; i < REPORT_COUNT && !(await left.isDisabled()); i++) {
      await left.click();
      await settle(page);
    }
    await expect(left).toBeDisabled();
    await expect(right).toBeEnabled();
    expect(await scrollLeftOf(page)).toBe(0);
  });

  test("the strip scrolls on its own — a plain wheel over it moves the pills sideways", async ({ page }) => {
    await scroller(page).hover();
    await page.mouse.wheel(0, 300);
    await settle(page);

    expect(await scrollLeftOf(page)).toBeGreaterThan(0);
  });

  test("a report scrolled out of view is still selectable via its tab", async ({ page }) => {
    const lastTab = page.getByTestId("open-report-tab").last();

    await lastTab.scrollIntoViewIfNeeded();
    // The pill's select button is named for the report; its sibling × is "Close …".
    await lastTab.getByRole("button", { name: "Initial Evaluation · Jun 12, 2026", exact: true }).click();

    await expect(lastTab).toHaveAttribute("aria-selected", "true");
  });
});
