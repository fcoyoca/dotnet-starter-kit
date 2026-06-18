# Patient Module

The Patient module manages multi-tenant clinical patient records with HIPAA-compliant PHI
field-level encryption. It lives in `src/Modules/Patient/`.

## Key quirks

### PhiEncryptor + PatientOptions

- `IPhiEncryptor` / `PhiEncryptor` handles SSN encrypt/decrypt and HMAC search-hash.
- **`PhiHmacKey` is required config** — set in `PatientOptions:PhiHmacKey` (base64 32-byte
  secret). Dev value lives in `appsettings.Development.json`; prod must come from Key Vault or
  an env var (`PatientOptions__PhiHmacKey`). Startup fails fast (`ValidateOnStart`) if missing.
- Encryption uses ASP.NET Data Protection (non-deterministic random IV) — decrypt-only is
  transparent; don't read raw column bytes.
- `HashForSearch` uses HMAC-SHA256 with the configured key, uppercased + trimmed input, hex
  output (64 chars). This is deterministic for exact-match lookups. Tenant isolation is EF
  query-filter, not hash-level.

### Entity shape

- `Patient` is `AggregateRoot<Guid>` + `ISoftDeletable`. Schema `patient`.
- 6 owned value objects (table-splitting onto the `patients` table): `Demographics`, `Contact`,
  `PHI`, `Employment`, `Guardian`, `NextOfKin`, `Insurance`.
- `PatientPHI.Ssn` and `PatientPHI.GuardianSsn` are encrypted via EF value converters in
  `PatientConfiguration.cs`.
- `PatientPHI.SsnSearchHash` is a HMAC hash stored separately for exact-match lookups — never
  expose the raw hash column in DTOs.
- Lookup FK columns (`RaceId`, `EthnicityId`, `LanguageId`, `SmokingStatusId`,
  `PreferredContactMethodId`, `ReferralTypeId`) are bare `int?` — no EF FK constraints cross
  module boundaries; the Administration module owns those lookup tables.

### `UpdatePatientCommand` is full-replace

There is no per-section PATCH endpoint. Every update sends all fields. The frontend's
`mergePatientUpdate(detail, overrides)` helper in `patient-mappers.ts` handles the merge so
each section dialog only deals with its own fields.

### MSSQL migration tool

`dotnet run --project src/Host/FSH.Starter.DbMigrator -- migrate-from-mssql` migrates
BackChart (MSSQL) patients into a tenant. The `BronstonChiro` Bronston DB uses:
- `pID` is `varchar`, not `int` — never cast it.
- Symmetric key encryption for PHI columns — decrypt before mapping.
- SQL compat level 100 — use `ROW_NUMBER()` pagination, `CONVERT(datetime, ...)` not `TRY_CONVERT`.
- Empty `pSex` → default to `"UN"` (Unknown).
- `IsMinor=true` with no guardian data → set `IsMinor=false` (mapper guard).

### Domain events

- `PatientCreatedDomainEvent` — raised on create.
- `PatientUpdatedDomainEvent` — raised on update.
- `PatientPhiAccessedDomainEvent` — raised on `GetPatientByIdQuery`; consumed by Auditing module
  to write HIPAA audit trail (`Action=PHI_ACCESS`).

### Base `OnModelCreating`

`PatientDbContext` calls `base.OnModelCreating(modelBuilder)` **last**, after
`ApplyConfigurationsFromAssembly`. This is required for Finbuckle tenant filters to apply
correctly — do not reorder.

### Tests

Unit tests live in `src/Tests/Patient.Tests/`. The `PhiEncryptorTests.Build()` helper passes
a `PatientOptions` instance with the dev key — update it if the `PatientOptions` shape changes.
