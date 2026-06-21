using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

/// <summary>One selected procedure code and its negotiated price under an insurance type.</summary>
public sealed record InsuranceTypeProcedureItem(Guid ProcedureCodeId, decimal Price = 0m);

/// <summary>
/// Replaces the full set of procedure associations for one insurance type with <see cref="Items"/>
/// (legacy InsuranceTypes "insertString" save). Codes present become/stay associated at the given price;
/// codes absent are removed. Saved together with the insurance type.
/// </summary>
public sealed record SetInsuranceTypeProceduresCommand(
    Guid InsuranceTypeId,
    IReadOnlyList<InsuranceTypeProcedureItem> Items) : ICommand<Unit>;
