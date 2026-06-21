using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;

/// <summary>
/// Lists every procedure association for one insurance type. Non-paginated — the association set per
/// type is small and the editor renders them all against the full procedure-code list.
/// </summary>
public sealed record ListInsuranceTypeProceduresQuery(Guid InsuranceTypeId)
    : IQuery<IReadOnlyList<InsuranceTypeProcedureDto>>;
