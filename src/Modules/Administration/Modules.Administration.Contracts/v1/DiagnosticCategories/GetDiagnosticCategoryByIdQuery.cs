using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

public sealed record GetDiagnosticCategoryByIdQuery(Guid Id) : IQuery<DiagnosticCategoryDto>;
