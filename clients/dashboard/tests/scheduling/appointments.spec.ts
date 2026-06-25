// E2E coverage for the scheduling day-view calendar. All scheduling +
// administration API calls are route-mocked; the authed session is seeded into
// localStorage and the shell calls are stubbed by installShellMocks. The page
// is auth-only (no per-route permission guard), so direct navigation renders
// regardless of the (empty) mocked permission list.
//
// The list endpoint returns a RAW ARRAY (not a paged envelope). Times render in
// the clinic's IANA timezone, so assertions key on tz-independent text (provider
// name, notes) rather than wall-clock labels.

import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

// ─── Fixtures ────────────────────────────────────────────────────────────

const CLINIC = {
  id: "00000000-0000-0000-0000-00000000c111",
  code: "DT-01",
  name: "Downtown Clinic",
  address1: "1 Main St",
  address2: null,
  city: "Chicago",
  state: "IL",
  zip: "60601",
  phone: null,
  timeZoneId: "America/Chicago",
  isActive: true,
  createdAtUtc: "2026-05-01T10:00:00Z",
  updatedAtUtc: null,
};

const PROVIDER = {
  id: "00000000-0000-0000-0000-00000000d222",
  firstName: "Joel",
  lastName: "Carter",
  prefix: null,
  suffix: null,
  specialty: "Family Medicine",
  npi: null,
  kareoExternalId: null,
  primaryClinicId: CLINIC.id,
  primaryClinicName: CLINIC.name,
  userId: null,
  isActive: true,
  createdAtUtc: "2026-05-01T10:00:00Z",
  updatedAtUtc: null,
};

const APPOINTMENT = {
  id: "00000000-0000-0000-0000-00000000e333",
  clinicId: CLINIC.id,
  providerId: PROVIDER.id,
  patientId: null,
  appointmentTypeId: null,
  startUtc: "2026-06-25T16:00:00Z", // 11:00 in America/Chicago
  endUtc: "2026-06-25T16:30:00Z",
  notes: "Annual checkup",
  status: "Scheduled",
  cancelled: false,
  noShow: false,
};

async function mockScheduling(
  page: import("@playwright/test").Page,
  opts: { providers?: typeof PROVIDER[]; appointments?: typeof APPOINTMENT[] } = {},
) {
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([CLINIC], { pageSize: 100 }));
  await mockJsonResponse(
    page,
    "**/api/v1/administration/providers**",
    paged(opts.providers ?? [PROVIDER], { pageSize: 200 }),
  );
  await mockJsonResponse(page, "**/api/v1/scheduling/appointments**", opts.appointments ?? [APPOINTMENT]);
}

// ─── Shared beforeEach ──────────────────────────────────────────────────

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

// ─── Tests ────────────────────────────────────────────────────────────────

test.describe("scheduling/appointments", () => {
  test("renders the calendar with a provider column and a mocked appointment", async ({ page }) => {
    await mockScheduling(page);

    await page.goto("/scheduling/appointments");

    await expect(page.getByRole("heading", { name: /appointments/i })).toBeVisible();
    // Clinic timezone surfaces in the toolbar.
    await expect(page.getByText("America/Chicago")).toBeVisible();
    // Provider column header — "Last, First".
    await expect(page.getByText("Carter, Joel")).toBeVisible();
    // The appointment card prints its notes.
    await expect(page.getByText("Annual checkup")).toBeVisible();
  });

  test("shows the empty state when the clinic has no providers or appointments", async ({ page }) => {
    await mockScheduling(page, { providers: [], appointments: [] });

    await page.goto("/scheduling/appointments");

    await expect(page.getByRole("heading", { name: /appointments/i })).toBeVisible();
    await expect(page.getByText(/no active providers for this clinic/i)).toBeVisible();
  });

  test("opens the New appointment dialog with its form fields", async ({ page }) => {
    await mockScheduling(page);

    await page.goto("/scheduling/appointments");
    await page.getByRole("button", { name: /new appointment/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /new appointment/i })).toBeVisible();
    await expect(dialog.getByLabel(/provider/i)).toBeVisible();
    await expect(dialog.getByLabel(/^start/i)).toBeVisible();
    await expect(dialog.getByLabel(/^end/i)).toBeVisible();
  });

  test("opens the edit dialog with lifecycle actions when a card is clicked", async ({ page }) => {
    await mockScheduling(page);

    await page.goto("/scheduling/appointments");
    await page.getByText("Annual checkup").click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /edit appointment/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /check in/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /no-show/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /delete/i })).toBeVisible();
  });
});
