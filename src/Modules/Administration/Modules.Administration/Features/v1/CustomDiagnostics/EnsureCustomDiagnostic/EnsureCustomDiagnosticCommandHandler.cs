using FSH.Framework.Persistence;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.EnsureCustomDiagnostic;

public sealed class EnsureCustomDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<EnsureCustomDiagnosticCommand, Guid>
{
    public async ValueTask<Guid> Handle(EnsureCustomDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string code = command.Code.Trim();

#pragma warning disable CA1308 // canonicalizing for a case-insensitive lookup key, not security-sensitive
        string normalizedCode = code.ToLowerInvariant();
#pragma warning restore CA1308

        // Deviation from EF.Functions.ILike (used by this module's Search/List handlers): ILike translates on
        // Postgres but the InMemory provider used by Administration.Tests has no translator for it, so the
        // case-insensitive match uses ToLower() comparison instead — this translates on both providers.
        // IgnoreQueryFilters targets only the named SoftDelete filter (see QueryFilters.SoftDelete) so a
        // soft-deleted match is still found while the tenant filter stays in force.
#pragma warning disable CA1304, CA1311, CA1862 // ToLower() runs inside an EF LINQ expression translated to SQL;
        // the culture-aware/string.Equals overloads these rules suggest are not translatable by EF providers.
        CustomDiagnostic? existing = await dbContext.CustomDiagnostics
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(c => c.Code.ToLower() == normalizedCode, cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore CA1304, CA1311, CA1862

        if (existing is not null)
        {
            if (existing.IsDeleted || !existing.IsActive)
            {
                existing.Reactivate();
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return existing.Id;
        }

        CustomDiagnostic entity = CustomDiagnostic.Create(
            code,
            command.Description,
            command.LongDescription,
            command.IsChiropractic);
        dbContext.CustomDiagnostics.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
