using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

public sealed record CreateInsuranceTypeCommand(
    string Name,
    Guid? ProcedureCategoryId = null) : ICommand<Guid>;
