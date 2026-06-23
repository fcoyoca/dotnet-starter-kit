using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportField;

public sealed class UpdateReportFieldCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateReportFieldCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateReportFieldCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.ReportField entity = await dbContext.ReportFields
            .FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report field {command.Id} not found.");

        entity.Update(command.Name, command.Category, command.DisplayOrder, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
