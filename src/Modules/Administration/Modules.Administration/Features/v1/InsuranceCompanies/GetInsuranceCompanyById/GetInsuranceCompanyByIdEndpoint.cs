using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.GetInsuranceCompanyById;

public static class GetInsuranceCompanyByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetInsuranceCompanyByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-companies/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetInsuranceCompanyByIdQuery(id), ct))
            .WithName("GetInsuranceCompanyById")
            .WithSummary("Get an insurance company by id")
            .RequirePermission(AdministrationPermissions.InsuranceCompanies.View);
    }
}
