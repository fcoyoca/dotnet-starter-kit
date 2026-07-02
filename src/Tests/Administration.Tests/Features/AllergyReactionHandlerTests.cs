using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Features.v1.AllergyReactions.CreateAllergyReaction;
using FSH.Modules.Administration.Features.v1.AllergyReactions.DeleteAllergyReaction;
using FSH.Modules.Administration.Features.v1.AllergyReactions.ListAllergyReactions;
using FSH.Modules.Administration.Features.v1.AllergyReactions.UpdateAllergyReaction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class AllergyReactionHandlerTests
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
        var create = new CreateAllergyReactionCommandHandler(db);
        var list = new ListAllergyReactionsQueryHandler(db);
        var update = new UpdateAllergyReactionCommandHandler(db);
        var delete = new DeleteAllergyReactionCommandHandler(db);

        int id = await create.Handle(new CreateAllergyReactionCommand("Rash", "271807003"), CancellationToken.None);

        var items = await list.Handle(new ListAllergyReactionsQuery(), CancellationToken.None);
        items.Count.ShouldBe(1);
        items.Single().Term.ShouldBe("Rash");
        items.Single().SnomedCode.ShouldBe("271807003");

        await update.Handle(new UpdateAllergyReactionCommand(id, "Rash", "271807003", IsActive: false), CancellationToken.None);
        (await list.Handle(new ListAllergyReactionsQuery(IsActive: false), CancellationToken.None)).Count.ShouldBe(1);
        (await list.Handle(new ListAllergyReactionsQuery(IsActive: true), CancellationToken.None)).Count.ShouldBe(0);

        await delete.Handle(new DeleteAllergyReactionCommand(id), CancellationToken.None);
        (await list.Handle(new ListAllergyReactionsQuery(), CancellationToken.None)).Count.ShouldBe(0);
    }
}
