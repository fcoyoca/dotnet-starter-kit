// E2E coverage for Procedures Performed (route-mocked): chart shortcut → report picker →
// add a procedure (dx defaults checked) → Save PUT payload; zero-dx guard toast.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PERMS = [
  "Permissions.Patient.SuperBills.View",
  "Permissions.Patient.SuperBills.Manage",
  "Permissions.Patient.Incidents.View",
  "Permissions.Patient.Reports.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000b2222";
const REPORT_ID = "00000000-0000-0000-0000-0000000c3333";
const DX_ID = "00000000-0000-0000-0000-0000000d4444";
const PC_ID = "00000000-0000-0000-0000-0000000e5555";
const IT_ID = "00000000-0000-0000-0000-0000000f6666";
const IT_ID_2 = "00000000-0000-0000-0000-0000000f7777";

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
  dateOfInitialVisit: null,
  dateOfLoss: "2026-01-10",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [DX_ID],
  createdAtUtc: "2026-01-10T08:00:00Z",
  updatedAtUtc: null,
};

const REPORT = {
  id: REPORT_ID,
  incidentId: INCIDENT_ID,
  patientId: PATIENT_ID,
  reportTypeId: 1,
  reportDate: "2026-01-12",
  isSigned: false,
  workflowStatus: "Draft",
  signedByName: null,
  version: 1,
};

const CUSTOM_DX = paged([
  {
    id: DX_ID,
    code: "M99.01",
    description: "Segmental dysfunction of cervical region",
    longDescription: null,
    isChiropractic: true,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

const PROCEDURE_CODES = paged([
  {
    id: PC_ID,
    code: "98940",
    name: "CMT 1-2 regions",
    description: "One to two spinal regions",
    procedureCategoryId: "00000000-0000-0000-0000-000000001111",
    procedureCategoryName: "CMT",
    codeSourceId: null,
    codeSourceName: null,
    macroText: "One to two spinal regions adjusted using CMT",
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

async function mockChartLookups(page: Page, opts?: { dxIds?: string[] }) {
  const incident = { ...INCIDENT, diagnosticIds: opts?.dxIds ?? [DX_ID] };
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);

  // ORDERING: Playwright resolves overlapping page.route globs LIFO (the most
  // recently registered handler wins first). "**/incidents**" also matches the
  // incident-detail URL ("/incidents/{id}"), so it must be registered BEFORE the
  // incident-detail-specific mock or getPatientIncident would receive the paged
  // list instead of the detail DTO.
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([incident]));
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_ID}`, incident);

  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([REPORT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", [
    { id: 1, name: "Daily Note", isActive: true },
  ]);
  await mockJsonResponse(page, "**/api/v1/administration/custom-diagnostics**", CUSTOM_DX);

  // Same LIFO caveat applies here: the insurance-types list glob also matches
  // the insurance-type-procedures sub-route, so it must be registered first.
  await mockJsonResponse(page, "**/api/v1/administration/insurance-types**", paged([
    { id: IT_ID, name: "Medicare", isActive: true, procedureCategoryId: null },
  ]));
  await mockJsonResponse(page, "**/api/v1/administration/procedure-categories**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/procedure-codes**", PROCEDURE_CODES);
  await mockJsonResponse(
    page,
    `**/api/v1/administration/insurance-types/${IT_ID}/procedures`,
    [{
      id: "00000000-0000-0000-0000-000000009999",
      insuranceTypeId: IT_ID,
      procedureCodeId: PC_ID,
      procedureCode: "98940",
      procedureName: "CMT 1-2 regions",
      procedureCategoryName: "CMT",
      price: 20.0,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: null,
    }],
  );
  await mockJsonResponse(page, `**/api/v1/patient/reports/${REPORT_ID}/procedures`, {
    id: null,
    reportId: REPORT_ID,
    isBilled: false,
    billedDateUtc: null,
    procedures: [],
  });
}

test.describe("procedures performed", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
  });

  test("chart shortcut → pick report → add procedure → save PUT payload", async ({ page }) => {
    await mockChartLookups(page);

    let putBody: unknown = null;
    await page.route(`**/api/v1/patient/reports/${REPORT_ID}/procedures`, async (route) => {
      if (route.request().method() === "PUT") {
        putBody = route.request().postDataJSON();
        await route.fulfill({ status: 204, body: "" });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          id: null, reportId: REPORT_ID, isBilled: false, billedDateUtc: null, procedures: [],
        }),
      });
    });

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await page.getByRole("button", { name: "Procedures", exact: true }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
    await expect(dialog).toBeVisible();

    // Phase 1: pick the report.
    await dialog.getByRole("button", { name: /Draft/ }).click();

    // Phase 2: wait for the negotiated price to land before adding, so the row
    // hydrates with the real charge rather than racing pricesQuery.
    await expect(dialog.getByText("$20.00")).toBeVisible();

    // Add the procedure from the picker (dx defaults to checked).
    await dialog.getByRole("button", { name: "Add 98940" }).click();
    await expect(dialog.getByRole("checkbox").last()).toBeChecked();
    await expect(dialog.getByLabel("Charge for 98940")).toHaveValue("20");

    await dialog.getByRole("button", { name: "Save" }).click();

    await expect.poll(() => putBody).not.toBeNull();
    const body = putBody as { reportId: string; procedures: Array<Record<string, unknown>> };
    expect(body.reportId).toBe(REPORT_ID);
    expect(body.procedures).toHaveLength(1);
    expect(body.procedures[0].procedureCodeId).toBe(PC_ID);
    expect(body.procedures[0].code).toBe("98940");
    expect(body.procedures[0].charge).toBe(20);
    expect(body.procedures[0].diagnosticIds).toEqual([DX_ID]);
  });

  test("adding a procedure with zero incident dx shows the guard toast", async ({ page }) => {
    await mockChartLookups(page, { dxIds: [] });

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await page.getByRole("button", { name: "Procedures", exact: true }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
    await dialog.getByRole("button", { name: /Draft/ }).click();
    await dialog.getByRole("button", { name: "Add 98940" }).click();

    await expect(
      page.getByText("Please add at least one DX code before picking procedures."),
    ).toBeVisible();
  });

  test("defaults the insurance picker to the patient's primary policy type", async ({ page }) => {
    await mockChartLookups(page);

    // Two admin insurance types, "Aetna PPO" sorting before "Medicare". The picker's
    // fallback would pick the alphabetical-first ("Aetna PPO"); the patient's primary
    // policy names "Medicare", so the patient's type must win. Re-register the price
    // sub-route after the broad list glob so LIFO doesn't let the list shadow it.
    await mockJsonResponse(page, "**/api/v1/administration/insurance-types**", paged([
      { id: IT_ID_2, name: "Aetna PPO", isActive: true, procedureCategoryId: null },
      { id: IT_ID, name: "Medicare", isActive: true, procedureCategoryId: null },
    ]));
    await mockJsonResponse(page, `**/api/v1/administration/insurance-types/${IT_ID}/procedures`, [{
      id: "00000000-0000-0000-0000-000000009999",
      insuranceTypeId: IT_ID,
      procedureCodeId: PC_ID,
      procedureCode: "98940",
      procedureName: "CMT 1-2 regions",
      procedureCategoryName: "CMT",
      price: 20.0,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: null,
    }]);
    await mockJsonResponse(page, "**/api/v1/patient/insurance-policies**", paged([{
      id: "00000000-0000-0000-0000-00000010aaaa",
      patientId: PATIENT_ID,
      insuranceCompanyId: "00000000-0000-0000-0000-00000010bbbb",
      insuranceCompanyName: "Medicare Nationwide",
      insuranceTypeId: IT_ID,
      insuranceTypeName: "Medicare",
      priority: "Primary",
      subscriberRelationship: "Self",
      isActive: true,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: null,
    }]));

    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // The incident-info sidebar surfaces the same associated type.
    await expect(page.getByText("Insurance Type:")).toBeVisible();

    await page.getByRole("button", { name: "Procedures", exact: true }).click();
    const dialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
    await dialog.getByRole("button", { name: /Draft/ }).click();

    // The Insurance combobox defaults to the patient's primary type, not the fallback.
    await expect(dialog.getByRole("button", { name: "Insurance" })).toContainText("Medicare");
  });
});
