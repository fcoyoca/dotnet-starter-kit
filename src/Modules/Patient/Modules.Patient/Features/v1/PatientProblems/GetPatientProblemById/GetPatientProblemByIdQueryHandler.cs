using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.GetPatientProblemById;

public sealed class GetPatientProblemByIdQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetPatientProblemByIdQuery, PatientProblemDto>
{
    public async ValueTask<PatientProblemDto> Handle(GetPatientProblemByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        PatientProblemDto? dto = await dbContext.PatientProblems
            .AsNoTracking()
            .Where(x => x.Id == query.ProblemId && !x.IsDeleted)
            .Select(x => new PatientProblemDto(
                x.Id, x.PatientId, x.IncidentId, x.DiagnosticId, x.DiagnosticCode, x.DiagnosticDescription,
                x.DiagnosisDate, x.Status, x.Notes, x.IsMedicalAlert,
                x.CreatedByName, x.CreatedAtUtc, x.UpdatedByName, x.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Problem {query.ProblemId} not found.");

        return dto;
    }
}
