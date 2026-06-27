using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientProblems;

public sealed record GetPatientProblemByIdQuery(Guid ProblemId) : IQuery<PatientProblemDto>;
