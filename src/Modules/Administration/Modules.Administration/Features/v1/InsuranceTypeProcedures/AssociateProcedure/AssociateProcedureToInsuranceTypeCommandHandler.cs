using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.AssociateProcedure;

public sealed class AssociateProcedureToInsuranceTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<AssociateProcedureToInsuranceTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(AssociateProcedureToInsuranceTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool typeExists = await dbContext.InsuranceTypes
            .AnyAsync(t => t.Id == command.InsuranceTypeId, cancellationToken)
            .ConfigureAwait(false);
        if (!typeExists)
        {
            throw new NotFoundException($"Insurance type {command.InsuranceTypeId} not found.");
        }

        bool codeExists = await dbContext.ProcedureCodes
            .AnyAsync(c => c.Id == command.ProcedureCodeId, cancellationToken)
            .ConfigureAwait(false);
        if (!codeExists)
        {
            throw new NotFoundException($"Procedure code {command.ProcedureCodeId} not found.");
        }

        bool alreadyAssociated = await dbContext.InsuranceTypeProcedures
            .AnyAsync(x => x.InsuranceTypeId == command.InsuranceTypeId && x.ProcedureCodeId == command.ProcedureCodeId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyAssociated)
        {
            throw new CustomException(
                "That procedure code is already associated with this insurance type.",
                Array.Empty<string>(),
                HttpStatusCode.Conflict);
        }

        InsuranceTypeProcedure entity = InsuranceTypeProcedure.Create(
            command.InsuranceTypeId,
            command.ProcedureCodeId,
            command.Price);
        dbContext.InsuranceTypeProcedures.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
