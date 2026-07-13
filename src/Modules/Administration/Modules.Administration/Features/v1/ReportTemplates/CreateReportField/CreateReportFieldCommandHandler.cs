using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportField;

public sealed class CreateReportFieldCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateReportFieldCommand, int>
{
    public async ValueTask<int> Handle(CreateReportFieldCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool typeExists = await dbContext.ReportTypes
            .AnyAsync(t => t.Id == command.ReportTypeId, cancellationToken)
            .ConfigureAwait(false);
        if (!typeExists)
        {
            throw new NotFoundException($"Report type {command.ReportTypeId} not found.");
        }

        ReportField entity = ReportField.Create(
            command.ReportTypeId, command.Name, command.Category, command.DisplayOrder,
            defaultText: command.DefaultText);
        dbContext.ReportFields.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
