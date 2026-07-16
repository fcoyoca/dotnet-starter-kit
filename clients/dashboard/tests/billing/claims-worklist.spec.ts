import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const CLAIMS_PAGE = {
  page: {
    items: [
      {
        id: "c1",
        superBillId: "s1",
        reportId: "r1",
        patientId: "p1",
        insuranceTypeId: "it1",
        status: "Draft",
        totalCharge: 420,
        lineCount: 3,
        createdAtUtc: "2026-05-10T10:00:00Z",
        submittedAtUtc: null,
        resolvedAtUtc: null,
      },
    ],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 20,
    totalPages: 1,
    hasPrevious: false,
    hasNext: false,
  },
  summary: { draft: 1, ready: 0, submitted: 0, paid: 0, denied: 0, outstandingCharge: 0 },
};

const CLAIM_DETAIL = {
  id: "c1",
  superBillId: "s1",
  reportId: "r1",
  patientId: "p1",
  insuranceTypeId: "it1",
  status: "Draft",
  totalCharge: 420,
  controlNumber: null,
  createdAtUtc: "2026-05-10T10:00:00Z",
  updatedAtUtc: null,
  submittedAtUtc: null,
  resolvedAtUtc: null,
  lines: [
    { procedureCodeId: "pc1", code: "99213", description: "Office visit", charge: 420, diagnosticIds: [] },
  ],
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
  await mockJsonResponse(page, "**/api/v1/identity/permissions", [
    "Permissions.Claims.View",
    "Permissions.Claims.Manage",
  ]);

  // Name-resolution endpoints the worklist calls — installShellMocks already
  // registered empty defaults for these, so re-registering here (after) wins.
  await mockJsonResponse(
    page,
    "**/api/v1/administration/insurance-types**",
    paged([{ id: "it1", name: "Medicare", isActive: true }]),
  );
  await mockJsonResponse(page, "**/api/v1/patient/patients/p1", {
    id: "p1",
    demographics: { firstName: "Maria", lastName: "Santos" },
  });
});

test.describe("Billing → Claims worklist", () => {
  test("renders worklist and advances a draft claim to Ready", async ({ page }) => {
    // Registration order matters (Playwright matches the most-recently
    // registered route first): the broad list mock goes first so the more
    // specific detail/ready mocks registered after it take priority for
    // their exact URLs.
    await mockJsonResponse(page, "**/api/v1/claims?**", CLAIMS_PAGE);
    await mockJsonResponse(page, "**/api/v1/claims/c1", CLAIM_DETAIL);
    await mockJsonResponse(page, "**/api/v1/claims/c1/ready", "c1", { method: "POST" });

    await page.goto("/billing/claims");

    await expect(page.getByRole("heading", { name: "Claims" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Santos, Maria" })).toBeVisible();
    await expect(page.getByText("Medicare")).toBeVisible();

    const readyReq = page.waitForRequest(
      (r) => r.url().includes("/api/v1/claims/c1/ready") && r.method() === "POST",
      { timeout: 5_000 },
    );

    // Navigate to the claim's detail page and drive Draft → Ready.
    await page.getByRole("link", { name: "Santos, Maria" }).click();
    await expect(page).toHaveURL(/\/billing\/claims\/c1$/);
    await expect(page.getByText("Procedures (snapshot)")).toBeVisible();
    await expect(page.getByText("99213")).toBeVisible();

    await page.getByRole("button", { name: /Mark Ready/ }).click();

    await readyReq;
  });
});
