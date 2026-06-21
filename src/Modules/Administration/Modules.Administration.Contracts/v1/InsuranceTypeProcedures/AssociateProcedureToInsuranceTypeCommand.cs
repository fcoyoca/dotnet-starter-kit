using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

public sealed record AssociateProcedureToInsuranceTypeCommand(
    Guid InsuranceTypeId,
    Guid ProcedureCodeId,
    decimal Price = 0m) : ICommand<Guid>;
