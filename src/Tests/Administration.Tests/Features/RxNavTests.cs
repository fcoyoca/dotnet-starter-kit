using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Features.v1.Drugs.ImportDrugs;
using FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class RxNavTests
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
    public void ParseDrugsResponse_Maps_ConceptProperties()
    {
        const string json = """
        {"drugGroup":{"name":"lipitor","conceptGroup":[
          {"tty":"SBD","conceptProperties":[
            {"rxcui":"617314","name":"atorvastatin 40 MG Oral Tablet [Lipitor]","synonym":"","tty":"SBD","language":"ENG","suppress":"N","umlscui":""}]},
          {"tty":"BPCK"}]}}
        """;

        IReadOnlyList<RxNavDrugDto> drugs = SearchRxNavQueryHandler.ParseDrugsResponse(json);

        drugs.Count.ShouldBe(1);
        drugs[0].RxCui.ShouldBe("617314");
        drugs[0].Name.ShouldBe("atorvastatin 40 MG Oral Tablet [Lipitor]");
        drugs[0].Tty.ShouldBe("SBD");
    }

    [Fact]
    public void ParseDrugsResponse_Empty_Group_Returns_Empty()
    {
        IReadOnlyList<RxNavDrugDto> drugs =
            SearchRxNavQueryHandler.ParseDrugsResponse("""{"drugGroup":{"name":"zzz"}}""");
        drugs.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportDrugs_Upserts_By_RxCui()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new ImportDrugsCommandHandler(db);
        var item = new RxNavDrugDto("314076", "Lisinopril 10 MG Oral Tablet", "SCD");

        int first = await sut.Handle(new ImportDrugsCommand([item]), CancellationToken.None);
        int second = await sut.Handle(
            new ImportDrugsCommand([item with { Name = "Lisinopril 10 MG Oral Tablet (updated)" }]),
            CancellationToken.None);

        first.ShouldBe(1);
        second.ShouldBe(1);
        (await db.Drugs.CountAsync(d => d.RxCui == "314076")).ShouldBe(1);
        (await db.Drugs.SingleAsync(d => d.RxCui == "314076")).Name.ShouldBe("Lisinopril 10 MG Oral Tablet (updated)");
    }
}
