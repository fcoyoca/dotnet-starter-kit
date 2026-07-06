using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.ListCustomDiagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class ListCustomDiagnosticsIdsFilterTests
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
    public async Task List_Should_Filter_By_Ids_When_Provided()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var keep = CustomDiagnostic.Create("M99.01", "Segmental dysfunction, cervical", null, true);
        var skip = CustomDiagnostic.Create("M54.5", "Low back pain", null, true);
        db.CustomDiagnostics.AddRange(keep, skip);
        await db.SaveChangesAsync();
        var sut = new ListCustomDiagnosticsQueryHandler(db);

        var result = await sut.Handle(
            new ListCustomDiagnosticsQuery(Ids: [keep.Id]), CancellationToken.None);

        result.Items.Single().Id.ShouldBe(keep.Id);
    }
}
