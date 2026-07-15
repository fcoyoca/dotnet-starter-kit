using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Claims.Features.v1.Claims.MarkReady;
using FSH.Modules.Claims.Features.v1.Claims.Submit;
using FSH.Modules.Claims.Features.v1.Claims.MarkPaid;
using FSH.Modules.Claims.Features.v1.Claims.MarkDenied;
using FSH.Modules.Claims.Features.v1.Claims.Void;
using FSH.Modules.Claims.Submission;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Claims.Tests.Features;

public sealed class ClaimTransitionHandlerTests
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

    private static async Task<Claim> SeedDraft(ClaimsDbContext db)
    {
        var claim = Claim.CreateFromSuperBill(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, false,
            [ClaimLine.Create(Guid.Empty, Guid.NewGuid(), "99213", "Visit", 100m, [])]);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        return claim;
    }

    [Fact]
    public async Task MarkReady_Advances_To_Ready()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        var sut = new MarkClaimReadyCommandHandler(db);

        await sut.Handle(new MarkClaimReadyCommand(claim.Id), CancellationToken.None);

        (await db.Claims.FindAsync(claim.Id))!.Status.ShouldBe(ClaimStatus.Ready);
    }

    [Fact]
    public async Task MarkReady_Missing_Claim_Throws_NotFound()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var sut = new MarkClaimReadyCommandHandler(db);

        await Should.ThrowAsync<FSH.Framework.Core.Exceptions.NotFoundException>(
            () => sut.Handle(new MarkClaimReadyCommand(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Submit_Calls_Submitter_And_Sets_ControlNumber()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        await db.SaveChangesAsync();

        var submitter = Substitute.For<IClaimSubmitter>();
        // CA2012 fires on NSubstitute's arrange syntax: `submitter.SubmitAsync(...)` is a call into
        // the substitute to record the expectation, not a real ValueTask being consumed.
#pragma warning disable CA2012
        submitter.SubmitAsync(Arg.Any<FSH.Modules.Claims.Contracts.Dtos.ClaimDetailDto>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("CTRL-9"));
#pragma warning restore CA2012
        var sut = new SubmitClaimCommandHandler(db, submitter);

        await sut.Handle(new SubmitClaimCommand(claim.Id), CancellationToken.None);

        var saved = await db.Claims.FindAsync(claim.Id);
        saved!.Status.ShouldBe(ClaimStatus.Submitted);
        saved.ControlNumber.ShouldBe("CTRL-9");
    }

    [Fact]
    public async Task Submit_From_Draft_Throws_InvalidOperation()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        var submitter = Substitute.For<IClaimSubmitter>();
        // CA2012 fires on NSubstitute's arrange syntax; see the comment on the analogous setup above.
#pragma warning disable CA2012
        submitter.SubmitAsync(Arg.Any<FSH.Modules.Claims.Contracts.Dtos.ClaimDetailDto>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("CTRL-UNUSED"));
#pragma warning restore CA2012
        var sut = new SubmitClaimCommandHandler(db, submitter);

        // Claim is still Draft (never MarkReady'd) — Claim.Submit's status guard must fire.
        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new SubmitClaimCommand(claim.Id), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task MarkPaid_From_Ready_Throws_InvalidOperation()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        await db.SaveChangesAsync();
        var sut = new MarkClaimPaidCommandHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new MarkClaimPaidCommand(claim.Id), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task MarkPaid_From_Submitted_Advances_To_Paid()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        claim.Submit("CTRL-1");
        await db.SaveChangesAsync();
        var sut = new MarkClaimPaidCommandHandler(db);

        await sut.Handle(new MarkClaimPaidCommand(claim.Id), CancellationToken.None);

        (await db.Claims.FindAsync(claim.Id))!.Status.ShouldBe(ClaimStatus.Paid);
    }

    [Fact]
    public async Task MarkDenied_From_Submitted_Advances_To_Denied()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        claim.Submit("CTRL-1");
        await db.SaveChangesAsync();
        var sut = new MarkClaimDeniedCommandHandler(db);

        await sut.Handle(new MarkClaimDeniedCommand(claim.Id), CancellationToken.None);

        (await db.Claims.FindAsync(claim.Id))!.Status.ShouldBe(ClaimStatus.Denied);
    }

    [Fact]
    public async Task MarkDenied_Missing_Claim_Throws_NotFound()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var sut = new MarkClaimDeniedCommandHandler(db);

        await Should.ThrowAsync<FSH.Framework.Core.Exceptions.NotFoundException>(
            () => sut.Handle(new MarkClaimDeniedCommand(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Void_From_Ready_Advances_To_Voided()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        await db.SaveChangesAsync();
        var sut = new VoidClaimCommandHandler(db);

        await sut.Handle(new VoidClaimCommand(claim.Id, "no longer needed"), CancellationToken.None);

        (await db.Claims.FindAsync(claim.Id))!.Status.ShouldBe(ClaimStatus.Voided);
    }

    [Fact]
    public async Task Void_From_Paid_Throws_InvalidOperation()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        claim.Submit("CTRL-1");
        claim.MarkPaid();
        await db.SaveChangesAsync();
        var sut = new VoidClaimCommandHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new VoidClaimCommand(claim.Id, null), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Void_Missing_Claim_Throws_NotFound()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var sut = new VoidClaimCommandHandler(db);

        await Should.ThrowAsync<FSH.Framework.Core.Exceptions.NotFoundException>(
            () => sut.Handle(new VoidClaimCommand(Guid.NewGuid(), null), CancellationToken.None).AsTask());
    }
}
