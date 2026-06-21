using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.CreateInsuranceCompany;

public static class CreateInsuranceCompanyEndpoint
{
    internal static RouteHandlerBuilder MapCreateInsuranceCompanyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/insurance-companies",
                async (CreateInsuranceCompanyCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateInsuranceCompany")
            .WithSummary("Create an insurance company")
            .RequirePermission(AdministrationPermissions.InsuranceCompanies.Create)
            .WithIdempotency();
    }
}
