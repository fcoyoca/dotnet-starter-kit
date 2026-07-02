using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.CreateMedicationDoseUnit;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.DeleteMedicationDoseUnit;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.ListMedicationDoseUnits;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.UpdateMedicationDoseUnit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class MedicationDoseUnitHandlerTests
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
        var create = new CreateMedicationDoseUnitCommandHandler(db);
        var list = new ListMedicationDoseUnitsQueryHandler(db);
        var update = new UpdateMedicationDoseUnitCommandHandler(db);
        var delete = new DeleteMedicationDoseUnitCommandHandler(db);

        int id = await create.Handle(new CreateMedicationDoseUnitCommand("mg"), CancellationToken.None);

        var items = await list.Handle(new ListMedicationDoseUnitsQuery(), CancellationToken.None);
        items.Count.ShouldBe(1);
        items.Single().Name.ShouldBe("mg");

        await update.Handle(new UpdateMedicationDoseUnitCommand(id, "mg", IsActive: false), CancellationToken.None);
        (await list.Handle(new ListMedicationDoseUnitsQuery(IsActive: false), CancellationToken.None)).Count.ShouldBe(1);
        (await list.Handle(new ListMedicationDoseUnitsQuery(IsActive: true), CancellationToken.None)).Count.ShouldBe(0);

        await delete.Handle(new DeleteMedicationDoseUnitCommand(id), CancellationToken.None);
        (await list.Handle(new ListMedicationDoseUnitsQuery(), CancellationToken.None)).Count.ShouldBe(0);
    }
}
