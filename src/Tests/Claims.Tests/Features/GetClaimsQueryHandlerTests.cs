using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Claims.Features.v1.Claims.GetClaims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;
using ContractsClaimStatus = FSH.Modules.Claims.Contracts.ClaimStatus;

namespace Claims.Tests.Features;

public sealed class GetClaimsQueryHandlerTests
{
    private static ClaimsDbContext Ctx(string name)
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>().UseInMemoryDatabase(name).Options;
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(
            new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        var settings = Options.Create(new DatabaseOptions
        { Provider = "postgresql", ConnectionString = string.Empty, MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL" });
        return new ClaimsDbContext(accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    private static Claim Draft(decimal charge) => Claim.CreateFromSuperBill(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, false,
        [ClaimLine.Create(Guid.Empty, Guid.NewGuid(), "99213", "Visit", charge, [])]);

    [Fact]
    public async Task Returns_Page_And_Summary_With_Outstanding()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var ready = Draft(100m); ready.MarkReady();
        var submitted = Draft(200m); submitted.MarkReady(); submitted.Submit("C");
        db.Claims.AddRange(Draft(50m), ready, submitted);
        await db.SaveChangesAsync();

        var sut = new GetClaimsQueryHandler(db);
        var result = await sut.Handle(new GetClaimsQuery(), CancellationToken.None);

        result.Page.TotalCount.ShouldBe(3);
        result.Summary.Draft.ShouldBe(1);
        result.Summary.Ready.ShouldBe(1);
        result.Summary.Submitted.ShouldBe(1);
        result.Summary.OutstandingCharge.ShouldBe(300m); // ready 100 + submitted 200
    }

    [Fact]
    public async Task Filters_By_Status()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var ready = Draft(100m); ready.MarkReady();
        db.Claims.AddRange(Draft(50m), ready);
        await db.SaveChangesAsync();

        var sut = new GetClaimsQueryHandler(db);
        var result = await sut.Handle(new GetClaimsQuery(Status: ContractsClaimStatus.Ready), CancellationToken.None);

        result.Page.Items.Count.ShouldBe(1);
        result.Page.Items.Single().Status.ShouldBe(ContractsClaimStatus.Ready);
    }
}
