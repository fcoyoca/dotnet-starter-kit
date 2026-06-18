using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Languages;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Languages.GetLanguageById;

public sealed class GetLanguageByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetLanguageByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetLanguageByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Language entity = await dbContext.Languages
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Language {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive);
    }
}
