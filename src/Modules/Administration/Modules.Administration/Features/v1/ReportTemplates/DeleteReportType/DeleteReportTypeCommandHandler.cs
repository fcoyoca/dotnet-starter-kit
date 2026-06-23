using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportType;

public sealed class DeleteReportTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteReportTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteReportTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.ReportType entity = await dbContext.ReportTypes
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report type {command.Id} not found.");

        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
