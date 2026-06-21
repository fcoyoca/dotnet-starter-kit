using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

public sealed record GetProcedureCategoryByIdQuery(Guid Id) : IQuery<ProcedureCategoryDto>;
