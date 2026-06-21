using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.ListProcedureCodes;

public static class ListProcedureCodesEndpoint
{
    internal static RouteHandlerBuilder MapListProcedureCodesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/procedure-codes",
                async (
                    string? search,
                    bool? isActive,
                    Guid? procedureCategoryId,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListProcedureCodesQuery(search, isActive, procedureCategoryId, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListProcedureCodes")
            .WithSummary("Search and list procedure codes")
            .RequirePermission(AdministrationPermissions.ProcedureCodes.View);
    }
}
