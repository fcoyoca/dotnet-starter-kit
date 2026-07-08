using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.SetCodes;

public sealed class SetDiagnosticCategoryCodesCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<SetDiagnosticCategoryCodesCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetDiagnosticCategoryCodesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool categoryExists = await dbContext.DiagnosticCategories
            .AnyAsync(c => c.Id == command.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (!categoryExists)
        {
            throw new NotFoundException($"Diagnostic category {command.CategoryId} not found.");
        }

        List<int> desiredIds = [.. command.DiagnosticIds.Distinct()];

        if (desiredIds.Count > 0)
        {
            List<int> existingIds = await dbContext.Diagnostics
                .Where(d => desiredIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<int> missingIds = [.. desiredIds.Except(existingIds)];
            if (missingIds.Count > 0)
            {
                throw new CustomException(
                    "One or more diagnostic codes were not found.",
                    missingIds.Select(id => $"{id}"),
                    HttpStatusCode.BadRequest);
            }
        }

        // InMemory test provider does not support ExecuteDeleteAsync — track and range-remove instead.
        List<DiagnosticCategoryCode> existing = await dbContext.DiagnosticCategoryCodes
            .Where(x => x.DiagnosticCategoryId == command.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        dbContext.DiagnosticCategoryCodes.RemoveRange(existing);

        foreach (int diagnosticId in desiredIds)
        {
            dbContext.DiagnosticCategoryCodes.Add(
                DiagnosticCategoryCode.Create(command.CategoryId, diagnosticId));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
