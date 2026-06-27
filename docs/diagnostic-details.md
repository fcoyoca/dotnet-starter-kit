# Diagnostic Details (ICD Catalog)

The global ICD diagnostics catalog ("Diagnostic Details" — legacy BackChart
`LupDiagnostics` / the CMS `ICD10CMCodes` reference set). A shared, cross-tenant
code list used by the patient-chart diagnostic/problem pickers.

## Model

`Diagnostic` (Administration module, `AggregateRoot<int>`, **`IGlobalEntity`** —
shared across tenants, not tenant-scoped):
`Code`, `Description`, `LongDescription?`, `CodeSourceId` (→ `CodeSource`, the
"ICD 10/9" source dropdown), `IsChiropractic`, `IsBillable?` (ICD-10-CM
billable-leaf vs header), `IsActive`, soft-delete, `LegacyId?`.

The **"ICD 10" dropdown** is the `CodeSource` lookup; an `ICD-10-CM` source
(id 7) was added to the seed (`DiagnosticSource` 2 = "ICD 10" in legacy).

## API (under `/api/v1/administration`)

| Method | Route | Purpose |
|---|---|---|
| GET | `/diagnostics?search=&codeSourceId=&isActive=&isChiropractic=&isBillable=` | Search/list |
| GET | `/diagnostics/{id}` | Get one |
| POST | `/diagnostics` | Create |
| PUT | `/diagnostics/{id}` | Update |
| DELETE | `/diagnostics/{id}` | Soft delete |

Permissions: `Administration.Diagnostics.{View,Create,Update,Delete}`.

## Data migration / seed

Seeded once (global) by `AdministrationDbInitializer.SeedDiagnosticsAsync`, which
bulk-loads the embedded `Data/Seed/icd10cm.tsv` when the `Diagnostics` table is
empty (batched). The current seed holds the **1,000 ICD-10-CM rows available in
the source LocalDB** (`BronstonAuthenticatingDB.dbo.ICD10CMCodes`, codes A00–B459).

> The source LocalDB is a 1,000-row sample. To load the full ICD-10-CM set
> (~74k codes) drop a fuller tab-separated file (`code <TAB> isBillable <TAB>
> shortDescription <TAB> longDescription`) at `Data/Seed/icd10cm.tsv` and re-run
> the initializer against an empty table.

## Frontend (dashboard)

- `src/api/administration.ts` — `listDiagnostics` / `createDiagnostic` /
  `updateDiagnostic` / `deleteDiagnostic` + `DiagnosticDto`.
- `src/pages/administration/diagnostics.tsx` — **Diagnostic Details** admin page:
  searchable list, ICD-source filter, create/edit/delete with the source dropdown.
- Nav entry under Administration; route `/administration/diagnostics`.

## Changelog

- **Added** — Diagnostic Details: a global ICD-10 diagnostics catalog with admin
  CRUD, an ICD source (code-source) dropdown, and a migrated seed.

> Mirror this entry into the separate docs site
> (`github.com/fullstackhero/docs` → `src/content/docs/changelog/`) per golden
> rule #10 — that repo is not cloned in this workspace.
