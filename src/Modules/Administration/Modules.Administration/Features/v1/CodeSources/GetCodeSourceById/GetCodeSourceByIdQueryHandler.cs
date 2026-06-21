using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CodeSources.GetCodeSourceById;

public sealed class GetCodeSourceByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetCodeSourceByIdQuery, LookupItemDto>
{
    public async ValueTask<LookupItemDto> Handle(GetCodeSourceByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.CodeSource entity = await dbContext.CodeSources
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Code source {query.Id} not found.");
        return new LookupItemDto(entity.Id, entity.Name, entity.IsActive);
    }
}
