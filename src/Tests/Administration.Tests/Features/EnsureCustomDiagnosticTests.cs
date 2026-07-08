using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.EnsureCustomDiagnostic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class EnsureCustomDiagnosticTests
{
    private static AdministrationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AdministrationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));

        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });

        return new AdministrationDbContext(
            accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    [Fact]
    public async Task Ensure_Should_Create_When_Absent()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new EnsureCustomDiagnosticCommandHandler(db);

        var id = await sut.Handle(
            new EnsureCustomDiagnosticCommand("M54.5", "Low back pain", "Chronic low back pain", IsChiropractic: true),
            CancellationToken.None);

        var created = await db.CustomDiagnostics.SingleAsync(c => c.Id == id);
        created.Code.ShouldBe("M54.5");
        created.Description.ShouldBe("Low back pain");
        created.LongDescription.ShouldBe("Chronic low back pain");
        created.IsChiropractic.ShouldBeTrue();
        created.IsActive.ShouldBeTrue();
        created.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Ensure_Should_Return_Existing_Id_CaseInsensitively_Without_Duplicating()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var existing = CustomDiagnostic.Create("M54.5", "Low back pain", null, isChiropractic: true);
        db.CustomDiagnostics.Add(existing);
        await db.SaveChangesAsync();
        var sut = new EnsureCustomDiagnosticCommandHandler(db);

        var id = await sut.Handle(
            new EnsureCustomDiagnosticCommand("m54.5", "Different description", "Different long description"),
            CancellationToken.None);

        id.ShouldBe(existing.Id);
        var all = await db.CustomDiagnostics.ToListAsync();
        all.Count.ShouldBe(1);
        var stored = all.Single();
        // Ensure only finds/reactivates — it does not overwrite fields on an existing active match.
        stored.Description.ShouldBe("Low back pain");
    }

    [Fact]
    public async Task Ensure_Should_Reactivate_SoftDeleted_Match()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var existing = CustomDiagnostic.Create("M54.5", "Low back pain", null, isChiropractic: true);
        existing.Delete("admin@tenant");
        db.CustomDiagnostics.Add(existing);
        await db.SaveChangesAsync();
        var sut = new EnsureCustomDiagnosticCommandHandler(db);

        var id = await sut.Handle(
            new EnsureCustomDiagnosticCommand("M54.5"),
            CancellationToken.None);

        id.ShouldBe(existing.Id);
        var all = await db.CustomDiagnostics.IgnoreQueryFilters().ToListAsync();
        all.Count.ShouldBe(1);
        var reactivated = all.Single();
        reactivated.IsDeleted.ShouldBeFalse();
        reactivated.DeletedOnUtc.ShouldBeNull();
        reactivated.DeletedBy.ShouldBeNull();
        reactivated.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Ensure_Should_Reactivate_Inactive_NonDeleted_Match()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var existing = CustomDiagnostic.Create("M54.5", "Low back pain", null, isChiropractic: true);
        existing.Update("M54.5", "Low back pain", null, isChiropractic: true, isActive: false);
        db.CustomDiagnostics.Add(existing);
        await db.SaveChangesAsync();
        var sut = new EnsureCustomDiagnosticCommandHandler(db);

        var id = await sut.Handle(new EnsureCustomDiagnosticCommand("M54.5"), CancellationToken.None);

        id.ShouldBe(existing.Id);
        var stored = await db.CustomDiagnostics.SingleAsync(c => c.Id == id);
        stored.IsActive.ShouldBeTrue();
    }
}
