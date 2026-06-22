using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.GetPatientDocumentTypeById;

public sealed class GetPatientDocumentTypeByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetPatientDocumentTypeByIdQuery, PatientDocumentTypeDto>
{
    public async ValueTask<PatientDocumentTypeDto> Handle(GetPatientDocumentTypeByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        PatientDocumentTypeDto? dto = await dbContext.PatientDocumentTypes
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new PatientDocumentTypeDto(c.Id, c.Name, c.IsActive, c.CreatedAtUtc, c.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return dto ?? throw new NotFoundException($"Patient document type {query.Id} not found.");
    }
}
