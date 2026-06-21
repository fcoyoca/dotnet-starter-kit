using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

public sealed record RemoveProcedureFromInsuranceTypeCommand(
    Guid InsuranceTypeId,
    Guid ProcedureCodeId) : ICommand<Unit>;
