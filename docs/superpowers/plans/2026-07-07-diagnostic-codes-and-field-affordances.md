# Diagnostic Codes Dialog & Report Field Affordances Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Procedures Performed dialog's "Edit Dx Codes" with a BackChart-parity Diagnostic Codes dialog (category browse + DX search + add/remove incident dx + add-to-problem confirm), and move Procedures Performed to the Plan field's affordance row alongside new Import Allergies / Import Medications affordances.

**Architecture:** New Administration join `DiagnosticCategoryCode` (tenant category Guid ↔ global Diagnostic int) with a replace-set editor; `ensure` find-or-create bridging global codes → tenant `CustomDiagnostic` guids (what incidents store); a dedicated `PUT /incidents/{id}/diagnostics` set slice. Frontend: one new dialog + per-field affordances in the report editor.

**Tech Stack:** same as the parent feature (Mediator/EF Core/FluentValidation; React 19 + TanStack Query v5; Playwright route-mocked).

**Spec:** `docs/superpowers/specs/2026-07-07-diagnostic-codes-and-field-affordances-design.md`

## Global Constraints

- No stored procedures/functions/triggers — C# handlers only; migrations create tables/indexes only.
- Handlers `public sealed`, `ValueTask<T>`, `.ConfigureAwait(false)`, CancellationToken into every EF call; every command handler has a `{Command}Validator` (Architecture.Tests pairing).
- Do NOT modify `src/BuildingBlocks/**`. `TreatWarningsAsErrors` — 0 warnings.
- Frontend golden rule 9: per-call data through `mutate(arg)`.
- Working tree contains unrelated user edits (`clients/dashboard/src/components/command-palette/command-palette-dialog.tsx`, `clients/dashboard/src/components/layout/nav-data.ts`, `aspire.config.json`, `src/Host/FSH.Starter.Api/secure-uploads/`) — NEVER stage or commit them.
- One commit per task; commit messages end with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>`.
- Backend verify: `dotnet build src/FSH.Starter.slnx` + `dotnet test src/Tests/Administration.Tests` / `src/Tests/Patient.Tests` / `src/Tests/Architecture.Tests`. Frontend verify: `npm run build` in clients/dashboard (+ eslint 0-new; chart.tsx has 2 pre-existing a11y errors).
- Legacy copy is binding where quoted (toast texts, insert formats).

---

### Task 1: `DiagnosticCategoryCode` join + `CategoryId` filter on global diagnostics list

**Files:**
- Create: `src/Modules/Administration/Modules.Administration/Domain/DiagnosticCategoryCode.cs`
- Create: `src/Modules/Administration/Modules.Administration/Data/Configurations/DiagnosticCategoryCodeConfiguration.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Data/AdministrationDbContext.cs` (add DbSet after `Diagnostics`; this context applies configurations from assembly — verify by reading its OnModelCreating; if it uses explicit ApplyConfiguration calls instead, add one before `base.OnModelCreating`)
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/v1/Diagnostics/ListDiagnosticsQuery.cs` (trailing `Guid? CategoryId = null` param)
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Diagnostics/ListDiagnostics/ListDiagnosticsQueryHandler.cs` + `ListDiagnosticsEndpoint.cs`
- Create (generated): `src/Host/FSH.Starter.Migrations.PostgreSQL/Administration/*_AddDiagnosticCategoryCodes.cs`
- Test: `src/Tests/Administration.Tests/Features/DiagnosticCategoryCodeTests.cs`

**Interfaces:**
- Produces: `DiagnosticCategoryCode { Guid DiagnosticCategoryId; int DiagnosticId }` (composite PK, `Create(categoryId, diagnosticId)` factory — mirror `src/Modules/Patient/Modules.Patient/Domain/PatientIncidentDiagnostic.cs` exactly, table `DiagnosticCategoryCodes`); `AdministrationDbContext.DiagnosticCategoryCodes`; `ListDiagnosticsQuery` gains trailing `Guid? CategoryId = null`; endpoint gains `Guid? categoryId` query param. Tasks 2/5 rely on these names.

- [ ] **Step 1: Failing test** — new test file using the `DrugHandlerTests.CreateContext` harness style (InMemory `AdministrationDbContext`):

```csharp
    [Fact]
    public async Task ListDiagnostics_Should_Filter_By_CategoryId()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var inCat = Diagnostic.Create("M54.5", "Low back pain", null, 7, isChiropractic: true);
        var outCat = Diagnostic.Create("G43.1", "Migraine", null, 7, isChiropractic: false);
        db.Diagnostics.AddRange(inCat, outCat);
        var category = DiagnosticCategory.Create("Spine");
        db.DiagnosticCategories.Add(category);
        await db.SaveChangesAsync();
        db.DiagnosticCategoryCodes.Add(DiagnosticCategoryCode.Create(category.Id, inCat.Id));
        await db.SaveChangesAsync();
        var sut = new ListDiagnosticsQueryHandler(db);

        var result = await sut.Handle(new ListDiagnosticsQuery(CategoryId: category.Id), CancellationToken.None);

        result.Items.Single().Id.ShouldBe(inCat.Id);
    }
```

(Adjust `Diagnostic.Create` / `DiagnosticCategory.Create` argument lists to the real factory signatures — read both domain files first; the test's intent is binding, the ctor call is not.)

- [ ] **Step 2: Run to verify build failure** (`dotnet test src/Tests/Administration.Tests --filter "FullyQualifiedName~DiagnosticCategoryCodeTests"`).
- [ ] **Step 3: Entity** — `DiagnosticCategoryCode.cs`:

```csharp
namespace FSH.Modules.Administration.Domain;

/// <summary>Associates a tenant <see cref="DiagnosticCategory"/> with a global <see cref="Diagnostic"/>
/// (legacy <c>ascDiagnosticCategories</c> — <c>adcDxCategoryID</c>/<c>adcDxCodeID</c>). Many-to-many;
/// bare int code id (Diagnostic is IGlobalEntity). Tenant-scoped like the category. Hard delete on
/// replace (pure join row).</summary>
public sealed class DiagnosticCategoryCode
{
    public Guid DiagnosticCategoryId { get; private set; }
    public int DiagnosticId { get; private set; }

    private DiagnosticCategoryCode() { }

    public static DiagnosticCategoryCode Create(Guid diagnosticCategoryId, int diagnosticId) =>
        new() { DiagnosticCategoryId = diagnosticCategoryId, DiagnosticId = diagnosticId };
}
```

Configuration mirrors `PatientIncidentDiagnosticConfiguration` (table `DiagnosticCategoryCodes`, `HasKey(x => new { x.DiagnosticCategoryId, x.DiagnosticId })`, both required, plus `builder.HasIndex(x => x.DiagnosticCategoryId);`).

- [ ] **Step 4: Filter** — in `ListDiagnosticsQueryHandler`, after the last existing filter block:

```csharp
        if (query.CategoryId.HasValue)
        {
            IQueryable<int> categoryCodeIds = dbContext.DiagnosticCategoryCodes
                .Where(a => a.DiagnosticCategoryId == query.CategoryId.Value)
                .Select(a => a.DiagnosticId);
            q = q.Where(d => categoryCodeIds.Contains(d.Id));
        }
```

Endpoint: add `Guid? categoryId` lambda param, pass as the final ctor arg (follow how Task 6 of the parent plan added `ids` to ListCustomDiagnosticsEndpoint — same file style).

- [ ] **Step 5: Migration** — `dotnet build src/FSH.Starter.slnx && dotnet tool restore && dotnet ef migrations add AddDiagnosticCategoryCodes --project src/Host/FSH.Starter.Migrations.PostgreSQL --startup-project src/Host/FSH.Starter.Api --context AdministrationDbContext --output-dir Administration`. Verify: 1 CreateTable + index, no unrelated drift (STOP/BLOCKED if drift).
- [ ] **Step 6: Green** — Administration tests + full build 0 warnings.
- [ ] **Step 7: Commit** — `feat(administration): DiagnosticCategoryCode join + categoryId filter on diagnostics list`.

---

### Task 2: Category-codes association slices (GET list / PUT replace-set)

**Files:**
- Create: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/DiagnosticCategoryCodeDto.cs`
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/DiagnosticCategoryCodes/ListDiagnosticCategoryCodesQuery.cs` + `SetDiagnosticCategoryCodesCommand.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/DiagnosticCategoryCodes/ListCodes/…` + `SetCodes/…` (handler+validator+endpoint per slice)
- Modify: `AdministrationModule.cs` `MapEndpoints()` (wire both, near the DiagnosticCategories endpoints)
- Test: append to `src/Tests/Administration.Tests/Features/DiagnosticCategoryCodeTests.cs` + `src/Tests/Administration.Tests/Validators/SetDiagnosticCategoryCodesCommandValidatorTests.cs`

**Interfaces (mirror the InsuranceTypeProcedures slices — files under `Features/v1/InsuranceTypeProcedures/` are the verified pattern for BOTH slices, including the replace-set handler shape):**
- `DiagnosticCategoryCodeDto(Guid DiagnosticCategoryId, int DiagnosticId, string Code, string? Description)`
- `ListDiagnosticCategoryCodesQuery(Guid CategoryId) : IQuery<IReadOnlyList<DiagnosticCategoryCodeDto>>` → `GET /diagnostic-categories/{id:guid}/codes`, `.RequirePermission(AdministrationPermissions.DiagnosticCategories.View)`. Handler: category exists check (NotFound), join the association rows to `Diagnostics` for Code/Description, order by Code, `AsNoTracking`.
- `SetDiagnosticCategoryCodesCommand(Guid CategoryId, IReadOnlyList<int> DiagnosticIds) : ICommand<Unit>` → `PUT /diagnostic-categories/{id:guid}/codes` (`body with { CategoryId = id }` route-id-wins), `.RequirePermission(AdministrationPermissions.DiagnosticCategories.Update)`, 204. Handler: category NotFound check; verify all ids exist in `Diagnostics` (`CustomException` 400 listing missing ids); delete existing rows for the category (`ExecuteDeleteAsync` or range-remove) then insert `DiagnosticIds.Distinct()` rows; single `SaveChangesAsync`. Validator: CategoryId NotEmpty, DiagnosticIds NotNull, each > 0.

- [ ] **Step 1: Failing tests** — list returns joined code details; set replaces (2→1 roundtrip); set with unknown diagnostic id throws; validator cases (empty CategoryId, null list, non-positive id; empty list VALID = clears).
- [ ] **Step 2: Implement per the interfaces above** (read the InsuranceTypeProcedures slice files first and mirror them structurally).
- [ ] **Step 3: Green** — Administration.Tests + Architecture.Tests (validator pairing) + full build.
- [ ] **Step 4: Commit** — `feat(administration): diagnostic-category code association slices (list + replace-set)`.

---

### Task 3: `POST /administration/custom-diagnostics/ensure`

**Files:**
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/CustomDiagnostics/EnsureCustomDiagnosticCommand.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/CustomDiagnostics/EnsureCustomDiagnostic/…` (handler+validator+endpoint)
- Modify: `AdministrationModule.cs` (wire before the generic `/custom-diagnostics/{id:guid}` routes — literal `/ensure` segment wins)
- Test: `src/Tests/Administration.Tests/Features/EnsureCustomDiagnosticTests.cs` + validator tests

**Interfaces:**
- `EnsureCustomDiagnosticCommand(string Code, string? Description, string? LongDescription, bool IsChiropractic) : ICommand<Guid>` → `POST /custom-diagnostics/ensure`, `.RequirePermission(AdministrationPermissions.CustomDiagnostics.Create)`, returns the guid.
- Handler semantics (binding): trim Code; find by `EF.Functions.ILike(c.Code, code)` exact (case-insensitive) INCLUDING soft-deleted (`IgnoreQueryFilters` is NOT needed — check how the soft-delete filter is named on CustomDiagnostic's config; if a query filter hides deleted rows, re-include per `database.md` cross-filter rules — otherwise `IsDeleted` is just a column and a plain `Where` sees all rows; verify by reading `CustomDiagnosticConfiguration`). If found: reactivate when `IsDeleted`/`!IsActive` (use existing `Update(...)`/undelete affordances on the entity — read `CustomDiagnostic.cs`; if no undelete method exists, add a minimal `Reactivate()` that clears IsDeleted/DeletedOnUtc/DeletedBy and sets IsActive=true), return its Id. Else create via `CustomDiagnostic.Create(code, description, longDescription, isChiropractic)` and return the new Id.
- Validator: Code NotEmpty, MaxLength matching the entity config (read `CustomDiagnosticConfiguration` for the exact max), Description/LongDescription max lengths likewise.

- [ ] **Step 1: Failing tests** — creates when absent; returns existing id on same-code (case-insensitive) match without creating a duplicate; reactivates a soft-deleted match.
- [ ] **Step 2: Implement.** **Step 3: Green** (Administration + Architecture + build). **Step 4: Commit** — `feat(administration): ensure (find-or-create) custom diagnostic from a global code`.

---

### Task 4: `PUT /patient/incidents/{id}/diagnostics`

**Files:**
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientIncidents/SetIncidentDiagnosticsCommand.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientIncidents/SetIncidentDiagnostics/…` (handler+validator+endpoint)
- Modify: `PatientModule.cs` (wire in the Incident endpoints block, before the generic `/incidents/{id:guid}` PUT)
- Test: `src/Tests/Patient.Tests/Features/SetIncidentDiagnosticsHandlerTests.cs` + validator tests

**Interfaces:**
- `SetIncidentDiagnosticsCommand(Guid IncidentId, IReadOnlyList<Guid> DiagnosticIds) : ICommand<Unit>` → `PUT /incidents/{id:guid}/diagnostics` (`body with { IncidentId = id }`), `.RequirePermission(PatientPermissions.Incidents.Update)`, 204.
- Handler: load the incident (tracked, `Include` its diagnostics collection — read `PatientIncident.cs` for the collection/mutator; it already supports setting `diagnosticIds` via the update path, reuse that domain method, e.g. `SetDiagnostics`/equivalent — mirror what `UpdatePatientIncidentCommandHandler` calls) else NotFound; replace the set with `DiagnosticIds.Distinct()`; SaveChanges. **Do not touch any other incident field.**
- Validator: IncidentId NotEmpty; DiagnosticIds NotNull, each NotEmpty. Empty list VALID (clears — legacy allows removing all; the Procedures dialog's ≥1-dx rule applies to procedures, not the incident).

- [ ] Steps: failing tests (replace roundtrip incl. clear; NotFound; other fields untouched — assert e.g. `comments` unchanged) → implement → green (Patient + Architecture + build) → commit `feat(patient): set-incident-diagnostics slice`.

---

### Task 5: Dashboard API additions

**Files:**
- Modify: `clients/dashboard/src/api/administration.ts`
- Modify: `clients/dashboard/src/api/incidents.ts`

**Interfaces (Tasks 6–8 import these exact names):**

```typescript
// administration.ts — add categoryId to ListDiagnosticsParams and its query-string handling:
  categoryId?: string | null;         // in ListDiagnosticsParams
  if (params.categoryId) query.set("categoryId", params.categoryId);   // in listDiagnostics

// administration.ts — new section after the diagnostic-categories functions:
export type DiagnosticCategoryCodeDto = {
  diagnosticCategoryId: string;
  diagnosticId: number;
  code: string;
  description?: string | null;
};
export function listDiagnosticCategoryCodes(categoryId: string): Promise<DiagnosticCategoryCodeDto[]> {
  return apiFetch<DiagnosticCategoryCodeDto[]>(
    `/api/v1/administration/diagnostic-categories/${encodeURIComponent(categoryId)}/codes`,
  );
}
export async function setDiagnosticCategoryCodes(input: { categoryId: string; diagnosticIds: number[] }): Promise<void> {
  await apiFetch<void>(
    `/api/v1/administration/diagnostic-categories/${encodeURIComponent(input.categoryId)}/codes`,
    { method: "PUT", body: JSON.stringify({ categoryId: input.categoryId, diagnosticIds: input.diagnosticIds }) },
  );
}
export type EnsureCustomDiagnosticInput = {
  code: string;
  description?: string | null;
  longDescription?: string | null;
  isChiropractic: boolean;
};
export async function ensureCustomDiagnostic(input: EnsureCustomDiagnosticInput): Promise<string> {
  return apiFetch<string>("/api/v1/administration/custom-diagnostics/ensure", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

// incidents.ts — after updateIncident:
export async function setIncidentDiagnostics(input: { incidentId: string; diagnosticIds: string[] }): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/incidents/${encodeURIComponent(input.incidentId)}/diagnostics`,
    { method: "PUT", body: JSON.stringify({ incidentId: input.incidentId, diagnosticIds: input.diagnosticIds }) },
  );
}
```

- [ ] Implement, `npm run build`, commit — `feat(dashboard): API for diagnostic-category codes, ensure custom dx, set incident dx`.

---

### Task 6: Diagnostic Codes dialog + wire into Procedures Performed

**Files:**
- Create: `clients/dashboard/src/pages/patient-charts/diagnostic-codes-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/procedures-performed-dialog.tsx` (replace the `IncidentDialog` Edit-Dx wiring with the new dialog; remove the now-unused IncidentDialog import)

**Interfaces:**
- `export function DiagnosticCodesDialog(props: { patientId: string; patientName?: string; incidentId: string; open: boolean; onClose(): void; onSaved?(): void })`
- Behavior (binding, from the spec/legacy):
  1. Loads the incident (`getPatientIncident`) and resolves current dx via `listCustomDiagnostics({ ids })` → bottom table rows `{ customId?: string; globalId?: number; code; description }` (existing = customId set).
  2. Left panel default = **DX Categories**: `useQuery listDiagnosticCategories({ isActive: true, pageSize: 200 })` → Combobox; selecting loads `listDiagnostics({ categoryId, isActive: true, pageSize: 200 })` into the results table. A **DX Search** button toggles to search mode: text input (min 3 chars — toast "Please enter at least 3 characters." otherwise), search button, and **"Hide non-chiropractic codes"** checkbox default ON → `listDiagnostics({ search, isChiropractic: hide ? true : undefined, isActive: true, pageSize: 200 })`. Toggling back re-runs the category load.
  3. Results table (Code, Description): double-click a row OR per-row Add button adds `{ globalId, code, description, isChiropractic }` to the bottom list; duplicate (same code, case-insensitive, vs bottom list) → `toast.warning("Item already in the list.")`. On successful add, open a small confirm (reuse the app's confirm pattern — check for an existing confirm-dialog component under `clients/dashboard/src/components/`; if none, inline a mini Dialog): header "Add Dx Code to Problems", body `Would you like to add this DX Code to the Problem List for {patientName ?? "this patient"}?` — Yes marks the row `addToProblems: true`.
  4. Bottom table (Code, Description, remove button + double-click row to remove; caption "Double click a DX to remove from this incident").
  5. **Save**: for each row without `customId` → `await ensureCustomDiagnostic({...})` to get the guid (sequential is fine, ≤ handful); then `setIncidentDiagnostics({ incidentId, diagnosticIds })` with the full ordered guid list; then for each `addToProblems` row → `createProblem({ patientId, diagnosticId: globalId, diagnosticCode: code, diagnosticDescription: description, status: "Active", isMedicalAlert: false, incidentId })` (read `problems.ts` `CreateProblemInput` for exact required fields/status union); invalidate `["incident", incidentId]` and `["problems", patientId]`-prefixed keys; `onSaved?.()`; close. All through a single `useMutation` whose `mutationFn` receives the rows via `mutate(arg)` (golden rule 9). Nothing persists before Save.
  6. Reset all state on close via the `useEffect(!open)` pattern (this repo's established dialog convention — see procedures-performed-dialog.tsx lines ~82-89).
- In `procedures-performed-dialog.tsx`: `Edit Dx Codes` now sets `dxDialogOpen`; render `<DiagnosticCodesDialog patientId={patientId} incidentId={incidentId} open={dxDialogOpen} onClose={...}/>` with the same incident-query invalidation on close that the IncidentDialog wiring has today.

- [ ] Implement (read the current procedures-performed-dialog + export-reports-dialog for conventions), `npm run build` + `npx eslint` on both touched files (0 new), commit — `feat(dashboard): Diagnostic Codes dialog (category browse + DX search) for incident dx`.

---

### Task 7: Admin — category codes editor

**Files:**
- Modify: `clients/dashboard/src/pages/administration/diagnostic-categories.tsx`

Add a "Codes" affordance per category row (button or row action following that page's existing edit pattern) opening an association editor dialog: left = search global codes (`listDiagnostics({ search, pageSize: 50 })`, min 2 chars) with Add per row; right/bottom = current associations (`listDiagnosticCategoryCodes(categoryId)`) with remove; Save → `setDiagnosticCategoryCodes({ categoryId, diagnosticIds })` via `mutate(arg)`, invalidate `["diagnostic-category-codes", categoryId]`. Gate the affordance on the page's existing update-permission boolean. Follow the page's own dialog/table idioms (read it first); the insurance-types procedures editor (`clients/dashboard/src/pages/administration/insurance-types.tsx` or sibling — locate with Grep `setInsuranceTypeProcedures`) is the shape to mirror.

- [ ] Implement, `npm run build` + eslint 0-new on the file, commit — `feat(dashboard): diagnostic-category codes association editor`.

---

### Task 8: Report editor per-field affordances + import dialogs

**Files:**
- Create: `clients/dashboard/src/pages/patient-charts/import-allergies-dialog.tsx`
- Create: `clients/dashboard/src/pages/patient-charts/import-medications-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/report-editor.tsx`

**report-editor.tsx changes:**
1. REMOVE the header-strip Procedures Performed button (restore the plain `workflowStatus` badge markup that existed before commit 4d929a0d — see `git show 4d929a0d` for the exact before-state).
2. Add name-match sets beside `DX_IMPORT_FIELDS` / `PLAN_FIELDS` (PLAN_FIELDS already exists):

```tsx
const ALLERGY_IMPORT_FIELDS = new Set(["allergies"]);
const MEDICATION_IMPORT_FIELDS = new Set(["medications"]);
```

3. In the per-field affordance row (where `fieldImportsDx(f.name)` renders Import Dx Codes): the row's container currently renders only when `!readOnly`. Restructure so:
   - **Procedures Performed** renders on PLAN_FIELDS-matched fields **regardless of readOnly** (super bill stays editable post-sign — legacy parity): a button like the existing Import Dx Codes one (`ClipboardList` icon, text "Procedures Performed") that opens the existing ProceduresPerformedDialog. The dialog's `onMacroText` must now target **the specific matched field's id** (not the memoized global `planFieldId` — store `proceduresFieldId` in state when the button is clicked, and have `onProcedureMacroText` insert there; keep the no-field toast as a fallback). Remove the now-dead `planFieldId` memo if nothing else uses it.
   - **Import Allergies** on ALLERGY_IMPORT_FIELDS-matched fields and **Import Medications** on MEDICATION_IMPORT_FIELDS-matched fields, draft-only (`!readOnly`), same button style (`Pill` / `Tablets` icons). Each opens its dialog with an `onDone(text: string)` that calls `insertMacro(f.id, text)`.
4. Keep the ProceduresPerformedDialog render; it now opens from the field affordance (state: `proceduresOpen` + `proceduresFieldId`).

**Import dialogs (both mirror the legacy pickers; read `export-reports-dialog.tsx` for Dialog conventions):**
- `ImportAllergiesDialog({ patientId, open, onClose, onDone })`: rows from `searchPatientAllergies({ patientId, includeInactive: showInactive, pageSize: 100 })`; columns: checkbox, Drug Name, Date Noted (`formatDate`), Reaction, Status (Active/Inactive badge); header row: **All** / **None** links, **Show Inactive** toggle (default off), **Done** button. Done builds (legacy format, binding): per selected row `Allergen: ${drugName} | Reaction: ${reaction ?? ""}` + `"\n"`, then `onDone(text)` and close.
- `ImportMedicationsDialog({ patientId, open, onClose, onDone })`: rows from `searchPatientMedications(...)`; columns: checkbox, Drug Name, Prescriber, Start Date, Status; All/None/Show Inactive/Done. Done format per selected row (binding): `Medication Name: ${drugName}\n` + (when prescriber or startDate present) `Prescriber: ${prescriber ?? ""} | Start Date: ${formatDate(startDate)}\n` + (when instructions present) `Instructions: ${instructions}\n` + trailing `"\n"`.
- Both reset selection state on close (`useEffect(!open)` pattern).

- [ ] Implement, verify: `npm run build`; eslint 0-new on the three files; **manually confirm via the existing Playwright spec that the chart-shortcut flow still passes** (`npx playwright test tests/patient-charts/procedures-performed.spec.ts` — it must stay 2/2; it uses the chart entry, which is untouched). Commit — `feat(dashboard): per-field report affordances — Procedures Performed on Plan, Import Allergies/Medications`.

---

### Task 9: Playwright coverage

**Files:**
- Create: `clients/dashboard/tests/patient-charts/diagnostic-codes.spec.ts`
- Modify (if needed): `clients/dashboard/tests/patient-charts/procedures-performed.spec.ts`

Two tests in the new spec (helpers/conventions from `procedures-performed.spec.ts`):
1. **Report-editor Plan affordance + dialog chain**: mock a report (draft) whose report-fields include one named "Plan"; goto the report editor; assert "Procedures Performed" button renders in the Plan field's row (and NOT in the page header); open it; click **Edit Dx Codes**; in the Diagnostic Codes dialog pick the mocked category, add the mocked code from the results table (assert the add-to-problems confirm appears — answer No), Save; assert the `PUT /incidents/{id}/diagnostics` payload contains the ensured guid (mock `POST /custom-diagnostics/ensure` → fixed guid).
2. **Dupe guard**: adding the same code twice shows "Item already in the list.".

Also verify the existing procedures-performed spec still passes; if the report-editor entry changed selectors it relies on (it should not — it tests the chart path), fix mocks minimally without weakening assertions.

- [ ] Implement until green (`npx playwright test tests/patient-charts/diagnostic-codes.spec.ts` 2/2 + existing spec 2/2), commit — `test(dashboard): Playwright coverage for Diagnostic Codes dialog + Plan-field affordance`.

---

## Self-review notes

- Spec §1 → Tasks 1–6; §2 → Task 8; §3 → Tasks 1–4 (unit) + 9 (e2e); admin editor → Task 7.
- Deliberate verify-at-execution points are called out inline (factory signatures in Task 1, CustomDiagnostic soft-delete filter/undelete in Task 3, PatientIncident dx mutator in Task 4, admin page idioms in Task 7, pre-4d929a0d badge markup in Task 8) — each names the exact file to read.
- Type consistency: `DiagnosticCategoryCodeDto` (backend) ↔ camelCase mirror (Task 5); `SetIncidentDiagnosticsCommand` body `{ incidentId, diagnosticIds }` matches Task 5's fetch; `EnsureCustomDiagnosticInput` matches Task 3's command.
