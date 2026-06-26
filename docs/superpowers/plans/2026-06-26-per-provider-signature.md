# Per-Provider Signature + Reusable Signature Editor — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give each Administration `Provider` a reusable signature image (set via an upload-crop-or-draw editor dialog in the provider admin record), stored via `IStorageService`, ready to be snapshotted onto reports when the Patient Reports sprint wires signing.

**Architecture:** Backend adds a `SignatureImagePath` to the Administration `Provider` entity plus set/clear commands that upload a PNG through `IStorageService` (`FileType.Image`). The Contracts `ProviderDto` gains `SignatureImagePath` + a derived `SignatureImageUrl` (built with `BuildPublicUrl`). The dashboard gets a presentational `SignatureEditor` dialog (two modes: upload+crop, draw → emits PNG base64) wired into the existing `ProviderEditorDialog` edit mode.

**Tech Stack:** .NET 10, EF Core 10 (PostgreSQL), Mediator 3.x, FluentValidation, xUnit + Shouldly; React 19 + Vite + TS, TanStack Query v5, Radix Dialog, `react-cropper` (cropperjs), Tailwind v4.

## Global Constraints

- **Module boundary:** this plan stays entirely inside the **Administration** module + the **dashboard** app. No Patient-module changes here (the R6/R8 snapshot logic lives in the Patient Reports sprint).
- **Mediator handlers:** `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` on every await, propagate `CancellationToken`.
- **Every command handler + paginated query handler needs a `{Name}Validator`** (Architecture.Tests enforces).
- **`src/BuildingBlocks` is read-only** — do not modify it; only consume `IStorageService`.
- **Do not modify `BaseDbContext` semantics**; `Provider` is already tenant-scoped.
- **Build runs with `TreatWarningsAsErrors`** — warnings fail the build. File-scoped namespaces, explicit types, `is null`/`is not null`, `ArgumentNullException.ThrowIfNull` guards.
- **Frontend golden rule #9:** pass per-call data through `mutate(arg)`, never via closed-over state.
- **Storage key convention:** `administration/providers/{providerId}/signature.png`. `FileType.Image` (5 MB cap, extension enforced by `FileTypeMetadata`).
- **Permission:** reuse `AdministrationPermissions.Providers.Update` for both set and clear — no new permission.
- **Migration command** (run a full build FIRST so the snapshot is current):
  ```bash
  dotnet ef migrations add AddProviderSignature \
    --project src/Host/FSH.Starter.Migrations.PostgreSQL \
    --startup-project src/Host/FSH.Starter.Api \
    --context AdministrationDbContext \
    --output-dir Administration
  ```

---

### Task 1: Provider domain — `SignatureImagePath` + set/clear

**Files:**
- Modify: `src/Modules/Administration/Modules.Administration/Domain/Provider.cs`
- Test: `src/Tests/Administration.Tests/Domain/ProviderTests.cs`

**Interfaces:**
- Produces: `Provider.SignatureImagePath` (`string?`, private setter); `Provider.SetSignature(string path)`; `Provider.ClearSignature()`. Both stamp `UpdatedAtUtc`.

- [ ] **Step 1: Write the failing tests** — append to `ProviderTests.cs` (inside the existing class):

```csharp
    [Fact]
    public void SetSignature_Should_StorePath_And_StampUpdatedAt()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);

        provider.SetSignature("administration/providers/abc/signature.png");

        provider.SignatureImagePath.ShouldBe("administration/providers/abc/signature.png");
        provider.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSignature_Should_Throw_When_PathBlank(string path)
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);

        Should.Throw<ArgumentException>(() => provider.SetSignature(path));
    }

    [Fact]
    public void ClearSignature_Should_NullPath_And_StampUpdatedAt()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);
        provider.SetSignature("administration/providers/abc/signature.png");

        provider.ClearSignature();

        provider.SignatureImagePath.ShouldBeNull();
        provider.UpdatedAtUtc.ShouldNotBeNull();
    }
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~ProviderTests"`
Expected: FAIL — `Provider` has no `SignatureImagePath` / `SetSignature` / `ClearSignature` (compile error).

- [ ] **Step 3: Add the property + methods to `Provider.cs`**

Add the property after the `UserId` property block (around line 32):

```csharp
    /// <summary>
    /// Storage key of this provider's signature image (PNG), or null when unset. Set via the
    /// Administration signature editor; snapshotted onto reports at signing time by the Patient module.
    /// </summary>
    public string? SignatureImagePath { get; private set; }
```

Add these methods just before `public void Delete(...)` (around line 109):

```csharp
    public void SetSignature(string signatureImagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureImagePath);
        SignatureImagePath = signatureImagePath.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ClearSignature()
    {
        SignatureImagePath = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
```

- [ ] **Step 4: Run the tests to verify they pass**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~ProviderTests"`
Expected: PASS (all ProviderTests, including the 3 new ones).

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Administration/Modules.Administration/Domain/Provider.cs src/Tests/Administration.Tests/Domain/ProviderTests.cs
git commit -m "feat(administration): provider signature path + set/clear domain methods"
```

---

### Task 2: EF config + `AddProviderSignature` migration

**Files:**
- Modify: `src/Modules/Administration/Modules.Administration/Data/Configurations/ProviderConfiguration.cs`
- Create (generated): `src/Host/FSH.Starter.Migrations.PostgreSQL/Administration/*_AddProviderSignature.cs` (+ Designer + snapshot update)

**Interfaces:**
- Produces: `Providers.SignatureImagePath` nullable `varchar(512)` column.

- [ ] **Step 1: Add the column mapping** — in `ProviderConfiguration.Configure`, after the `UserId` property line (line 21):

```csharp
        builder.Property(x => x.SignatureImagePath).HasMaxLength(512);
```

- [ ] **Step 2: Build so the migration snapshot is current**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: Build succeeded, 0 warnings, 0 errors.

- [ ] **Step 3: Generate the migration**

Run:
```bash
dotnet ef migrations add AddProviderSignature \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context AdministrationDbContext \
  --output-dir Administration
```
Expected: a new `*_AddProviderSignature.cs` under `.../Administration/`, plus an updated `AdministrationDbContextModelSnapshot.cs`.

- [ ] **Step 4: Verify the migration is a single additive column**

Read the generated `Up(...)`: it should be exactly one `AddColumn<string>(name: "SignatureImagePath", table: "Providers", type: "character varying(512)", maxLength: 512, nullable: true)` and `Down` a matching `DropColumn`. If it contains anything else, the snapshot was stale — `dotnet ef migrations remove` (same args), rebuild, regenerate.

- [ ] **Step 5: Build again to confirm the migrations project compiles**

Run: `dotnet build src/Host/FSH.Starter.Migrations.PostgreSQL`
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Administration/Modules.Administration/Data/Configurations/ProviderConfiguration.cs src/Host/FSH.Starter.Migrations.PostgreSQL/Administration
git commit -m "feat(administration): AddProviderSignature migration (Providers.SignatureImagePath)"
```

---

### Task 3: Contracts `ProviderDto` + query handler URL projection

**Files:**
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/ProviderDto.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/GetProviderById/GetProviderByIdQueryHandler.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/ListProviders/ListProvidersQueryHandler.cs`

**Interfaces:**
- Consumes: `IStorageService.BuildPublicUrl(string)` from `FSH.Framework.Storage.Services`.
- Produces: `ProviderDto.SignatureImagePath` (`string?`) and `ProviderDto.SignatureImageUrl` (`string?`) as the last two positional members. `GetProviderByIdQuery`/`ListProvidersQuery` populate both (URL null when path null).

- [ ] **Step 1: Extend `ProviderDto`** — replace the record body so the two new members are last:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record ProviderDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Prefix,
    string? Suffix,
    string? Specialty,
    string? Npi,
    string? KareoExternalId,
    Guid? PrimaryClinicId,
    string? PrimaryClinicName,
    string? UserId,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? SignatureImagePath = null,
    string? SignatureImageUrl = null);
```

(The trailing defaults keep existing positional call sites compiling; the query handlers below set them explicitly.)

- [ ] **Step 2: Build to find every broken `new ProviderDto(...)` call site**

Run: `dotnet build src/Modules/Administration/Modules.Administration`
Expected: Build succeeds (defaults absorb the new params) — but the two query handlers still need to populate the new fields. Proceed to wire them.

- [ ] **Step 3: Update `GetProviderByIdQueryHandler`** to inject storage and build the URL in memory:

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.GetProviderById;

public sealed class GetProviderByIdQueryHandler(AdministrationDbContext dbContext, IStorageService storage)
    : IQueryHandler<GetProviderByIdQuery, ProviderDto>
{
    public async ValueTask<ProviderDto> Handle(GetProviderByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ProviderDto? dto = await dbContext.Providers
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            .Select(p => new ProviderDto(
                p.Id, p.FirstName, p.LastName, p.Prefix, p.Suffix, p.Specialty,
                p.Npi, p.KareoExternalId, p.PrimaryClinicId,
                dbContext.Clinics.Where(c => c.Id == p.PrimaryClinicId).Select(c => c.Name).FirstOrDefault(),
                p.UserId, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc,
                p.SignatureImagePath, null))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (dto is null)
        {
            throw new NotFoundException($"Provider {query.Id} not found.");
        }

        return dto.SignatureImagePath is null
            ? dto
            : dto with { SignatureImageUrl = storage.BuildPublicUrl(dto.SignatureImagePath) };
    }
}
```

- [ ] **Step 4: Update `ListProvidersQueryHandler`** the same way — inject storage, project the path with a null URL placeholder, then map URLs after materialization. Change the constructor line to:

```csharp
public sealed class ListProvidersQueryHandler(AdministrationDbContext dbContext, IStorageService storage)
    : IQueryHandler<ListProvidersQuery, PagedResponse<ProviderDto>>
```

Add `using FSH.Framework.Storage.Services;` at the top. In the `.Select(...)` projection, change the trailing arguments from `p.UpdatedAtUtc))` to:

```csharp
                p.UserId, p.IsActive, p.CreatedAtUtc, p.UpdatedAtUtc,
                p.SignatureImagePath, null))
```

Then, immediately after the `.ToListAsync(...)` assignment to `items`, add:

```csharp
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].SignatureImagePath is { } path)
            {
                items[i] = items[i] with { SignatureImageUrl = storage.BuildPublicUrl(path) };
            }
        }
```

- [ ] **Step 5: Build to verify**

Run: `dotnet build src/Modules/Administration/Modules.Administration`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Administration/Modules.Administration.Contracts/Dtos/ProviderDto.cs src/Modules/Administration/Modules.Administration/Features/v1/Providers/GetProviderById/GetProviderByIdQueryHandler.cs src/Modules/Administration/Modules.Administration/Features/v1/Providers/ListProviders/ListProvidersQueryHandler.cs
git commit -m "feat(administration): expose provider signature path + public URL on ProviderDto"
```

---

### Task 4: `SetProviderSignatureCommand` (contract + validator + handler + endpoint)

**Files:**
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/Providers/SetProviderSignatureCommand.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/SetProviderSignature/SetProviderSignatureCommandValidator.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/SetProviderSignature/SetProviderSignatureCommandHandler.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/SetProviderSignature/SetProviderSignatureEndpoint.cs`
- Test: `src/Tests/Administration.Tests/Validators/SetProviderSignatureCommandValidatorTests.cs`

**Interfaces:**
- Produces: `SetProviderSignatureCommand(Guid ProviderId, string ImageBase64) : ICommand<string>` — returns the stored signature's public URL. Endpoint `PUT /providers/{id}/signature`, permission `Providers.Update`.

- [ ] **Step 1: Write the contract** — `SetProviderSignatureCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

/// <summary>
/// Upload (or replace) a provider's signature image. <see cref="ImageBase64"/> is a base64 PNG,
/// optionally with a <c>data:image/png;base64,</c> prefix. Returns the stored image's public URL.
/// </summary>
public sealed record SetProviderSignatureCommand(Guid ProviderId, string ImageBase64) : ICommand<string>;
```

- [ ] **Step 2: Write the failing validator test** — `SetProviderSignatureCommandValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

namespace Administration.Tests.Validators;

public sealed class SetProviderSignatureCommandValidatorTests
{
    private readonly SetProviderSignatureCommandValidator _sut = new();

    private static SetProviderSignatureCommand Valid() =>
        new(Guid.CreateVersion7(), "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ProviderIdEmpty()
    {
        _sut.TestValidate(Valid() with { ProviderId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ProviderId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_ImageBase64Blank(string image)
    {
        _sut.TestValidate(Valid() with { ImageBase64 = image })
            .ShouldHaveValidationErrorFor(x => x.ImageBase64);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~SetProviderSignatureCommandValidatorTests"`
Expected: FAIL — `SetProviderSignatureCommandValidator` does not exist (compile error).

- [ ] **Step 4: Write the validator** — `SetProviderSignatureCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

public sealed class SetProviderSignatureCommandValidator : AbstractValidator<SetProviderSignatureCommand>
{
    public SetProviderSignatureCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.ImageBase64).NotEmpty();
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~SetProviderSignatureCommandValidatorTests"`
Expected: PASS.

- [ ] **Step 6: Write the handler** — `SetProviderSignatureCommandHandler.cs`:

```csharp
using System.Globalization;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Storage;
using FSH.Framework.Storage.Services;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

public sealed class SetProviderSignatureCommandHandler(AdministrationDbContext dbContext, IStorageService storage)
    : ICommandHandler<SetProviderSignatureCommand, string>
{
    public async ValueTask<string> Handle(SetProviderSignatureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Provider entity = await dbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider {command.ProviderId} not found.");

        byte[] bytes = DecodePng(command.ImageBase64);
        string key = string.Create(CultureInfo.InvariantCulture, $"administration/providers/{command.ProviderId}/signature.png");

        var request = new FileUploadRequest
        {
            FileName = "signature.png",
            ContentType = "image/png",
            Data = [.. bytes]
        };

        string storedPath = await storage.UploadAsync<Domain.Provider>(request, FileType.Image, cancellationToken)
            .ConfigureAwait(false);

        entity.SetSignature(storedPath);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return storage.BuildPublicUrl(storedPath);
    }

    private static byte[] DecodePng(string imageBase64)
    {
        string payload = imageBase64;
        int comma = payload.IndexOf(',', StringComparison.Ordinal);
        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma >= 0)
        {
            payload = payload[(comma + 1)..];
        }

        try
        {
            return Convert.FromBase64String(payload);
        }
        catch (FormatException ex)
        {
            throw new FshException("Signature image is not valid base64.", [ex.Message]);
        }
    }
}
```

> **Note for the implementer:** `IStorageService.UploadAsync` derives the storage key from the request/filename + provider conventions; `key` above documents the intended layout. If the storage service signature in this repo takes the key explicitly (check `LocalStorageService`/`S3StorageService` for an overload), pass `key` through and use the returned path. If `FshException`'s constructor differs, use the matching exception type from `FSH.Framework.Core.Exceptions` for a 400-class error (look at how other handlers throw validation-style errors). Confirm both during Step 8's build.

- [ ] **Step 7: Write the endpoint** — `SetProviderSignatureEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

public static class SetProviderSignatureEndpoint
{
    internal static RouteHandlerBuilder MapSetProviderSignatureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/providers/{id:guid}/signature",
                async (Guid id, SetProviderSignatureCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetProviderSignatureCommand command = body with { ProviderId = id };
                    string url = await mediator.Send(command, ct);
                    return Results.Ok(url);
                })
            .WithName("SetProviderSignature")
            .WithSummary("Upload or replace a provider's signature image")
            .RequirePermission(AdministrationPermissions.Providers.Update);
    }
}
```

- [ ] **Step 8: Build**

Run: `dotnet build src/Modules/Administration/Modules.Administration`
Expected: Build succeeded, 0 warnings. (Resolve the storage-key / exception-type notes from Step 6 here if the build flags them.)

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Administration/Modules.Administration.Contracts/v1/Providers/SetProviderSignatureCommand.cs src/Modules/Administration/Modules.Administration/Features/v1/Providers/SetProviderSignature src/Tests/Administration.Tests/Validators/SetProviderSignatureCommandValidatorTests.cs
git commit -m "feat(administration): set provider signature command + endpoint"
```

---

### Task 5: `ClearProviderSignatureCommand` (contract + validator + handler + endpoint)

**Files:**
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/Providers/ClearProviderSignatureCommand.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/ClearProviderSignature/ClearProviderSignatureCommandValidator.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/ClearProviderSignature/ClearProviderSignatureCommandHandler.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Providers/ClearProviderSignature/ClearProviderSignatureEndpoint.cs`
- Test: `src/Tests/Administration.Tests/Validators/ClearProviderSignatureCommandValidatorTests.cs`

**Interfaces:**
- Produces: `ClearProviderSignatureCommand(Guid ProviderId) : ICommand<Unit>`. Endpoint `DELETE /providers/{id}/signature`, permission `Providers.Update`. Idempotent (no-op when no signature; tolerate a missing blob).

- [ ] **Step 1: Write the contract** — `ClearProviderSignatureCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

/// <summary>Remove a provider's signature image (idempotent — no-op when unset).</summary>
public sealed record ClearProviderSignatureCommand(Guid ProviderId) : ICommand<Unit>;
```

- [ ] **Step 2: Write the failing validator test** — `ClearProviderSignatureCommandValidatorTests.cs`:

```csharp
using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

namespace Administration.Tests.Validators;

public sealed class ClearProviderSignatureCommandValidatorTests
{
    private readonly ClearProviderSignatureCommandValidator _sut = new();

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(new ClearProviderSignatureCommand(Guid.CreateVersion7()))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ProviderIdEmpty()
    {
        _sut.TestValidate(new ClearProviderSignatureCommand(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.ProviderId);
    }
}
```

- [ ] **Step 3: Run the test to verify it fails**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~ClearProviderSignatureCommandValidatorTests"`
Expected: FAIL — validator does not exist (compile error).

- [ ] **Step 4: Write the validator** — `ClearProviderSignatureCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

public sealed class ClearProviderSignatureCommandValidator : AbstractValidator<ClearProviderSignatureCommand>
{
    public ClearProviderSignatureCommandValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
    }
}
```

- [ ] **Step 5: Run the test to verify it passes**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~ClearProviderSignatureCommandValidatorTests"`
Expected: PASS.

- [ ] **Step 6: Write the handler** — `ClearProviderSignatureCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Storage.Services;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

public sealed class ClearProviderSignatureCommandHandler(AdministrationDbContext dbContext, IStorageService storage)
    : ICommandHandler<ClearProviderSignatureCommand, Unit>
{
    public async ValueTask<Unit> Handle(ClearProviderSignatureCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Provider entity = await dbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == command.ProviderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider {command.ProviderId} not found.");

        if (entity.SignatureImagePath is { } path)
        {
            await storage.RemoveAsync(path, cancellationToken).ConfigureAwait(false);
            entity.ClearSignature();
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
```

- [ ] **Step 7: Write the endpoint** — `ClearProviderSignatureEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

public static class ClearProviderSignatureEndpoint
{
    internal static RouteHandlerBuilder MapClearProviderSignatureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/providers/{id:guid}/signature",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ClearProviderSignatureCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("ClearProviderSignature")
            .WithSummary("Remove a provider's signature image")
            .RequirePermission(AdministrationPermissions.Providers.Update);
    }
}
```

- [ ] **Step 8: Build**

Run: `dotnet build src/Modules/Administration/Modules.Administration`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Administration/Modules.Administration.Contracts/v1/Providers/ClearProviderSignatureCommand.cs src/Modules/Administration/Modules.Administration/Features/v1/Providers/ClearProviderSignature src/Tests/Administration.Tests/Validators/ClearProviderSignatureCommandValidatorTests.cs
git commit -m "feat(administration): clear provider signature command + endpoint"
```

---

### Task 6: Register the signature endpoints + full backend verification

**Files:**
- Modify: `src/Modules/Administration/Modules.Administration/AdministrationModule.cs:177-181`

**Interfaces:**
- Consumes: `MapSetProviderSignatureEndpoint`, `MapClearProviderSignatureEndpoint` from Tasks 4–5.

- [ ] **Step 1: Wire the endpoints** — in `AdministrationModule` `MapEndpoints`, the provider block currently reads:

```csharp
        group.MapCreateProviderEndpoint();
        group.MapListProvidersEndpoint();
        group.MapGetProviderByIdEndpoint();
        group.MapUpdateProviderEndpoint();
        group.MapDeleteProviderEndpoint();
```

Add the two literal sub-routes immediately after `MapDeleteProviderEndpoint()` (literal `/signature` routes are fine here — they don't collide with `/{id:guid}` because the segment count differs, but keep them grouped with the provider block):

```csharp
        group.MapCreateProviderEndpoint();
        group.MapListProvidersEndpoint();
        group.MapGetProviderByIdEndpoint();
        group.MapUpdateProviderEndpoint();
        group.MapSetProviderSignatureEndpoint();
        group.MapClearProviderSignatureEndpoint();
        group.MapDeleteProviderEndpoint();
```

Add the matching `using` lines at the top of the file (alongside the other feature usings):

```csharp
using FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;
using FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;
```

- [ ] **Step 2: Full solution build**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: Build succeeded, 0 warnings, 0 errors.

- [ ] **Step 3: Run the Administration + Architecture tests**

Run:
```bash
dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj
dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj
```
Expected: PASS. (Architecture.Tests confirms the two new command handlers each have a validator and respect module boundaries.)

- [ ] **Step 4: Apply the migration to the local tenants**

Run: `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply`
Expected: `AddProviderSignature` applies across tenant DBs (acme/globex); `Providers.SignatureImagePath` exists.

- [ ] **Step 5: Smoke-test via Scalar** (API running, real tenant token, `X-FSH-App: admin`):
  - `PUT /api/v1/administration/providers/{id}/signature` with body `{ "imageBase64": "<tiny base64 PNG>" }` → 200 + a URL string.
  - `GET /api/v1/administration/providers/{id}` → `signatureImagePath` + `signatureImageUrl` populated.
  - `DELETE /api/v1/administration/providers/{id}/signature` → 204; subsequent GET shows both null.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Administration/Modules.Administration/AdministrationModule.cs
git commit -m "feat(administration): register provider signature endpoints"
```

---

### Task 7: Dashboard API — `ProviderDto` fields + set/clear functions

**Files:**
- Modify: `clients/dashboard/src/api/administration.ts:292-307` (the `ProviderDto` type) and the Providers section (after `deleteProvider`, ~line 380)

**Interfaces:**
- Produces: `ProviderDto.signatureImagePath?: string | null`, `ProviderDto.signatureImageUrl?: string | null`; `setProviderSignature(providerId: string, imageBase64: string): Promise<string>`; `clearProviderSignature(providerId: string): Promise<void>`.

- [ ] **Step 1: Add the two fields to `ProviderDto`** — inside the `export type ProviderDto = { ... }` block, after `userId?: string | null;`:

```ts
  signatureImagePath?: string | null;
  signatureImageUrl?: string | null;
```

- [ ] **Step 2: Add the API functions** — immediately after `deleteProvider`:

```ts
/** Upload/replace a provider's signature image. Returns the stored image's public URL. */
export async function setProviderSignature(providerId: string, imageBase64: string): Promise<string> {
  return apiFetch<string>(`/api/v1/administration/providers/${encodeURIComponent(providerId)}/signature`, {
    method: "PUT",
    body: JSON.stringify({ providerId, imageBase64 }),
  });
}

/** Remove a provider's signature image (idempotent). */
export async function clearProviderSignature(providerId: string): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/providers/${encodeURIComponent(providerId)}/signature`, {
    method: "DELETE",
  });
}
```

- [ ] **Step 3: Type-check**

Run: `cd clients/dashboard && npx tsc --noEmit`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/api/administration.ts
git commit -m "feat(dashboard): provider signature fields + set/clear API"
```

---

### Task 8: Reusable `SignatureEditor` dialog component

**Files:**
- Create: `clients/dashboard/src/components/ui/signature-editor.tsx`
- Modify: `clients/dashboard/package.json` (add `react-cropper` + `cropperjs`)

**Interfaces:**
- Produces: `SignatureEditor` React component with props
  `{ open: boolean; onOpenChange: (open: boolean) => void; value?: string | null; onSave: (pngBase64: string) => void; targetWidth?: number; targetHeight?: number }`.
  `onSave` receives a full data URL (`data:image/png;base64,...`). Two modes via an internal segmented toggle: **Upload** (file pick → crop) and **Draw** (canvas pad + Clear). No new shared primitive — the toggle is two buttons bound to local state.

- [ ] **Step 1: Add the crop dependency**

Run: `cd clients/dashboard && npm install react-cropper cropperjs`
Expected: both added to `dependencies`.

- [ ] **Step 2: Create the component** — `signature-editor.tsx`:

```tsx
import { useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import Cropper, { type ReactCropperElement } from "react-cropper";
import "cropperjs/dist/cropper.css";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

type Mode = "upload" | "draw";

export type SignatureEditorProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Current signature image URL, shown as the starting preview. */
  value?: string | null;
  /** Receives the new signature as a PNG data URL ("data:image/png;base64,..."). */
  onSave: (pngBase64: string) => void;
  targetWidth?: number;
  targetHeight?: number;
};

export function SignatureEditor({
  open,
  onOpenChange,
  value,
  onSave,
  targetWidth = 180,
  targetHeight = 30,
}: SignatureEditorProps) {
  const [mode, setMode] = useState<Mode>("upload");
  const [uploadSrc, setUploadSrc] = useState<string | null>(null);
  const cropperRef = useRef<ReactCropperElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drawing = useRef(false);
  const hasStrokes = useRef(false);

  const aspect = targetWidth / targetHeight;

  const onFile = (file: File | undefined) => {
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => setUploadSrc(typeof reader.result === "string" ? reader.result : null);
    reader.readAsDataURL(file);
  };

  // ── Draw mode helpers ──
  const ctx = () => canvasRef.current?.getContext("2d") ?? null;
  const pos = (e: ReactPointerEvent<HTMLCanvasElement>) => {
    const rect = canvasRef.current!.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  };
  const onDown = (e: ReactPointerEvent<HTMLCanvasElement>) => {
    const c = ctx();
    if (!c) return;
    drawing.current = true;
    hasStrokes.current = true;
    const { x, y } = pos(e);
    c.beginPath();
    c.moveTo(x, y);
    canvasRef.current!.setPointerCapture(e.pointerId);
  };
  const onMove = (e: ReactPointerEvent<HTMLCanvasElement>) => {
    if (!drawing.current) return;
    const c = ctx();
    if (!c) return;
    const { x, y } = pos(e);
    c.lineWidth = 2;
    c.lineCap = "round";
    c.strokeStyle = "#111827";
    c.lineTo(x, y);
    c.stroke();
  };
  const onUp = () => {
    drawing.current = false;
  };
  const clearCanvas = () => {
    const c = ctx();
    if (c && canvasRef.current) c.clearRect(0, 0, canvasRef.current.width, canvasRef.current.height);
    hasStrokes.current = false;
  };

  const handleSave = () => {
    if (mode === "upload") {
      const cropper = cropperRef.current?.cropper;
      if (!cropper) return;
      const out = cropper.getCroppedCanvas({ width: targetWidth, height: targetHeight });
      onSave(out.toDataURL("image/png"));
    } else {
      if (!hasStrokes.current || !canvasRef.current) return;
      onSave(canvasRef.current.toDataURL("image/png"));
    }
    onOpenChange(false);
    reset();
  };

  const reset = () => {
    setUploadSrc(null);
    clearCanvas();
    setMode("upload");
  };

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset();
        onOpenChange(o);
      }}
    >
      <DialogContent className="!max-w-xl">
        <DialogHeader>
          <DialogTitle>Edit signature</DialogTitle>
        </DialogHeader>
        <DialogBody className="space-y-4">
          <div className="inline-flex rounded-lg border border-[var(--color-border)] p-0.5">
            <button
              type="button"
              onClick={() => setMode("upload")}
              className={`rounded-md px-3 py-1 text-[13px] font-medium ${mode === "upload" ? "bg-[var(--color-muted)] text-[var(--color-foreground)]" : "text-[var(--color-muted-foreground)]"}`}
            >
              Upload
            </button>
            <button
              type="button"
              onClick={() => setMode("draw")}
              className={`rounded-md px-3 py-1 text-[13px] font-medium ${mode === "draw" ? "bg-[var(--color-muted)] text-[var(--color-foreground)]" : "text-[var(--color-muted-foreground)]"}`}
            >
              Draw
            </button>
          </div>

          {mode === "upload" ? (
            <div className="space-y-3">
              <input
                type="file"
                accept="image/*"
                onChange={(e) => onFile(e.target.files?.[0])}
                className="block text-[13px]"
              />
              {uploadSrc ? (
                <Cropper
                  ref={cropperRef}
                  src={uploadSrc}
                  style={{ height: 240, width: "100%" }}
                  aspectRatio={aspect}
                  viewMode={1}
                  background={false}
                  autoCropArea={1}
                  responsive
                  guides
                />
              ) : value ? (
                <img src={value} alt="Current signature" className="h-16 border border-[var(--color-border)] object-contain" />
              ) : (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">Select an image to crop to the signature box.</p>
              )}
            </div>
          ) : (
            <div className="space-y-2">
              <canvas
                ref={canvasRef}
                width={540}
                height={120}
                onPointerDown={onDown}
                onPointerMove={onMove}
                onPointerUp={onUp}
                className="w-full touch-none rounded-md border border-[var(--color-border)] bg-white"
              />
              <Button type="button" variant="outline" size="sm" onClick={clearCanvas}>
                Clear
              </Button>
            </div>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button type="button" onClick={handleSave}>
            Save signature
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

> **Implementer note:** Confirm `Button` accepts a `size` prop in this repo's `components/ui/button.tsx`; if not, drop the `size="sm"`. Confirm `react-cropper`'s `ReactCropperElement` export name against the installed version — adjust the import if the type is named differently. The component must remain presentational (no API calls).

- [ ] **Step 3: Type-check + build**

Run: `cd clients/dashboard && npx tsc --noEmit && npm run build`
Expected: 0 TS errors; build succeeds.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/components/ui/signature-editor.tsx clients/dashboard/package.json clients/dashboard/package-lock.json
git commit -m "feat(dashboard): reusable signature editor (upload-crop + draw)"
```

---

### Task 9: Wire `SignatureEditor` into the provider edit dialog

**Files:**
- Modify: `clients/dashboard/src/pages/administration/providers.tsx` (the `ProviderEditorDialog`)

**Interfaces:**
- Consumes: `SignatureEditor` (Task 8), `setProviderSignature`/`clearProviderSignature` + `ProviderDto.signatureImageUrl` (Task 7).

- [ ] **Step 1: Add imports** at the top of `providers.tsx`:

```tsx
import { SignatureEditor } from "@/components/ui/signature-editor";
```
and extend the existing `@/api/administration` import to include `setProviderSignature` and `clearProviderSignature`.

- [ ] **Step 2: Add signature state + mutations inside `ProviderEditorDialog`** — after the `updateMutation` declaration:

```tsx
  const [signatureOpen, setSignatureOpen] = useState(false);

  const setSignatureMutation = useMutation({
    mutationFn: (arg: { providerId: string; imageBase64: string }) =>
      setProviderSignature(arg.providerId, arg.imageBase64),
    onSuccess: () => {
      toast.success("Signature updated");
      invalidate();
    },
    onError: (err) => toast.error("Signature update failed", { description: describe(err) }),
  });

  const clearSignatureMutation = useMutation({
    mutationFn: (providerId: string) => clearProviderSignature(providerId),
    onSuccess: () => {
      toast.success("Signature removed");
      invalidate();
    },
    onError: (err) => toast.error("Remove failed", { description: describe(err) }),
  });
```

- [ ] **Step 3: Add the Signature field** — inside `<DialogBody>`, render it only in edit mode (a saved provider id is required), just before the `Active` block (`{provider && ( ... Active ... )}`):

```tsx
            {provider && (
              <div className="rounded-lg border border-[var(--color-border)] px-3 py-2.5">
                <p className="mb-2 text-[13px] font-medium text-[var(--color-foreground)]">Signature</p>
                <div className="flex items-center gap-3">
                  {provider.signatureImageUrl ? (
                    <img
                      src={provider.signatureImageUrl}
                      alt="Provider signature"
                      className="h-[30px] w-[180px] border border-[var(--color-border)] object-contain"
                    />
                  ) : (
                    <span className="text-[12px] text-[var(--color-muted-foreground)]">No signature on file.</span>
                  )}
                  <Button type="button" variant="outline" size="sm" onClick={() => setSignatureOpen(true)}>
                    {provider.signatureImageUrl ? "Change signature image" : "Add signature image"}
                  </Button>
                  {provider.signatureImageUrl && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => clearSignatureMutation.mutate(provider.id)}
                      disabled={clearSignatureMutation.isPending}
                    >
                      Remove
                    </Button>
                  )}
                </div>
              </div>
            )}
```

- [ ] **Step 4: Render the editor** — just before the closing `</Dialog>` of `ProviderEditorDialog` (after `</DialogContent>` form), add (only meaningful in edit mode):

```tsx
        {provider && (
          <SignatureEditor
            open={signatureOpen}
            onOpenChange={setSignatureOpen}
            value={provider.signatureImageUrl}
            onSave={(pngBase64) => setSignatureMutation.mutate({ providerId: provider.id, imageBase64: pngBase64 })}
          />
        )}
```

> **Implementer note:** Place the `<SignatureEditor>` as a sibling of the `<form>` inside `<DialogContent>` (or move it outside `<DialogContent>` as a sibling `<Dialog>` if nesting a dialog inside dialog content misbehaves — Radix supports nested dialogs, but verify focus handling in Step 5). Drop `size="sm"` if `Button` has no `size` prop (same note as Task 8).

- [ ] **Step 5: Type-check + build + manual check**

Run: `cd clients/dashboard && npx tsc --noEmit && npm run build`
Expected: 0 TS errors; build succeeds.
Manual (dev server, real API): Administration → Providers → edit a provider → "Add signature image" → upload+crop saves and the preview updates; reopen → "Change signature image" → Draw a signature, save, preview updates; "Remove" clears it.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/pages/administration/providers.tsx
git commit -m "feat(dashboard): provider signature field in provider editor"
```

---

### Task 10: Docs, changelog, and master-plan sync

**Files:**
- Create/modify: docs repo (`github.com/fullstackhero/docs`) — Administration providers page note + changelog entry under `src/content/docs/changelog/` (golden rule #10).
- Modify: `~/.claude/plans/jolly-bouncing-crescent.md` (master Patient Reports plan) — fold in the design revisions.

- [ ] **Step 1: Update the master Patient Reports plan** so the Reports sprint inherits the new design. In `jolly-bouncing-crescent.md`:
  - **R6 (Sign):** replace "`SignPatientReportCommand(ReportId, SignatureImageBase64?)` → … uploads the PNG …" with: "`SignPatientReportCommand(ReportId)` (no image param). Handler resolves the signing user from `ICurrentUser`, then — if `report.ProviderId` is set — sends `GetProviderByIdQuery(report.ProviderId)` (cross-module via a new `Modules.Patient` → `Modules.Administration.Contracts` reference) and snapshots `ProviderDto.SignatureImagePath` onto `PatientReport.SignatureImagePath`. Signature optional — signing succeeds with null when the provider has none."
  - **R8 (review-sign):** same pattern using `ReviewerProviderId` → `ReviewSignatureImagePath`.
  - **R11:** note that the provider-signature API (`setProviderSignature`/`clearProviderSignature`) + `ProviderDto` signature fields are delivered by this separate plan.
  - **R12:** replace the draw-only `SignaturePad` description with: "Delivered by the per-provider signature plan (`docs/superpowers/plans/2026-06-26-per-provider-signature.md`) as `components/ui/signature-editor.tsx` (upload-crop + draw), surfaced in the provider admin record. No per-report signature capture."
  - Add a one-line dependency note: "Reports R6/R8 depend on the Administration provider-signature backend (this plan, Tasks 1–6) being merged first."

- [ ] **Step 2: Add the changelog entry** in the docs repo under `src/content/docs/changelog/` (follow the existing entries' frontmatter/format): summarize "Providers now have a signature image, set via a reusable upload-crop-or-draw editor in Administration → Providers."

- [ ] **Step 3: Update the Administration docs page** describing the Providers admin to mention the Signature field.

- [ ] **Step 4: Commit** (plan change in this repo; docs changes in the docs repo per its workflow):

```bash
git add docs/superpowers/plans/2026-06-26-per-provider-signature.md
git commit -m "docs(patient): sync Patient Reports plan to per-provider signature design"
```

---

## Verification (whole feature)

1. **Backend build:** `dotnet build src/FSH.Starter.slnx` — 0 warnings/errors.
2. **Unit tests:** `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj` and `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj` — PASS (validators present, boundaries respected).
3. **Migration applied:** `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply` — `Providers.SignatureImagePath` exists in acme/globex.
4. **API smoke (Scalar, admin token):** PUT signature → 200 URL; GET provider → path+url populated; DELETE → 204, GET → both null.
5. **Frontend:** `cd clients/dashboard && npx tsc --noEmit && npm run build` — 0 errors.
6. **Browser walkthrough:** Administration → Providers → edit → Add signature (upload+crop), reopen → Change (draw), Remove — preview reflects each state.

## Self-review notes (addressed)

- **Spec coverage:** A1→Tasks 1–3; A2→Tasks 4–6; R12→Task 8; R11 admin wiring→Tasks 7+9; R6/R8 revisions→recorded in the master plan (Task 10) since they depend on the not-yet-built `PatientReport`. Optional/`ProviderId=null`/missing-provider sign behavior is captured in the master-plan wording, not codeable here.
- **Type consistency:** `SetProviderSignatureCommand(ProviderId, ImageBase64)→string`, `ClearProviderSignatureCommand(ProviderId)→Unit`, `ProviderDto.SignatureImagePath`/`SignatureImageUrl`, `setProviderSignature(providerId, imageBase64)`, `clearProviderSignature(providerId)`, `SignatureEditor.onSave(pngBase64)` — names match across backend, API, and component tasks.
- **Storage-key / exception-type / Button-size / cropper-type** uncertainties are flagged inline as implementer notes to confirm against the repo at build time (each within a task that builds and would surface the mismatch).
```
