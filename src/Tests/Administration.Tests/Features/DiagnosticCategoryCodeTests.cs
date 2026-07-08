using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using FSH.Modules.Administration.Features.v1.Diagnostics.ListDiagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class DiagnosticCategoryCodeTests
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
}
