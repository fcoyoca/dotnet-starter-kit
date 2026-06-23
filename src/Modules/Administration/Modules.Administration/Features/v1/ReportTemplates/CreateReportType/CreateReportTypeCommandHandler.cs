using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportType;

public sealed class CreateReportTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateReportTypeCommand, int>
{
    public async ValueTask<int> Handle(CreateReportTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ReportType entity = ReportType.Create(command.Name, command.DisplayOrder);
        dbContext.ReportTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
