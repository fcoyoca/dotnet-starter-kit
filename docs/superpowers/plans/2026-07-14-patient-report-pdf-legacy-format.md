# Patient Report PDF — Legacy Format + Per-Clinic Orientation — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the exported patient-report PDF carry the same content, in the same order, as the legacy BackChart PDF, and let an administrator choose the page orientation per clinic.

**Architecture:** Orientation becomes a `PrintOrientation` enum property on the `Clinic` aggregate (Administration module), edited in the dashboard's Administration → Clinics form. The export query handler (Patient module) enriches its render models with incident context (DOIV/DOL/DX), clinic name + orientation, provider/department names, and signature-image bytes — all cross-module reads go through `Administration.Contracts` over Mediator. `PatientReportPdfRenderer` (QuestPDF) is rewritten to the legacy layout: Letter size, per-report orientation, repeating header (clinic name, report title, patient block), category → field → text body, inline vitals under Clinical Exam, addendums, signature images, `N of M` footer.

**Tech Stack:** .NET 10, EF Core 10 (PostgreSQL), Mediator 3.x, FluentValidation, QuestPDF (render), PdfSharp (test assertions), xUnit + Shouldly + NSubstitute; React 19 + TanStack Query (dashboard).

**Spec:** `docs/superpowers/specs/2026-07-14-patient-report-pdf-legacy-format-design.md`
**Legacy reference:** `backchart-master/DotNet/App_Code/VB/BCFileGeneration.vb` — `generatePDFTemplate` (996–1075), `generateReportsAsPDF` (697–992).

## Global Constraints

- Mediator handlers are `public sealed`, return `ValueTask<T>`, and `.ConfigureAwait(false)` every await.
- `CancellationToken` propagates into every EF / IO call.
- Every command handler and paginated query handler has a `{Name}Validator` (enforced by `Architecture.Tests`).
- A module references another module only through its `.Contracts` project — never its runtime project.
- Do **not** modify `src/BuildingBlocks`.
- Build runs with `TreatWarningsAsErrors` — warnings fail the build.
- File-scoped namespaces, explicit types (`var` only when the right-hand side is obvious), `is null` / `is not null`, records for DTOs.
- Backend build: `dotnet build src/FSH.Starter.slnx`. Backend tests: `dotnet test src/FSH.Starter.slnx`. Single project: `dotnet test src/Tests/Patient.Tests`.
- Default orientation is **Portrait** (the value legacy `ExportReport.razor:201` passed).
- Keep the `DRAFT — UNSIGNED` watermark for unsigned reports. Keep the patient code in the header; do not print gender in the header.

## File Structure

| File | Responsibility |
|---|---|
| `src/Modules/Administration/Modules.Administration.Contracts/Dtos/PrintOrientation.cs` | **Create** — the `PrintOrientation` enum, shared by module + consumers |
| `src/Modules/Administration/Modules.Administration.Contracts/Dtos/ClinicDto.cs` | Modify — carry `PrintOrientation` |
| `src/Modules/Administration/Modules.Administration.Contracts/v1/Clinics/{Create,Update}ClinicCommand.cs` | Modify — accept `PrintOrientation` |
| `src/Modules/Administration/Modules.Administration/Domain/Clinic.cs` | Modify — `PrintOrientation` property, `Create`/`Update` params |
| `src/Modules/Administration/Modules.Administration/Data/Configurations/ClinicConfiguration.cs` | Modify — persist the enum as a string column |
| `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/**` | Modify — handlers + validators + DTO mapping |
| `src/Host/FSH.Starter.Migrations.PostgreSQL/Administration/*_AddClinicPrintOrientation.cs` | **Create** (generated) — the migration |
| `clients/dashboard/src/api/administration.ts` | Modify — `ClinicDto` / `ClinicInput` gain `printOrientation` |
| `clients/dashboard/src/pages/administration/clinics.tsx` | Modify — Portrait/Landscape select in the clinic form |
| `src/Modules/Patient/Modules.Patient/Services/IPatientReportPdfRenderer.cs` | Modify — render models gain clinic/incident/signature fields |
| `src/Modules/Patient/Modules.Patient/Services/PatientReportPdfRenderer.cs` | Rewrite — legacy layout, per-report orientation |
| `.../ExportPatientReportsPdf/ExportPatientReportsPdfQueryHandler.cs` | Modify — load incident, DX, clinic, provider, department, signature bytes |
| `src/Tests/Administration.Tests/**` | Tests for the entity + validators |
| `src/Tests/Patient.Tests/Services/PatientReportPdfRendererTests.cs` | Tests for orientation + legacy content |
| `src/Tests/Patient.Tests/Features/ExportPatientReportsPdfHandlerTests.cs` | **Create** — handler enrichment tests |
| `docs/patient-reports.md` | Modify — document the format + orientation setting |

---

### Task 1: `PrintOrientation` on the Clinic aggregate (backend, end to end)

**Files:**
- Create: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/PrintOrientation.cs`
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/ClinicDto.cs`
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/v1/Clinics/CreateClinicCommand.cs`
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/v1/Clinics/UpdateClinicCommand.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Domain/Clinic.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Data/Configurations/ClinicConfiguration.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/CreateClinic/CreateClinicCommandHandler.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/CreateClinic/CreateClinicCommandValidator.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/UpdateClinic/UpdateClinicCommandHandler.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/UpdateClinic/UpdateClinicCommandValidator.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/GetClinicById/GetClinicByIdQueryHandler.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Clinics/ListClinics/ListClinicsQueryHandler.cs`
- Test: `src/Tests/Administration.Tests/Domain/ClinicTests.cs` (create if absent)

**Interfaces:**
- Produces: `enum PrintOrientation { Portrait, Landscape }` in namespace `FSH.Modules.Administration.Contracts.Dtos`; `ClinicDto` gains a `PrintOrientation PrintOrientation` member (appended **before** `CreatedAtUtc`? No — append **after** `UpdatedAtUtc` to avoid reordering existing positional args at every call site: final member is `PrintOrientation PrintOrientation = PrintOrientation.Portrait`); `Clinic.PrintOrientation` (get; private set); `Clinic.Create(..., string? timeZoneId = null, PrintOrientation printOrientation = PrintOrientation.Portrait)`; `Clinic.Update(..., string? timeZoneId = null, PrintOrientation printOrientation = PrintOrientation.Portrait)`; both commands gain a trailing `PrintOrientation PrintOrientation = PrintOrientation.Portrait` parameter.

- [ ] **Step 1: Write the failing test**

Create `src/Tests/Administration.Tests/Domain/ClinicTests.cs` (if the file exists, add these two facts to it):

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Domain;
using Shouldly;
using Xunit;

namespace Administration.Tests.Domain;

public sealed class ClinicTests
{
    private static Clinic SomeClinic(PrintOrientation orientation = PrintOrientation.Portrait) =>
        Clinic.Create("C1", "Main Street Clinic", "1 Main St", null, "Springfield", "IL", "62701",
            phone: null, legacyId: null, timeZoneId: "America/Chicago", printOrientation: orientation);

    [Fact]
    public void Create_Should_Default_PrintOrientation_To_Portrait()
    {
        Clinic clinic = Clinic.Create("C1", "Main Street Clinic", "1 Main St", null, "Springfield",
            "IL", "62701", phone: null);

        clinic.PrintOrientation.ShouldBe(PrintOrientation.Portrait);
    }

    [Fact]
    public void Update_Should_Change_PrintOrientation()
    {
        Clinic clinic = SomeClinic();

        clinic.Update("C1", "Main Street Clinic", "1 Main St", null, "Springfield", "IL", "62701",
            phone: null, isActive: true, timeZoneId: "America/Chicago",
            printOrientation: PrintOrientation.Landscape);

        clinic.PrintOrientation.ShouldBe(PrintOrientation.Landscape);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test src/Tests/Administration.Tests --filter FullyQualifiedName~ClinicTests`
Expected: FAIL — compile error, `Clinic` has no `PrintOrientation` and `Create` has no `printOrientation` parameter.

- [ ] **Step 3: Add the enum**

Create `src/Modules/Administration/Modules.Administration.Contracts/Dtos/PrintOrientation.cs`:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

/// <summary>
/// Page orientation used when printing/exporting a clinic's patient reports (legacy BackChart
/// ClientSettings <c>PRINT_ORIENTATION</c>, scoped per clinic here). Persisted as a string so the
/// column stays readable and stable if members are reordered.
/// </summary>
public enum PrintOrientation
{
    Portrait,
    Landscape,
}
```

- [ ] **Step 4: Add the property and thread it through the entity**

In `src/Modules/Administration/Modules.Administration/Domain/Clinic.cs`:

Add `using FSH.Modules.Administration.Contracts.Dtos;` at the top, and the property beside `TimeZoneId`:

```csharp
    /// <summary>
    /// Page orientation for this clinic's exported patient-report PDFs (legacy
    /// <c>PRINT_ORIENTATION</c>). Defaults to <see cref="PrintOrientation.Portrait"/>.
    /// </summary>
    public PrintOrientation PrintOrientation { get; private set; } = PrintOrientation.Portrait;
```

In `Create`, add the trailing parameter and set it:

```csharp
    public static Clinic Create(
        string code,
        string name,
        string address1,
        string? address2,
        string city,
        string state,
        string zip,
        string? phone,
        int? legacyId = null,
        string? timeZoneId = null,
        PrintOrientation printOrientation = PrintOrientation.Portrait)
```

and inside the object initializer, after `TimeZoneId = …`:

```csharp
            PrintOrientation = printOrientation,
```

In `Update`, add the trailing parameter and assign it:

```csharp
    public void Update(
        string code,
        string name,
        string address1,
        string? address2,
        string city,
        string state,
        string zip,
        string? phone,
        bool isActive,
        string? timeZoneId = null,
        PrintOrientation printOrientation = PrintOrientation.Portrait)
```

and after `TimeZoneId = …;` in the body:

```csharp
        PrintOrientation = printOrientation;
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test src/Tests/Administration.Tests --filter FullyQualifiedName~ClinicTests`
Expected: PASS (2 tests).

- [ ] **Step 6: Persist the column**

In `src/Modules/Administration/Modules.Administration/Data/Configurations/ClinicConfiguration.cs`, add after the `TimeZoneId` line:

```csharp
        builder.Property(x => x.PrintOrientation)
            .IsRequired()
            .HasMaxLength(20)
            .HasConversion<string>();
```

- [ ] **Step 7: Thread it through contracts and handlers**

`ClinicDto.cs` — append as the last member so existing positional constructions keep compiling:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ClinicDto(
    Guid Id,
    string Code,
    string Name,
    string Address1,
    string? Address2,
    string City,
    string State,
    string Zip,
    string? Phone,
    string TimeZoneId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    PrintOrientation PrintOrientation = PrintOrientation.Portrait);
```

`CreateClinicCommand.cs` — add the trailing parameter (and the `using`):

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Clinics;

public sealed record CreateClinicCommand(
    string Code,
    string Name,
    string Address1,
    string? Address2,
    string City,
    string State,
    string Zip,
    string? Phone,
    string? TimeZoneId = null,
    PrintOrientation PrintOrientation = PrintOrientation.Portrait) : ICommand<Guid>;
```

`UpdateClinicCommand.cs` — same treatment:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Clinics;

public sealed record UpdateClinicCommand(
    Guid Id,
    string Code,
    string Name,
    string Address1,
    string? Address2,
    string City,
    string State,
    string Zip,
    string? Phone,
    bool IsActive,
    string? TimeZoneId = null,
    PrintOrientation PrintOrientation = PrintOrientation.Portrait) : ICommand<Unit>;
```

`CreateClinicCommandHandler.cs` — pass it to `Clinic.Create` (note `legacyId` must be named because `printOrientation` follows it):

```csharp
        Clinic entity = Clinic.Create(
            command.Code,
            command.Name,
            command.Address1,
            command.Address2,
            command.City,
            command.State,
            command.Zip,
            command.Phone,
            timeZoneId: command.TimeZoneId,
            printOrientation: command.PrintOrientation);
```

`UpdateClinicCommandHandler.cs` — pass it to `entity.Update`:

```csharp
        entity.Update(
            command.Code,
            command.Name,
            command.Address1,
            command.Address2,
            command.City,
            command.State,
            command.Zip,
            command.Phone,
            command.IsActive,
            command.TimeZoneId,
            command.PrintOrientation);
```

Both validators (`CreateClinicCommandValidator.cs`, `UpdateClinicCommandValidator.cs`) — add, after the `TimeZoneId` rule:

```csharp
        RuleFor(x => x.PrintOrientation).IsInEnum();
```

`GetClinicByIdQueryHandler.cs` and `ListClinicsQueryHandler.cs` — wherever a `ClinicDto` is constructed, append `entity.PrintOrientation` (or `c.PrintOrientation`, matching the local variable name) as the final positional argument.

- [ ] **Step 8: Build and run the module's tests**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: build succeeds (zero warnings — warnings are errors).

Run: `dotnet test src/Tests/Administration.Tests`
Expected: PASS.

- [ ] **Step 9: Generate the migration**

The migration is generated from the **current build snapshot**, so the build in Step 8 must have succeeded first.

```bash
dotnet ef migrations add AddClinicPrintOrientation \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context AdministrationDbContext \
  --output-dir Administration
```

Open the generated `src/Host/FSH.Starter.Migrations.PostgreSQL/Administration/*_AddClinicPrintOrientation.cs` and confirm it only **adds** a non-nullable `PrintOrientation` text column with `defaultValue: "Portrait"` on `Clinics` — no drops, no renames. If the generated `defaultValue` is `""`, hand-edit it to `"Portrait"` so existing rows land on the intended default.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Administration src/Host/FSH.Starter.Migrations.PostgreSQL src/Tests/Administration.Tests
git commit -m "feat(admin): per-clinic report print orientation"
```

---

### Task 2: Orientation select in the dashboard's Clinics form

**Files:**
- Modify: `clients/dashboard/src/api/administration.ts` (`ClinicDto` ~line 114, `ClinicInput` ~line 139, and the create/update payload builders ~lines 182 and 200)
- Modify: `clients/dashboard/src/pages/administration/clinics.tsx` (form state ~line 352, payload ~line 411, the form body after the Time zone `Field` ~line 558)

**Interfaces:**
- Consumes: the backend `printOrientation` field on `ClinicDto` and both clinic commands (Task 1).
- Produces: `printOrientation: "Portrait" | "Landscape"` on the dashboard's `ClinicDto` / `ClinicInput`.

- [ ] **Step 1: Extend the API types**

In `clients/dashboard/src/api/administration.ts`:

Add above `ClinicDto`:

```ts
export type PrintOrientation = "Portrait" | "Landscape";
```

Add to the `ClinicDto` type (beside `timeZoneId`):

```ts
  printOrientation: PrintOrientation;
```

Add to the `ClinicInput` type:

```ts
  printOrientation?: PrintOrientation | null;
```

In both the create and the update payload builders (the objects that already carry `timeZoneId: input.timeZoneId ?? null`), add:

```ts
      printOrientation: input.printOrientation ?? "Portrait",
```

- [ ] **Step 2: Add the field to the clinic form**

In `clients/dashboard/src/pages/administration/clinics.tsx`, in the `initial` form state (beside `timeZoneId: clinic?.timeZoneId ?? "UTC"`):

```tsx
      printOrientation: clinic?.printOrientation ?? "Portrait",
```

In the submit payload (beside `timeZoneId: form.timeZoneId`):

```tsx
      printOrientation: form.printOrientation,
```

And directly after the Time zone `<Field>` block, add:

```tsx
            <Field
              id="clinic-print-orientation"
              label="Report print orientation"
              hint="Page orientation used when this clinic's patient reports are exported to PDF."
            >
              <select
                id="clinic-print-orientation"
                value={form.printOrientation}
                onChange={(e) =>
                  set("printOrientation", e.target.value as "Portrait" | "Landscape")
                }
                className="h-9 w-full rounded-lg border border-[var(--color-input)] bg-transparent px-2 text-[13px] shadow-xs focus-visible:border-[var(--color-ring)] focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[oklch(from_var(--color-ring)_l_c_h_/_0.5)]"
              >
                <option value="Portrait">Portrait</option>
                <option value="Landscape">Landscape</option>
              </select>
            </Field>
```

- [ ] **Step 3: Typecheck and lint**

Run: `cd clients/dashboard && npm run build`
Expected: succeeds with no TypeScript errors.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/api/administration.ts clients/dashboard/src/pages/administration/clinics.tsx
git commit -m "feat(dashboard): choose report print orientation per clinic"
```

---

### Task 3: Render models + legacy PDF layout

**Files:**
- Modify: `src/Modules/Patient/Modules.Patient/Services/IPatientReportPdfRenderer.cs`
- Modify: `src/Modules/Patient/Modules.Patient/Services/PatientReportPdfRenderer.cs`
- Test: `src/Tests/Patient.Tests/Services/PatientReportPdfRendererTests.cs`

**Interfaces:**
- Consumes: `PrintOrientation` from `FSH.Modules.Administration.Contracts.Dtos` (Task 1).
- Produces:
  - `ReportPdfPatientInfo(string FullName, string PatientCode, DateTime? DateOfBirth, string? Gender, DateTime? DateOfInitialVisit, DateTime? DateOfLoss, string? DiagnosisCodes)` — DOIV / DOL / DX appended; `DiagnosisCodes` is the pre-joined DX string (e.g. `"M54.5, M99.01"`).
  - `ReportPdfModel(… existing 14 members …, string? ClinicName, PrintOrientation Orientation, string? DepartmentName, string? ProviderName, DateTime? ModifiedOnUtc, byte[]? SignatureImage, byte[]? ReviewSignatureImage)` — seven new trailing members.
  - `IPatientReportPdfRenderer.Render(ReportPdfPatientInfo patient, IReadOnlyList<ReportPdfModel> reports)` — unchanged signature.

- [ ] **Step 1: Write the failing tests**

Replace the two model factories at the top of `src/Tests/Patient.Tests/Services/PatientReportPdfRendererTests.cs` and add the new facts. Full file:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.IO;
using Shouldly;

namespace Patient.Tests.Services;

public sealed class PatientReportPdfRendererTests
{
    private readonly PatientReportPdfRenderer _sut = new();

    private static ReportPdfPatientInfo SomePatient() =>
        new("Jane A Doe", "P-0001", new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc), "Female",
            DateOfInitialVisit: new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            DateOfLoss: new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            DiagnosisCodes: "M54.5, M99.01");

    private static ReportPdfModel SomeReport(
        string typeName = "Initial Evaluation",
        bool supportsVitals = true,
        bool isSigned = true,
        PrintOrientation orientation = PrintOrientation.Portrait) => new(
        ReportTypeName: typeName,
        ReportDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Version: 1,
        IsNoShow: false,
        WorkflowStatus: isSigned ? "Signed" : "Draft",
        IsSigned: isSigned,
        Vitals: new ReportVitalsDto(66m, 150m, 24.2m, 120, 80, 72, 98.6m),
        SupportsVitals: supportsVitals,
        Sections:
        [
            new ReportPdfSection("Subjective", "SOAP", "Patient reports lower back pain."),
            new ReportPdfSection("Objective", "SOAP", "Reduced lumbar range of motion."),
        ],
        SignedByName: "Dr. Smith",
        SignedOnUtc: new DateTime(2026, 6, 1, 15, 30, 0, DateTimeKind.Utc),
        ReviewSignedByName: null,
        ReviewSignedOnUtc: null,
        Addendums: [new ReportPdfAddendum("Dr. Smith", new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc), "Follow-up scheduled.")],
        ClinicName: "Main Street Clinic",
        Orientation: orientation,
        DepartmentName: "Chiropractic",
        ProviderName: "Dr. Smith",
        ModifiedOnUtc: null,
        SignatureImage: null,
        ReviewSignatureImage: null);

    /// <summary>Concatenates every text-showing operator in the page's content stream.</summary>
    private static string TextOf(PdfPage page)
    {
        var text = new System.Text.StringBuilder();
        AppendText(ContentReader.ReadContent(page), text);
        return text.ToString();

        static void AppendText(CSequence sequence, System.Text.StringBuilder into)
        {
            foreach (CObject obj in sequence)
            {
                switch (obj)
                {
                    case COperator op:
                        foreach (CObject operand in op.Operands)
                        {
                            if (operand is CString s)
                            {
                                into.Append(s.Value).Append(' ');
                            }
                        }

                        break;
                    case CSequence nested:
                        AppendText(nested, into);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    private static PdfDocument Open(byte[] pdf)
    {
        var stream = new MemoryStream(pdf);
        return PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    }

    [Fact]
    public void Render_Should_Produce_Pdf_For_SingleReport()
    {
        byte[] result = _sut.Render(SomePatient(), [SomeReport()]);

        result.ShouldNotBeEmpty();
        // Every PDF starts with the %PDF- magic marker.
        result[..5].ShouldBe("%PDF-"u8.ToArray());
    }

    [Fact]
    public void Render_Should_Use_Letter_Portrait_By_Default()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport()]));

        PdfPage page = doc.Pages[0];
        // US Letter is 8.5in x 11in = 612pt x 792pt.
        page.Width.Point.ShouldBe(612, tolerance: 1);
        page.Height.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public void Render_Should_Use_Landscape_When_The_Reports_Clinic_Is_Landscape()
    {
        using PdfDocument doc = Open(
            _sut.Render(SomePatient(), [SomeReport(orientation: PrintOrientation.Landscape)]));

        PdfPage page = doc.Pages[0];
        page.Width.Point.ShouldBe(792, tolerance: 1);
        page.Height.Point.ShouldBe(612, tolerance: 1);
    }

    [Fact]
    public void Render_Should_Honour_Each_Reports_Own_Orientation_In_One_Document()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(),
        [
            SomeReport(orientation: PrintOrientation.Portrait),
            SomeReport(typeName: "Progress", orientation: PrintOrientation.Landscape),
        ]));

        doc.Pages[0].Width.Point.ShouldBe(612, tolerance: 1);
        doc.Pages[^1].Width.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public void Render_Should_Print_The_Legacy_Header_Fields()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport()]));

        string text = TextOf(doc.Pages[0]);
        text.ShouldContain("Main Street Clinic");
        text.ShouldContain("Initial Evaluation");
        text.ShouldContain("Jane A Doe");
        text.ShouldContain("DOIV");
        text.ShouldContain("DOL");
        text.ShouldContain("M54.5");
    }

    [Fact]
    public void Render_Should_Print_The_Digitally_Signed_Line()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport()]));

        TextOf(doc.Pages[0]).ShouldContain("digitally signed by");
    }

    [Fact]
    public void Render_Should_Watermark_An_Unsigned_Report()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport(isSigned: false)]));

        TextOf(doc.Pages[0]).ShouldContain("DRAFT");
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Tests/Patient.Tests --filter FullyQualifiedName~PatientReportPdfRendererTests`
Expected: FAIL — compile error, `ReportPdfPatientInfo` / `ReportPdfModel` have no such members.

- [ ] **Step 3: Extend the render models**

In `src/Modules/Patient/Modules.Patient/Services/IPatientReportPdfRenderer.cs`, add `using FSH.Modules.Administration.Contracts.Dtos;` and replace the two records:

```csharp
/// <summary>Patient banner shown on every exported report page (legacy BackChart template:
/// Patient / DOB / DOIV / DOL / DX).</summary>
public sealed record ReportPdfPatientInfo(
    string FullName,
    string PatientCode,
    DateTime? DateOfBirth,
    string? Gender,
    DateTime? DateOfInitialVisit,
    DateTime? DateOfLoss,
    string? DiagnosisCodes);
```

and append seven members to `ReportPdfModel` (keep its existing XML doc comment):

```csharp
public sealed record ReportPdfModel(
    string ReportTypeName,
    DateTime ReportDate,
    int Version,
    bool IsNoShow,
    string WorkflowStatus,
    bool IsSigned,
    ReportVitalsDto Vitals,
    bool SupportsVitals,
    IReadOnlyList<ReportPdfSection> Sections,
    string? SignedByName,
    DateTime? SignedOnUtc,
    string? ReviewSignedByName,
    DateTime? ReviewSignedOnUtc,
    IReadOnlyList<ReportPdfAddendum> Addendums,
    string? ClinicName,
    PrintOrientation Orientation,
    string? DepartmentName,
    string? ProviderName,
    DateTime? ModifiedOnUtc,
    byte[]? SignatureImage,
    byte[]? ReviewSignatureImage);
```

- [ ] **Step 4: Rewrite the renderer to the legacy layout**

Replace the body of `Render` in `src/Modules/Patient/Modules.Patient/Services/PatientReportPdfRenderer.cs` (keep the class, the `Culture` field, the static ctor setting the QuestPDF Community licence, and `FormatDate` / `FormatDateTime`). Add `using FSH.Modules.Administration.Contracts.Dtos;`.

```csharp
    public byte[] Render(ReportPdfPatientInfo patient, IReadOnlyList<ReportPdfModel> reports)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.Count == 0)
        {
            throw new ArgumentException("At least one report is required.", nameof(reports));
        }

        return Document.Create(container =>
        {
            foreach (ReportPdfModel report in reports)
            {
                container.Page(page =>
                {
                    // Legacy BCFileGeneration used Letter; orientation is the report's clinic setting.
                    page.Size(report.Orientation == PrintOrientation.Landscape
                        ? PageSizes.Letter.Landscape()
                        : PageSizes.Letter.Portrait());
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                    // An unsigned report is not a finalised clinical record. It can still be
                    // printed, but every page says so — a printout that outlives the draft must
                    // not read as the signed note.
                    if (!report.IsSigned)
                    {
                        page.Foreground()
                            .AlignCenter()
                            .AlignMiddle()
                            .Rotate(-45)
                            .Text("DRAFT — UNSIGNED")
                            .FontSize(60).Bold()
                            .FontColor(Colors.Red.Lighten4);
                    }

                    page.Header().Element(c => ComposeHeader(c, patient, report));
                    page.Content().PaddingVertical(12).Element(c => ComposeBody(c, report));
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium));
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    /// <summary>Repeats on every page of a report — the legacy PDF template: clinic identity, the
    /// report title, and the patient block (Patient / DOB / DOIV / DOL / DX). The logo slot legacy
    /// drew top-left is intentionally empty: clinic-app has no logo storage yet.</summary>
    private static void ComposeHeader(IContainer container, ReportPdfPatientInfo patient, ReportPdfModel report)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(report.ClinicName ?? string.Empty)
                .FontSize(13).Bold();
            col.Item().AlignCenter().Text(report.ReportTypeName).FontSize(12).Bold();

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Patient: ").SemiBold();
                    t.Span(patient.FullName);
                });
                row.ConstantItem(150).Text(t =>
                {
                    t.Span("DOB: ").SemiBold();
                    t.Span(patient.DateOfBirth is { } dob ? FormatDate(dob) : "—");
                });
                row.ConstantItem(120).Text(t =>
                {
                    t.Span("Code: ").SemiBold();
                    t.Span(patient.PatientCode);
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("DOIV: ").SemiBold();
                    t.Span(patient.DateOfInitialVisit is { } doiv ? FormatDate(doiv) : "—");
                });
                row.RelativeItem().Text(t =>
                {
                    t.Span("DOL: ").SemiBold();
                    t.Span(patient.DateOfLoss is { } dol ? FormatDate(dol) : "—");
                });
            });

            col.Item().Text(t =>
            {
                t.Span("DX: ").SemiBold();
                t.Span(string.IsNullOrWhiteSpace(patient.DiagnosisCodes) ? "—" : patient.DiagnosisCodes);
            });

            col.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
        });
    }

    /// <summary>The legacy report body: date line, then fields grouped category → field → text,
    /// with vitals rendered inline under the Clinical Exam category, then addendums, then the
    /// signature images and their "digitally signed by" lines.</summary>
    private static void ComposeBody(IContainer container, ReportPdfModel report)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Text(t =>
            {
                t.Span(FormatDate(report.ReportDate)).SemiBold();
                if (report.ModifiedOnUtc is { } modified)
                {
                    t.Span($"  (Modified: {FormatDate(modified)})").FontColor(Colors.Grey.Darken1);
                }

                string who = report.ProviderName ?? report.DepartmentName ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(who))
                {
                    t.Span($"        {who}:");
                }
            });

            if (report.IsNoShow)
            {
                col.Item().Text("NO SHOW").FontColor(Colors.Red.Darken2).SemiBold();
            }

            string? lastCategory = null;
            foreach (ReportPdfSection section in report.Sections)
            {
                string category = section.Category ?? string.Empty;
                if (!string.Equals(category, lastCategory, StringComparison.Ordinal))
                {
                    col.Item().PaddingTop(4).Text(category).FontSize(12).Bold();
                    lastCategory = category;

                    // Legacy printed vitals at the head of the Clinical Exam category.
                    if (report.SupportsVitals
                        && string.Equals(category, VitalsCategory, StringComparison.OrdinalIgnoreCase)
                        && HasAnyVital(report.Vitals))
                    {
                        col.Item().Element(c => ComposeVitals(c, report.Vitals));
                    }
                }

                if (string.IsNullOrWhiteSpace(section.Text))
                {
                    continue;
                }

                col.Item().Column(c =>
                {
                    c.Item().Text(section.Name).SemiBold();
                    c.Item().Text(section.Text);
                });
            }

            foreach (ReportPdfAddendum addendum in report.Addendums)
            {
                col.Item().PaddingTop(4).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Addendum ").Bold();
                        t.Span($"({addendum.CreatedByName ?? "Unknown"} — {FormatDateTime(addendum.CreatedAtUtc)})")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    c.Item().Text(addendum.Text);
                });
            }

            if (report.SignedByName is not null || report.SignedOnUtc is not null)
            {
                col.Item().PaddingTop(10).Element(c => ComposeSignature(
                    c, report.SignatureImage, report.SignedByName, report.SignedOnUtc));
            }

            if (report.ReviewSignedByName is not null || report.ReviewSignedOnUtc is not null)
            {
                col.Item().PaddingTop(6).Element(c => ComposeSignature(
                    c, report.ReviewSignatureImage, report.ReviewSignedByName, report.ReviewSignedOnUtc));
            }
        });
    }

    /// <summary>Signature image (when the stored file was readable) above the legacy attestation
    /// line. A missing image degrades to the line alone — the attestation is the record, the
    /// picture is decoration.</summary>
    private static void ComposeSignature(IContainer container, byte[]? image, string? name, DateTime? signedOn)
    {
        container.Column(col =>
        {
            if (image is { Length: > 0 })
            {
                col.Item().Height(40).Image(image).FitHeight();
            }

            col.Item().Text(
                $"(This report was digitally signed by {name ?? "—"} on {FormatDateTime(signedOn)})")
                .Italic();
        });
    }

    /// <summary>Legacy report category that carries vitals (rcID 8).</summary>
    private const string VitalsCategory = "Clinical Exam";
```

Then replace `ComposeVitals` so vitals print as legacy label/value lines rather than a bordered box:

```csharp
    private static void ComposeVitals(IContainer container, ReportVitalsDto vitals)
    {
        container.PaddingBottom(4).Column(col =>
        {
            AddVital(col, "Height", vitals.HeightInches is { } h ? $"{h.ToString("0.##", Culture)} in." : null);
            AddVital(col, "Weight", vitals.WeightLbs is { } w ? $"{w.ToString("0.##", Culture)} lbs." : null);
            AddVital(col, "BMI", vitals.Bmi?.ToString("0.##", Culture));
            AddVital(col, "BP", vitals.Systolic is null && vitals.Diastolic is null
                ? null
                : $"{vitals.Systolic?.ToString(Culture) ?? "—"}/{vitals.Diastolic?.ToString(Culture) ?? "—"}");
            AddVital(col, "Heart Rate", vitals.Pulse?.ToString(Culture));
            AddVital(col, "Temperature", vitals.TemperatureF is { } t ? $"{t.ToString("0.#", Culture)} °F" : null);
        });
    }

    private static void AddVital(ColumnDescriptor col, string label, string? value)
    {
        if (value is null)
        {
            return;
        }

        col.Item().Text(t =>
        {
            t.Span($"{label}: ").SemiBold();
            t.Span(value);
        });
    }
```

Delete the now-unused `RowDescriptor`-based `AddVital` overload and any `using` that is no longer referenced. Keep `HasAnyVital`, `FormatDate`, `FormatDateTime` as they are.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test src/Tests/Patient.Tests --filter FullyQualifiedName~PatientReportPdfRendererTests`
Expected: PASS (all facts, including the three orientation facts).

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Services src/Tests/Patient.Tests/Services/PatientReportPdfRendererTests.cs
git commit -m "feat(reports): render the report PDF in the legacy BackChart layout"
```

---

### Task 4: Export handler loads clinic, incident, DX, provider/department and signature images

**Files:**
- Modify: `src/Modules/Patient/Modules.Patient/Features/v1/PatientReports/ExportPatientReportsPdf/ExportPatientReportsPdfQueryHandler.cs`
- Test: `src/Tests/Patient.Tests/Features/ExportPatientReportsPdfHandlerTests.cs` (create)

**Interfaces:**
- Consumes: `ReportPdfPatientInfo` / `ReportPdfModel` (Task 3); `PrintOrientation`, `ClinicDto`, `GetClinicByIdQuery`, `CustomDiagnosticDto`, `ListCustomDiagnosticsQuery`, `ProviderDto`, `GetProviderByIdQuery`, `DepartmentDto`, `GetDepartmentByIdQuery` (Administration.Contracts); `IPatientDocumentStorage.ReadAsync`.
- Produces: the handler's constructor gains a fifth dependency — `IPatientDocumentStorage storage` — appended after `IPdfPasswordProtector passwordProtector` and before `IAuditPublisher auditPublisher`.

- [ ] **Step 1: Write the failing tests**

Create `src/Tests/Patient.Tests/Features/ExportPatientReportsPdfHandlerTests.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;
using FSH.Modules.Patient.Services;
using Mediator;
using NSubstitute;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

/// <summary>
/// The export renders the legacy BackChart report: the page orientation comes from the report's
/// clinic, and the header needs incident context (DOIV / DOL / DX) that lives outside the report row.
/// </summary>
public sealed class ExportPatientReportsPdfHandlerTests
{
    private const int ReportTypeId = 7;

    private static readonly Guid ClinicId = Guid.NewGuid();
    private static readonly Guid DiagnosticId = Guid.NewGuid();

    private static IMediator Mediator(PrintOrientation orientation)
    {
        var mediator = Substitute.For<IMediator>();

        // CA2012: NSubstitute's arrange syntax records an expectation; the lambda builds a fresh
        // ValueTask per invocation, so nothing is awaited twice.
#pragma warning disable CA2012
        mediator.Send(Arg.Any<ListReportTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IReadOnlyList<ReportTypeDto>>(
                new[] { new ReportTypeDto(ReportTypeId, "Initial Evaluation", 1, true, DateTime.UtcNow, null) }));

        mediator.Send(Arg.Any<ListReportFieldsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IReadOnlyList<ReportFieldDto>>(
                new[] { new ReportFieldDto(1, ReportTypeId, "Subjective", "SOAP", 1, true, null) }));

        mediator.Send(Arg.Any<GetClinicByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ClinicDto>(new ClinicDto(
                ClinicId, "C1", "Main Street Clinic", "1 Main St", null, "Springfield", "IL",
                "62701", null, "UTC", true, DateTime.UtcNow, null, orientation)));

        // PagedResponse<T> is a class with init-only properties — build it with an initializer.
        mediator.Send(Arg.Any<ListCustomDiagnosticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<PagedResponse<CustomDiagnosticDto>>(
                new PagedResponse<CustomDiagnosticDto>
                {
                    Items = new[]
                    {
                        new CustomDiagnosticDto(DiagnosticId, "M54.5", "Low back pain", null, false, true, DateTime.UtcNow, null),
                    },
                    PageNumber = 1,
                    PageSize = 200,
                    TotalCount = 1,
                }));

        mediator.Send(Arg.Any<GetProviderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ProviderDto>(new ProviderDto(
                Guid.NewGuid(), "Ada", "Smith", "Dr.", null, null, null, null, ClinicId,
                "Main Street Clinic", null, true, DateTime.UtcNow, null)));

        mediator.Send(Arg.Any<GetDepartmentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<DepartmentDto>(new DepartmentDto(
                Guid.NewGuid(), "Chiropractic", 1, true, DateTime.UtcNow, null)));
#pragma warning restore CA2012
        return mediator;
    }

    private static async Task<PatientReport> Seed(PatientDbContext db, Guid? clinicId)
    {
        // Same minimal patient the other handler suites seed (see PatientAllergyHandlerTests).
        FSH.Modules.Patient.Domain.Patient patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-0001", true,
            PatientDemographics.Create(
                "Jane", "Doe", "A",
                new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                "F", null, isMinor: false,
                null, null, null, null, null, null, null),
            PatientContact.Create(null, null, null, null, null, null, null, null, null, null),
            PatientPhi.Create(null, null, null),
            null, null, null, null,
            hasNoKnownProblems: false,
            hasNoKnownMedications: false,
            hasNoKnownAllergies: false,
            receivesEmailReminders: false,
            lastVisitDate: null, nextVisitDate: null);
        db.Patients.Add(patient);

        PatientIncident incident = PatientIncident.Create(
            patient.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            isTransfer: false,
            isAccident: false,
            accidentType: null,
            accidentState: null,
            comments: null);
        incident.SetDiagnostics([DiagnosticId]);
        db.PatientIncidents.Add(incident);

        PatientReport report = PatientReport.Create(
            incident.Id, patient.Id, ReportTypeId, DateTime.UtcNow.Date, providerId: Guid.NewGuid(),
            clinicId: clinicId, appointmentId: null, isNoShow: false);
        db.PatientReports.Add(report);

        await db.SaveChangesAsync();
        return report;
    }

    private static ExportPatientReportsPdfQueryHandler Sut(PatientDbContext db, IMediator mediator) =>
        new(db, mediator, new PatientReportPdfRenderer(), new PdfPasswordProtector(),
            Substitute.For<IPatientDocumentStorage>(), Substitute.For<IAuditPublisher>());

    [Fact]
    public async Task Export_Should_Use_The_Reports_Clinic_Orientation()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await Seed(db, ClinicId);
        ExportPatientReportsPdfQueryHandler sut = Sut(db, Mediator(PrintOrientation.Landscape));

        ExportedReportsPdfDto result = await sut.Handle(
            new ExportPatientReportsPdfQuery([report.Id], null), CancellationToken.None);

        using var stream = new MemoryStream(result.Content);
        using PdfDocument doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        doc.Pages[0].Width.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public async Task Export_Should_Fall_Back_To_Portrait_When_The_Report_Has_No_Clinic()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await Seed(db, clinicId: null);
        ExportPatientReportsPdfQueryHandler sut = Sut(db, Mediator(PrintOrientation.Landscape));

        ExportedReportsPdfDto result = await sut.Handle(
            new ExportPatientReportsPdfQuery([report.Id], null), CancellationToken.None);

        using var stream = new MemoryStream(result.Content);
        using PdfDocument doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        doc.Pages[0].Width.Point.ShouldBe(612, tolerance: 1);
    }
}
```

Before running: open `src/Tests/Patient.Tests/Features/SuperBillHandlerTests.cs` and copy its **existing** patient-seeding helper into the `Seed` method above (replacing the `Domain.Patient.Create(/* … */)` placeholder), and confirm the real `ExportPatientReportsPdfQuery` and `PatientReport.Create` parameter lists match what is written here — adjust the test to the real signatures rather than changing production signatures to fit the test. Also confirm `PagedResponse<T>`'s constructor shape and `ReportTypeDto` / `ReportFieldDto` / `ProviderDto` / `DepartmentDto` positional arguments against their definitions in `Modules.Administration.Contracts/Dtos/`.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Tests/Patient.Tests --filter FullyQualifiedName~ExportPatientReportsPdfHandlerTests`
Expected: FAIL — the handler's constructor takes no `IPatientDocumentStorage`, and the render models are not populated with clinic/incident data.

- [ ] **Step 3: Enrich the handler**

In `ExportPatientReportsPdfQueryHandler.cs`:

Add the usings:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Contracts.v1.Providers;
```

Take the storage dependency:

```csharp
public sealed class ExportPatientReportsPdfQueryHandler(
    PatientDbContext dbContext,
    IMediator mediator,
    IPatientReportPdfRenderer renderer,
    IPdfPasswordProtector passwordProtector,
    IPatientDocumentStorage storage,
    IAuditPublisher auditPublisher)
    : IQueryHandler<ExportPatientReportsPdfQuery, ExportedReportsPdfDto>
```

After the existing `fieldsByType` loop, load the incident, its DX codes, and the clinics/providers/departments the reports reference:

```csharp
        // The legacy header is incident-scoped: DOIV, DOL and the DX list. All reports in an export
        // belong to one patient; take the incident of the first report.
        Domain.PatientIncident? incident = await dbContext.PatientIncidents
            .Include(i => i.Diagnostics)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == reports[0].IncidentId && !i.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        string? diagnosisCodes = null;
        if (incident is not null && incident.Diagnostics.Count > 0)
        {
            // Incident diagnostics reference CustomDiagnostic rows (Guid keys) in Administration.
            List<Guid> diagnosticIds = incident.Diagnostics.Select(d => d.DiagnosticId).ToList();
            PagedResponse<CustomDiagnosticDto> dx = await mediator
                .Send(new ListCustomDiagnosticsQuery(Ids: diagnosticIds, PageSize: 200), cancellationToken)
                .ConfigureAwait(false);
            diagnosisCodes = dx.Items.Count == 0
                ? null
                : string.Join(", ", dx.Items.Select(d => d.Code));
        }

        var clinicNames = new Dictionary<Guid, string>();
        var orientations = new Dictionary<Guid, PrintOrientation>();
        foreach (Guid clinicId in reports.Where(r => r.ClinicId is not null)
                     .Select(r => r.ClinicId!.Value).Distinct())
        {
            ClinicDto clinic = await mediator
                .Send(new GetClinicByIdQuery(clinicId), cancellationToken).ConfigureAwait(false);
            clinicNames[clinicId] = clinic.Name;
            orientations[clinicId] = clinic.PrintOrientation;
        }

        var providerNames = new Dictionary<Guid, string>();
        foreach (Guid providerId in reports.Where(r => r.ProviderId is not null)
                     .Select(r => r.ProviderId!.Value).Distinct())
        {
            ProviderDto provider = await mediator
                .Send(new GetProviderByIdQuery(providerId), cancellationToken).ConfigureAwait(false);
            providerNames[providerId] = string.Join(" ", new[] { provider.Prefix, provider.FirstName, provider.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        string? departmentName = null;
        if (incident?.DepartmentId is { } departmentId)
        {
            DepartmentDto department = await mediator
                .Send(new GetDepartmentByIdQuery(departmentId), cancellationToken).ConfigureAwait(false);
            departmentName = department.Name;
        }
```

Replace the `models` projection (`BuildModel` is no longer a pure static — signature images need an await, so build the list in a loop):

```csharp
        // Legacy grid ordered exports by report date ascending.
        var models = new List<ReportPdfModel>();
        foreach (Domain.PatientReport report in reports
                     .OrderBy(r => r.ReportDate)
                     .ThenBy(r => r.CreatedAtUtc))
        {
            byte[]? signature = await ReadSignature(report.SignatureImagePath, cancellationToken)
                .ConfigureAwait(false);
            byte[]? reviewSignature = await ReadSignature(report.ReviewSignatureImagePath, cancellationToken)
                .ConfigureAwait(false);

            models.Add(BuildModel(
                report,
                typeNames,
                fieldsByType,
                report.ClinicId is { } cid && clinicNames.TryGetValue(cid, out string? cname) ? cname : null,
                report.ClinicId is { } oid && orientations.TryGetValue(oid, out PrintOrientation o)
                    ? o
                    : PrintOrientation.Portrait,
                departmentName,
                report.ProviderId is { } pid && providerNames.TryGetValue(pid, out string? pname) ? pname : null,
                signature,
                reviewSignature));
        }
```

Add the signature reader (a missing file is not an error — the attestation line still prints):

```csharp
    private async Task<byte[]?> ReadSignature(string? storedPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            return null;
        }

        return await storage.ReadAsync(storedPath, cancellationToken).ConfigureAwait(false);
    }
```

Extend `patientInfo` with the incident context:

```csharp
        var patientInfo = new ReportPdfPatientInfo(
            string.Join(" ", new[]
            {
                patient.Demographics.FirstName,
                patient.Demographics.MiddleInitial,
                patient.Demographics.LastName,
            }.Where(s => !string.IsNullOrWhiteSpace(s))),
            patient.PatientCode,
            patient.Demographics.DateOfBirth,
            patient.Demographics.Gender,
            incident?.DateOfInitialVisit,
            incident?.DateOfLoss,
            diagnosisCodes);
```

Finally extend `BuildModel`'s signature and its `return`:

```csharp
    private static ReportPdfModel BuildModel(
        Domain.PatientReport report,
        Dictionary<int, string> typeNames,
        Dictionary<int, IReadOnlyList<ReportFieldDto>> fieldsByType,
        string? clinicName,
        PrintOrientation orientation,
        string? departmentName,
        string? providerName,
        byte[]? signatureImage,
        byte[]? reviewSignatureImage)
```

and append the seven new arguments to the `new ReportPdfModel(…)` it returns, after `Addendums`:

```csharp
            report.Addendums
                .OrderBy(a => a.CreatedAtUtc)
                .Select(a => new ReportPdfAddendum(a.CreatedByName, a.CreatedAtUtc, a.Text))
                .ToList(),
            clinicName,
            orientation,
            departmentName,
            providerName,
            report.UpdatedAtUtc,
            signatureImage,
            reviewSignatureImage);
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Tests/Patient.Tests --filter FullyQualifiedName~ExportPatientReportsPdfHandlerTests`
Expected: PASS (2 tests).

- [ ] **Step 5: Run the whole backend suite**

Run: `dotnet test src/FSH.Starter.slnx`
Expected: PASS — in particular the existing export tests (SHA-256, password protection, PHI audit, same-patient guard) and `Architecture.Tests`.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Patient src/Tests/Patient.Tests
git commit -m "feat(reports): export the PDF with clinic, incident and signature context"
```

---

### Task 5: Verify end to end and document

**Files:**
- Modify: `docs/patient-reports.md`

- [ ] **Step 1: Run the stack and export a report**

Run: `dotnet run --project src/Host/FSH.Starter.AppHost`

In the dashboard (http://localhost:5174): Administration → Clinics → edit a clinic → set **Report print orientation** to Landscape → save. Then open a patient chart whose reports belong to that clinic → Export Reports → select a report → Export PDF.

Confirm in the downloaded PDF: landscape Letter pages; the clinic name and report type at the top; Patient / DOB / Code and DOIV / DOL / DX beneath; category headings with the field text under them; vitals listed under Clinical Exam; `N of M` at the bottom; the signature image and "(This report was digitally signed by …)" for a signed report; the DRAFT watermark on an unsigned one. Switch the clinic back to Portrait and re-export to confirm the pages flip.

- [ ] **Step 2: Document it**

In `docs/patient-reports.md`, add a section describing the exported PDF's layout (the legacy BackChart parity list above), and state that page orientation is a per-clinic setting on Administration → Clinics, defaulting to Portrait, resolved per report from the report's clinic (Portrait when the report has no clinic). Note the clinic-logo slot is intentionally empty pending logo storage.

- [ ] **Step 3: Commit**

```bash
git add docs/patient-reports.md
git commit -m "docs: report PDF format and per-clinic print orientation"
```
