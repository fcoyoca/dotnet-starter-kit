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
        await SeedDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

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

    /// <summary>
    /// Seeds the global ICD-10-CM diagnostics catalog from the embedded seed (migrated from legacy
    /// <c>ICD10CMCodes</c>). Diagnostics are <c>IGlobalEntity</c> (cross-tenant), so this is guarded on the
    /// shared table being empty — the first tenant initialized loads it, the rest skip. Batched insert.
    /// </summary>
    private async Task SeedDiagnosticsAsync(CancellationToken cancellationToken)
    {
        const int icd10CmCodeSourceId = 7; // see CodeSourceConfiguration seed.

        if (await dbContext.Diagnostics.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        int count = 0;
        var batch = new List<Diagnostic>(2000);
        foreach (Icd10SeedData.SeedRow row in Icd10SeedData.Read())
        {
            batch.Add(Diagnostic.Create(
                row.Code, row.ShortDescription, row.LongDescription,
                icd10CmCodeSourceId, isChiropractic: false, isBillable: row.IsBillable));

            if (batch.Count >= 2000)
            {
                dbContext.Diagnostics.AddRange(batch);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                dbContext.ChangeTracker.Clear();
                count += batch.Count;
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            dbContext.Diagnostics.AddRange(batch);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();
            count += batch.Count;
        }

        if (count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[Administration] seeded {Count} ICD-10 diagnostics", count);
        }
    }
}
