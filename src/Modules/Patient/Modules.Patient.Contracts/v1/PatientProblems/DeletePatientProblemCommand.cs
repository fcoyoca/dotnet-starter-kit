using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientProblems;

public sealed record DeletePatientProblemCommand(Guid ProblemId) : ICommand<Unit>;
