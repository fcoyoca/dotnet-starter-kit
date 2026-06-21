using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

public sealed record UpdateInsuranceTypeProcedurePriceCommand(
    Guid InsuranceTypeId,
    Guid ProcedureCodeId,
    decimal Price) : ICommand<Unit>;
