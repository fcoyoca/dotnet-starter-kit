using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;
using FSH.Modules.Administration.Features.v1.Drugs.DeleteDrug;
using FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;
using FSH.Modules.Administration.Features.v1.Drugs.UpdateDrug;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class DrugHandlerTests
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
    public async Task Create_List_Update_Delete_Roundtrip()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var create = new CreateDrugCommandHandler(db);
        var list = new ListDrugsQueryHandler(db);
        var update = new UpdateDrugCommandHandler(db);
        var delete = new DeleteDrugCommandHandler(db);

        int id = await create.Handle(
            new CreateDrugCommand("Lisinopril 10 MG Oral Tablet", "1998001", "314076", "SCD", "RXNORM", "314076"),
            CancellationToken.None);

        var page = await list.Handle(new ListDrugsQuery(), CancellationToken.None);
        page.Items.Count.ShouldBe(1);
        page.Items.Single().RxCui.ShouldBe("314076");

        await update.Handle(new UpdateDrugCommand(id, "Lisinopril 10 MG Oral Tablet", "1998001", "314076",
            "SCD", "RXNORM", "314076", IsActive: false), CancellationToken.None);
        (await list.Handle(new ListDrugsQuery(IsActive: false), CancellationToken.None)).Items.Count.ShouldBe(1);

        await delete.Handle(new DeleteDrugCommand(id), CancellationToken.None);
        (await list.Handle(new ListDrugsQuery(), CancellationToken.None)).Items.Count.ShouldBe(0);
    }
}
