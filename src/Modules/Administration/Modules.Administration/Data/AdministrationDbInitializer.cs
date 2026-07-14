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
            await RealignLegacyFieldOrderAsync(cancellationToken).ConfigureAwait(false);
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
    /// One-time fix-up for tenants seeded before <c>SeedField.Order</c> became a global per-type
    /// sequence: the original seed carried the legacy within-category order, whose duplicate values
    /// made section order fall back to the list query's alphabetical category sort instead of the
    /// legacy clinical sequence (Chief Complaint → Present Problem → … → Work Status). Duplicate
    /// DisplayOrders among a type's legacy-seeded fields are the fingerprint of that old numbering —
    /// a catalog an admin has since renumbered has no duplicates and is left alone, as are
    /// admin-created types and fields (no LegacyId).
    /// </summary>
    private async Task RealignLegacyFieldOrderAsync(CancellationToken cancellationToken)
    {
        foreach (ReportTemplateSeedData.SeedType seedType in ReportTemplateSeedData.Types)
        {
            ReportType? type = await dbContext.ReportTypes
                .FirstOrDefaultAsync(t => t.LegacyId == seedType.LegacyId, cancellationToken)
                .ConfigureAwait(false);
            if (type is null)
            {
                continue;
            }

            List<ReportField> fields = await dbContext.ReportFields
                .Where(f => f.ReportTypeId == type.Id && f.LegacyId != null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!fields.GroupBy(f => f.DisplayOrder).Any(g => g.Count() > 1))
            {
                continue;
            }

            Dictionary<int, ReportTemplateSeedData.SeedField> seedByLegacyId =
                seedType.Fields.ToDictionary(f => f.LegacyId);

            bool changed = false;
            foreach (ReportField field in fields)
            {
                if (seedByLegacyId.TryGetValue(field.LegacyId!.Value, out ReportTemplateSeedData.SeedField? seed)
                    && field.DisplayOrder != seed.Order)
                {
                    field.Update(field.Name, field.Category, seed.Order, field.IsActive, field.DefaultText);
                    changed = true;
                }
            }

            if (changed)
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "[Administration] realigned report-field display order for type '{Type}'", type.Name);
                }
            }
        }
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
