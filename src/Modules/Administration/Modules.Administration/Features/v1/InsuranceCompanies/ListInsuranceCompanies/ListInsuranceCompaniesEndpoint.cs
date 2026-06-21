using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.ListInsuranceCompanies;

public static class ListInsuranceCompaniesEndpoint
{
    internal static RouteHandlerBuilder MapListInsuranceCompaniesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-companies",
                async (
                    string? search,
                    bool? isActive,
                    Guid? insuranceTypeId,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListInsuranceCompaniesQuery(search, isActive, insuranceTypeId, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListInsuranceCompanies")
            .WithSummary("Search and list insurance companies")
            .RequirePermission(AdministrationPermissions.InsuranceCompanies.View);
    }
}
