using FSH.Framework.Persistence;
using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Administration.Data;

public sealed class AdministrationDbInitializer(
    AdministrationDbContext dbContext,
    ILogger<AdministrationDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Administration] applied migrations");
        }
    }

    /// <summary>
    /// Seeds the standard report-template catalog (types + fields) for the current tenant on first run.
    /// Idempotent — skips when the tenant already has any report type. Tenants manage their own catalog thereafter.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ReportTypes.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        foreach (ReportTemplateSeedData.SeedType seedType in ReportTemplateSeedData.Types)
        {
            ReportType type = ReportType.Create(seedType.Name, seedType.Order, seedType.LegacyId);
            dbContext.ReportTypes.Add(type);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            foreach (ReportTemplateSeedData.SeedField field in seedType.Fields)
            {
                dbContext.ReportFields.Add(ReportField.Create(
                    type.Id, field.Name, field.Category, field.Order, field.IsActive, field.LegacyId));
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("[Administration] seeded report-template catalog");
    }
}
