using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using FSH.Modules.Administration.Features.v1.Diagnostics.ListDiagnostics;
using FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.ListCodes;
using FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.SetCodes;
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

    [Fact]
    public async Task ListCodes_Should_Return_Joined_Code_Details()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var dx = Diagnostic.Create("M54.5", "Low back pain", null, 7, isChiropractic: true);
        db.Diagnostics.Add(dx);
        var category = DiagnosticCategory.Create("Spine");
        db.DiagnosticCategories.Add(category);
        await db.SaveChangesAsync();
        db.DiagnosticCategoryCodes.Add(DiagnosticCategoryCode.Create(category.Id, dx.Id));
        await db.SaveChangesAsync();
        var sut = new ListDiagnosticCategoryCodesQueryHandler(db);

        var result = await sut.Handle(new ListDiagnosticCategoryCodesQuery(category.Id), CancellationToken.None);

        var item = result.Single();
        item.DiagnosticCategoryId.ShouldBe(category.Id);
        item.DiagnosticId.ShouldBe(dx.Id);
        item.Code.ShouldBe("M54.5");
        item.Description.ShouldBe("Low back pain");
    }

    [Fact]
    public async Task ListCodes_Should_Throw_NotFound_When_Category_Missing()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new ListDiagnosticCategoryCodesQueryHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            async () => await sut.Handle(new ListDiagnosticCategoryCodesQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task SetCodes_Should_Replace_Existing_Set()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var dx1 = Diagnostic.Create("M54.5", "Low back pain", null, 7);
        var dx2 = Diagnostic.Create("G43.1", "Migraine", null, 7);
        db.Diagnostics.AddRange(dx1, dx2);
        var category = DiagnosticCategory.Create("Spine");
        db.DiagnosticCategories.Add(category);
        await db.SaveChangesAsync();
        db.DiagnosticCategoryCodes.AddRange(
            DiagnosticCategoryCode.Create(category.Id, dx1.Id),
            DiagnosticCategoryCode.Create(category.Id, dx2.Id));
        await db.SaveChangesAsync();
        var sut = new SetDiagnosticCategoryCodesCommandHandler(db);

        await sut.Handle(new SetDiagnosticCategoryCodesCommand(category.Id, [dx1.Id]), CancellationToken.None);

        var remaining = await db.DiagnosticCategoryCodes
            .Where(x => x.DiagnosticCategoryId == category.Id)
            .ToListAsync();
        remaining.Single().DiagnosticId.ShouldBe(dx1.Id);
    }

    [Fact]
    public async Task SetCodes_Should_Clear_When_Empty_List()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var dx1 = Diagnostic.Create("M54.5", "Low back pain", null, 7);
        db.Diagnostics.Add(dx1);
        var category = DiagnosticCategory.Create("Spine");
        db.DiagnosticCategories.Add(category);
        await db.SaveChangesAsync();
        db.DiagnosticCategoryCodes.Add(DiagnosticCategoryCode.Create(category.Id, dx1.Id));
        await db.SaveChangesAsync();
        var sut = new SetDiagnosticCategoryCodesCommandHandler(db);

        await sut.Handle(new SetDiagnosticCategoryCodesCommand(category.Id, []), CancellationToken.None);

        var remaining = await db.DiagnosticCategoryCodes
            .Where(x => x.DiagnosticCategoryId == category.Id)
            .ToListAsync();
        remaining.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetCodes_Should_Throw_When_Unknown_Diagnostic_Id()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var category = DiagnosticCategory.Create("Spine");
        db.DiagnosticCategories.Add(category);
        await db.SaveChangesAsync();
        var sut = new SetDiagnosticCategoryCodesCommandHandler(db);

        await Should.ThrowAsync<CustomException>(
            async () => await sut.Handle(new SetDiagnosticCategoryCodesCommand(category.Id, [999]), CancellationToken.None));
    }

    [Fact]
    public async Task SetCodes_Should_Throw_NotFound_When_Category_Missing()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SetDiagnosticCategoryCodesCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            async () => await sut.Handle(new SetDiagnosticCategoryCodesCommand(Guid.NewGuid(), [1]), CancellationToken.None));
    }
}
