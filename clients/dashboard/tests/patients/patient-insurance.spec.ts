// E2E coverage for Patient Insurance (route-mocked): the Insurance panel in the patient-info
// dialog, the policy list (many policies per patient, coordination-of-benefits order, Show
// inactive), and the Add/Edit policy dialog — including the two behaviours ported from BackChart
// that its Blazor rewrite had lost: the "Self" subscriber auto-fill and the required-subscriber
// validation. Mirrors the patterns in problems.spec.ts.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const INSURANCE_PERMS = [
  "Permissions.Patient.Patients.View",
  "Permissions.Patient.Patients.Update",
  "Permissions.Patient.InsurancePolicies.View",
  "Permissions.Patient.InsurancePolicies.Create",
  "Permissions.Patient.InsurancePolicies.Update",
  "Permissions.Patient.InsurancePolicies.Delete",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const AETNA_ID = "00000000-0000-0000-0000-0000000c1111";
const CIGNA_ID = "00000000-0000-0000-0000-0000000c2222";
const PPO_ID = "00000000-0000-0000-0000-0000000d1111";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: {
    firstName: "Alice",
    middleInitial: "Q",
    lastName: "Vance",
    dateOfBirth: "1990-04-12",
    gender: "F",
    maritalStatus: null,
    isMinor: false,
    raceId: null,
    ethnicityId: null,
    languageId: null,
    smokingStatusId: null,
    smokingStartDate: null,
    smokingEndDate: null,
    medicalAlertNotes: null,
  },
  contact: {
    address1: "42 Elm St", address2: null, city: "Austin", state: "TX", zipCode: "78701",
    phone: null, phoneExtension: null, cellPhone: null, email: null, preferredContactMethodId: null,
  },
  phi: { ssnMasked: null },
  employment: { occupation: null, employerName: "Acme Corp", employerAddress1: null, employerAddress2: null, employerCity: null, employerState: null, employerZipCode: null, employerPhone: null, employerPhoneExtension: null },
  guardian: null,
  nextOfKin: null,
  referralTypeId: null,
  hasNoKnownProblems: false,
  hasNoKnownMedications: false,
  hasNoKnownAllergies: false,
  receivesEmailReminders: false,
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const COMPANIES = paged([
  { id: AETNA_ID, name: "Aetna", insuranceTypeId: PPO_ID, insuranceTypeName: "PPO", formularyTiers: 0, address1: null, address2: null, city: null, state: null, zip: null, phone: null, isActive: true, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
  { id: CIGNA_ID, name: "Cigna", insuranceTypeId: PPO_ID, insuranceTypeName: "PPO", formularyTiers: 0, address1: null, address2: null, city: null, state: null, zip: null, phone: null, isActive: true, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
]);

const TYPES = paged([
  { id: PPO_ID, name: "PPO", isActive: true, procedureCategoryId: null, procedureCategoryName: null, createdAtUtc: "2026-01-01T00:00:00Z", updatedAtUtc: null },
]);

function policy(overrides: Record<string, unknown> = {}) {
  return {
    id: "00000000-0000-0000-0000-0000000e1111",
    patientId: PATIENT_ID,
    insuranceCompanyId: AETNA_ID,
    insuranceCompanyName: "Aetna",
    insuranceTypeId: PPO_ID,
    insuranceTypeName: "PPO",
    priority: "Primary",
    policyNumber: "POL-123",
    groupNumber: "GRP-9",
    memberId: "MBR-7",
    coPay: 25,
    deductible: 1500,
    effectiveDate: "2026-01-01",
    expirationDate: "2026-12-31",
    subscriberRelationship: "Self",
    subscriberFirstName: "Alice",
    subscriberLastName: "Vance",
    subscriberDateOfBirth: "1990-04-12",
    subscriberGender: "F",
    subscriberSsnMasked: "***-**-6789",
    subscriberEmployerName: "Acme Corp",
    subscriberAddress1: "42 Elm St",
    subscriberAddress2: null,
    subscriberCity: "Austin",
    subscriberState: "TX",
    subscriberZipCode: "78701",
    notes: null,
    isActive: true,
    createdByName: "Alice Nguyen",
    createdAtUtc: "2026-01-18T08:00:00Z",
    updatedByName: null,
    updatedAtUtc: null,
    ...overrides,
  };
}

async function mockLookups(page: Page) {
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/clinics**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/races**", []);
  await mockJsonResponse(page, "**/api/v1/administration/ethnicities**", []);
  await mockJsonResponse(page, "**/api/v1/administration/languages**", []);
  await mockJsonResponse(page, "**/api/v1/administration/smoking-statuses**", []);
  await mockJsonResponse(page, "**/api/v1/administration/preferred-contact-methods**", []);
  await mockJsonResponse(page, "**/api/v1/administration/referral-types**", []);
  await mockJsonResponse(page, "**/api/v1/administration/insurance-companies**", COMPANIES);
  await mockJsonResponse(page, "**/api/v1/administration/insurance-types**", TYPES);
}

/** Opens the patient-info dialog, then the Insurance section's policy-list dialog. */
async function openInsuranceList(page: Page) {
  await page.goto(`/patient-charts/${PATIENT_ID}`);
  const editButton = page.getByRole("button", { name: /edit patient info/i });
  await expect(editButton).toBeVisible({ timeout: 20_000 });
  await editButton.click();

  const info = page.getByRole("dialog").filter({ hasText: "Demographics" });
  await expect(info).toBeVisible();
  await info.getByRole("button", { name: "Manage" }).click();

  const list = page.getByRole("dialog").filter({ hasText: "Show inactive" });
  await expect(list.getByRole("heading", { name: "Insurance" })).toBeVisible();
  return list;
}

test.describe("patient insurance", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", INSURANCE_PERMS);
    await mockLookups(page);
  });

  test("lists many policies per patient in coordination-of-benefits order", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/patient/insurance-policies**",
      paged([
        policy({ id: "p-1", insuranceCompanyName: "Aetna", priority: "Primary" }),
        policy({ id: "p-2", insuranceCompanyName: "Cigna", priority: "Secondary", policyNumber: "POL-456" }),
      ]),
    );

    const list = await openInsuranceList(page);

    const rows = list.locator("li");
    await expect(rows).toHaveCount(2);
    await expect(rows.nth(0)).toContainText("Aetna");
    await expect(rows.nth(0)).toContainText("Primary");
    await expect(rows.nth(1)).toContainText("Cigna");
    await expect(rows.nth(1)).toContainText("Secondary");

    // The biller-facing summary line.
    await expect(rows.nth(0)).toContainText("Policy POL-123");
    await expect(rows.nth(0)).toContainText("Group GRP-9");
    await expect(rows.nth(0)).toContainText("Subscriber: Self");
  });

  test("Show inactive re-queries with includeInactive — the checkbox BackChart left unwired", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/patient/insurance-policies**",
      paged([policy({ id: "p-1" })]),
    );

    const list = await openInsuranceList(page);
    await expect(list.locator("li")).toHaveCount(1);

    const inactiveRequest = page.waitForRequest(
      (r) =>
        r.url().includes("/api/v1/patient/insurance-policies") &&
        r.url().includes("includeInactive=true"),
    );
    await list.getByLabel("Show inactive").check();
    await inactiveRequest;
  });

  test("Add Policy posts a Self policy with the subscriber auto-filled from the patient", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/patient/insurance-policies**", paged([]));

    const list = await openInsuranceList(page);
    await list.getByRole("button", { name: "Add Insurance" }).click();

    const editor = page.getByRole("dialog").filter({ hasText: "Add Insurance Policy" });
    await expect(editor).toBeVisible();

    // Self is the default, so the insured block is filled from the patient and locked.
    await expect(editor.getByLabel("Insured's First Name")).toHaveValue("Alice");
    await expect(editor.getByLabel("Insured's Last Name")).toHaveValue("Vance");
    await expect(editor.getByLabel("Insured's First Name")).toBeDisabled();
    await expect(editor.getByLabel("Insured's Employer")).toHaveValue("Acme Corp");
    await expect(editor.getByLabel("City")).toHaveValue("Austin");

    await editor.getByLabel("Insurer").click();
    await page.getByRole("menuitemradio", { name: "Aetna", exact: true }).click();
    await editor.getByLabel("Policy Number").fill("POL-777");
    await editor.getByLabel("Copay").fill("30");

    const post = page.waitForRequest(
      (r) => r.url().includes("/api/v1/patient/insurance-policies") && r.method() === "POST",
    );
    await editor.getByRole("button", { name: "Add Policy" }).click();

    const body = JSON.parse((await post).postData() ?? "{}");
    expect(body.insuranceCompanyId).toBe(AETNA_ID);
    expect(body.priority).toBe("Primary");
    expect(body.policyNumber).toBe("POL-777");
    expect(body.coPay).toBe(30);
    expect(body.subscriberRelationship).toBe("Self");
    // The patient's demographics ride along, so the row is a self-contained snapshot.
    expect(body.subscriberFirstName).toBe("Alice");
    expect(body.subscriberLastName).toBe("Vance");
    expect(body.subscriberCity).toBe("Austin");
    // Never send an SSN the user didn't type — the API only ever handed back a mask.
    expect(body.subscriberSsn).toBeNull();
  });

  test("a non-Self subscriber must be identified before the policy can be saved", async ({
    page,
  }) => {
    await mockJsonResponse(page, "**/api/v1/patient/insurance-policies**", paged([]));

    const list = await openInsuranceList(page);
    await list.getByRole("button", { name: "Add Insurance" }).click();
    const editor = page.getByRole("dialog").filter({ hasText: "Add Insurance Policy" });

    await editor.getByLabel("Insurer").click();
    await page.getByRole("menuitemradio", { name: "Aetna", exact: true }).click();

    // Switching off Self unlocks the insured block and clears the borrowed demographics.
    await editor.getByLabel("Relationship to Insured").click();
    await page.getByRole("menuitemradio", { name: "Spouse", exact: true }).click();

    const firstName = editor.getByLabel("Insured's First Name");
    await expect(firstName).toBeEnabled();
    await expect(firstName).toHaveValue("");

    // Legacy Flex required name + DOB here; the Blazor rewrite dropped the rule. It's back.
    const submit = editor.getByRole("button", { name: "Add Policy" });
    await expect(submit).toBeDisabled();

    await firstName.fill("Bob");
    await editor.getByLabel("Insured's Last Name").fill("Vance");
    await expect(submit).toBeDisabled();

    await editor.getByLabel("Insured's Date of Birth").fill("1988-05-04");
    await expect(submit).toBeEnabled();
  });

  test("an expiration date before the effective date blocks submit", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/insurance-policies**", paged([]));

    const list = await openInsuranceList(page);
    await list.getByRole("button", { name: "Add Insurance" }).click();
    const editor = page.getByRole("dialog").filter({ hasText: "Add Insurance Policy" });

    await editor.getByLabel("Insurer").click();
    await page.getByRole("menuitemradio", { name: "Aetna", exact: true }).click();

    await editor.getByLabel("Plan Effective Date").fill("2026-06-01");
    await editor.getByLabel("Plan Expiration Date").fill("2026-01-01");

    await expect(
      editor.getByText("The plan expiration date cannot precede the plan effective date."),
    ).toBeVisible();
    await expect(editor.getByRole("button", { name: "Add Policy" })).toBeDisabled();

    await editor.getByLabel("Plan Expiration Date").fill("2026-12-31");
    await expect(editor.getByRole("button", { name: "Add Policy" })).toBeEnabled();
  });

  test("editing a policy leaves the stored SSN alone unless a new one is typed", async ({
    page,
  }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/patient/insurance-policies**",
      paged([policy({ id: "p-1" })]),
    );

    const list = await openInsuranceList(page);
    await list.getByRole("button", { name: "Edit insurance policy" }).click();

    const editor = page.getByRole("dialog").filter({ hasText: "Edit Insurance Policy" });
    await expect(editor).toBeVisible();

    // The SSN is only ever shown masked, and the input starts blank.
    await expect(editor.getByLabel(/Insured's SSN \(on file: \*\*\*-\*\*-6789\)/)).toHaveValue("");

    await editor.getByLabel("Policy Number").fill("POL-999");

    const put = page.waitForRequest(
      (r) => r.url().includes("/api/v1/patient/insurance-policies/") && r.method() === "PUT",
    );
    await editor.getByRole("button", { name: "Save Changes" }).click();

    const body = JSON.parse((await put).postData() ?? "{}");
    expect(body.policyNumber).toBe("POL-999");
    // Blank means "leave unchanged" — the mask must never be posted back as the SSN.
    expect(body.subscriberSsn).toBeNull();
  });

  test("the info dialog's Insurance panel summarizes the policies on file", async ({ page }) => {
    await mockJsonResponse(
      page,
      "**/api/v1/patient/insurance-policies**",
      paged([
        policy({ id: "p-1", insuranceCompanyName: "Aetna", priority: "Primary" }),
        policy({ id: "p-2", insuranceCompanyName: "Cigna", priority: "Secondary" }),
      ]),
    );

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    const editButton = page.getByRole("button", { name: /edit patient info/i });
    await expect(editButton).toBeVisible({ timeout: 20_000 });
    await editButton.click();

    const info = page.getByRole("dialog").filter({ hasText: "Demographics" });
    const section = info.locator("section", { has: page.getByRole("heading", { name: "Insurance" }) });
    await expect(section).toContainText("Aetna");
    await expect(section).toContainText("Primary");
    await expect(section).toContainText("Cigna");
    await expect(section).toContainText("Secondary");
  });
});
