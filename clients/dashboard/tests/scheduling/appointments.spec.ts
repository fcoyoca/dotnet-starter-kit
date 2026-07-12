// E2E coverage for the scheduling calendar (react-big-calendar). All scheduling +
// administration + patient API calls are route-mocked; the authed session is seeded
// and shell calls stubbed by installShellMocks. The appointment is dated "today" so
// it lands in the default Day view. The list endpoint returns a RAW ARRAY.

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

const TYPE = {
  id: "00000000-0000-0000-0000-00000000f444",
  name: "Consultation",
  color: "#7045af",
  defaultDurationMinutes: 30,
  displayOrder: 0,
  isActive: true,
};

const SCHEDULE_CONFIG = {
  clinicId: CLINIC.id,
  startTime: "08:00",
  endTime: "18:00",
  intervalMinutes: 30,
};

// Date the appointment "today" in LOCAL terms — the calendar opens on the machine's
// local date, so the ymd must come from local time, not toISOString() (UTC), which is
// still yesterday between local midnight and the UTC rollover. 15:00Z on this date is
// 10:00 America/Chicago wall time on the SAME date, inside the 08:00–18:00 day view.
const now = new Date();
const todayYmd = [
  now.getFullYear(),
  String(now.getMonth() + 1).padStart(2, "0"),
  String(now.getDate()).padStart(2, "0"),
].join("-");
const APPOINTMENT = {
  id: "00000000-0000-0000-0000-00000000e333",
  clinicId: CLINIC.id,
  providerId: PROVIDER.id,
  patientId: null,
  appointmentTypeId: null,
  startUtc: `${todayYmd}T15:00:00Z`,
  endUtc: `${todayYmd}T15:30:00Z`,
  notes: "Annual checkup",
  status: "Scheduled",
  cancelled: false,
  noShow: false,
  isReservation: false,
  reservationTitle: null,
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
  await mockJsonResponse(page, "**/api/v1/administration/appointment-types**", [TYPE]);
  await mockJsonResponse(page, "**/api/v1/administration/schedule-config/**", SCHEDULE_CONFIG);
  await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([], { pageSize: 8 }));
  await mockJsonResponse(page, "**/api/v1/scheduling/appointments**", opts.appointments ?? [APPOINTMENT]);
}

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, TEST_USER);
  await installShellMocks(page);
});

// ─── Tests ────────────────────────────────────────────────────────────────

test.describe("scheduling/appointments", () => {
  test("renders the calendar with provider column and a mocked appointment", async ({ page }) => {
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");

    await expect(page.getByRole("heading", { name: /appointments/i })).toBeVisible();
    await expect(page.getByText("America/Chicago")).toBeVisible();
    // RBC toolbar + day-view resource header (Last, First) + event title.
    await expect(page.getByRole("button", { name: "Today" })).toBeVisible();
    await expect(page.getByText("Carter, Joel")).toBeVisible();
    await expect(page.getByText("Annual checkup")).toBeVisible();
  });

  test("switches to the Month view", async ({ page }) => {
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");

    await page.getByRole("button", { name: "Month" }).click();
    // Month view renders the appointment in a day cell.
    await expect(page.getByText("Annual checkup")).toBeVisible();
  });

  test("shows the empty state when the clinic has no providers", async ({ page }) => {
    await mockScheduling(page, { providers: [], appointments: [] });
    await page.goto("/scheduling/appointments");

    await expect(page.getByText(/no active providers for this clinic/i)).toBeVisible();
  });

  test("New appointment dialog has patient, type, reserve toggle, and times", async ({ page }) => {
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");
    await page.getByRole("button", { name: /new appointment/i }).click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /new appointment/i })).toBeVisible();
    await expect(dialog.getByRole("switch", { name: /reserve time/i })).toBeVisible();
    await expect(dialog.getByLabel(/provider/i)).toBeVisible();
    await expect(dialog.getByLabel(/appointment type/i)).toBeVisible();
    await expect(dialog.getByLabel(/^start/i)).toBeVisible();
    await expect(dialog.getByLabel(/^end/i)).toBeVisible();
    // Empty patient state is a "Search for patient" button (opens the search popup).
    await expect(dialog.getByRole("button", { name: /search for patient/i })).toBeVisible();
  });

  test("New appointment: searching and picking a patient collapses to a name + Change", async ({ page }) => {
    const patient = {
      id: "00000000-0000-0000-0000-0000000000a1",
      patientCode: "P-1001",
      firstName: "Jane",
      lastName: "Doe",
      dateOfBirth: "1990-04-05",
      isActive: true,
    };
    await mockScheduling(page);
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([patient], { pageSize: 8 }));
    await page.goto("/scheduling/appointments");
    await page.getByRole("button", { name: /new appointment/i }).click();

    // Empty state → open the nested search popup.
    await page.getByRole("dialog").getByRole("button", { name: /search for patient/i }).click();

    // Only the search popup has a text input; type and pick the result.
    await page.getByPlaceholder(/search by name or code/i).fill("Doe");
    await page.getByRole("option", { name: /Doe, Jane/ }).click();

    // Collapses to a read-only chip with the patient name + a Change button.
    await expect(page.getByText("Doe, Jane · P-1001")).toBeVisible();
    await expect(page.getByRole("button", { name: /^change$/i })).toBeVisible();
  });

  test("toggling Reserve swaps the patient/type fields for a Title field", async ({ page }) => {
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");
    await page.getByRole("button", { name: /new appointment/i }).click();

    const dialog = page.getByRole("dialog");
    await dialog.getByRole("switch", { name: /reserve time/i }).click();

    await expect(dialog.getByLabel(/title/i)).toBeVisible();
    await expect(dialog.getByLabel(/appointment type/i)).toHaveCount(0);
  });

  test("clicking an appointment opens the edit dialog with lifecycle actions", async ({ page }) => {
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");

    await page.getByText("Annual checkup").click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /edit appointment/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /check in/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /no-show/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /delete/i })).toBeVisible();
  });

  test("editing an existing appointment hides the Reserve time toggle", async ({ page }) => {
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");

    await page.getByText("Annual checkup").click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /edit appointment/i })).toBeVisible();
    // Reserve time is a create-time choice — a booked appointment can't become a reservation.
    await expect(dialog.getByRole("switch", { name: /reserve time/i })).toHaveCount(0);
  });

  test("a past, unconfirmed appointment is coloured as LATE (BackChart parity)", async ({ page }) => {
    // Start one hour ago → past + Scheduled + unconfirmed + non-reservation = "late".
    const late = {
      ...APPOINTMENT,
      id: "00000000-0000-0000-0000-00000000e555",
      notes: "Late visit",
      startUtc: new Date(now.getTime() - 60 * 60 * 1000).toISOString(),
      endUtc: new Date(now.getTime() - 30 * 60 * 1000).toISOString(),
    };
    await mockScheduling(page, { appointments: [late] });
    await page.goto("/scheduling/appointments");

    // Month view renders the event regardless of the clinic's business hours.
    await page.getByRole("button", { name: "Month" }).click();

    const event = page.locator(".rbc-event", { hasText: "Late visit" });
    // BackChart late colour is yellow #ffd31d → rgb(255, 211, 29).
    await expect(event).toHaveCSS("background-color", "rgb(255, 211, 29)");
  });

  test("editing an appointment: Change swaps the chip to the newly picked patient", async ({ page }) => {
    // Regression test: the picker's initialLabel-sync effect used to re-fire on
    // every `value` change and snap the chip back to the stale initialLabel
    // (sourced from a query keyed on the ORIGINAL patientId, which never
    // repoints) even after the user picked a different patient via Change.
    const patientAId = "00000000-0000-0000-0000-0000000a1a1a";
    const patientA = {
      id: patientAId,
      patientCode: "P-2001",
      isActive: true,
      demographics: {
        firstName: "Alice",
        middleInitial: null,
        lastName: "Anders",
        dateOfBirth: "1985-02-10",
        gender: "F",
      },
      insurance: null,
      lastVisitDate: null,
      nextVisitDate: null,
    };
    const patientB = {
      id: "00000000-0000-0000-0000-0000000b2b2b",
      patientCode: "P-2002",
      firstName: "Brian",
      lastName: "Baxter",
      dateOfBirth: "1978-11-22",
      isActive: true,
    };
    const apt = { ...APPOINTMENT, patientId: patientAId, notes: "Annual checkup" };

    await mockScheduling(page, { appointments: [apt] });
    // Route ordering matters: Playwright matches the most-recently-registered
    // handler first. Register the general search-list route (→ patient B)
    // BEFORE the specific by-id route (→ patient A's detail), so the by-id
    // URL resolves to A's detail while the search list resolves to [B].
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([patientB], { pageSize: 8 }));
    await mockJsonResponse(page, "**/api/v1/patient/patients/" + patientAId + "**", patientA);

    await page.goto("/scheduling/appointments");
    await page.getByText("Annual checkup").click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /edit appointment/i })).toBeVisible();
    await expect(page.getByText("Anders, Alice · P-2001")).toBeVisible();

    await dialog.getByRole("button", { name: /^change$/i }).click();
    await page.getByPlaceholder(/search by name or code/i).fill("Baxter");
    await page.getByRole("option", { name: /Baxter, Brian/ }).click();

    // The chip must show the newly picked patient (B), not snap back to A.
    await expect(page.getByText("Baxter, Brian · P-2002")).toBeVisible();
    await expect(page.getByText("Anders, Alice · P-2001")).toHaveCount(0);
  });

  test("editing an existing reservation still shows the Reserve time toggle", async ({ page }) => {
    const reservation = {
      ...APPOINTMENT,
      id: "00000000-0000-0000-0000-00000000e444",
      notes: null,
      isReservation: true,
      reservationTitle: "Lunch break",
    };
    await mockScheduling(page, { appointments: [reservation] });
    await page.goto("/scheduling/appointments");

    await page.getByText("Lunch break").click();

    const dialog = page.getByRole("dialog");
    await expect(dialog.getByRole("heading", { name: /edit appointment/i })).toBeVisible();
    // An existing reservation keeps the toggle so it can be turned back into an appointment.
    await expect(dialog.getByRole("switch", { name: /reserve time/i })).toBeVisible();
  });
});
