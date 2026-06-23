using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportField;

public sealed class DeleteReportFieldCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteReportFieldCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteReportFieldCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.ReportField entity = await dbContext.ReportFields
            .FirstOrDefaultAsync(f => f.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report field {command.Id} not found.");

        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
