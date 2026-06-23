using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportType;

public sealed class UpdateReportTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateReportTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateReportTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.ReportType entity = await dbContext.ReportTypes
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report type {command.Id} not found.");

        entity.Update(command.Name, command.DisplayOrder, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
