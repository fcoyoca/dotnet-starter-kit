using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.DeleteInsuranceCompany;

public static class DeleteInsuranceCompanyEndpoint
{
    internal static RouteHandlerBuilder MapDeleteInsuranceCompanyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/insurance-companies/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteInsuranceCompanyCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteInsuranceCompany")
            .WithSummary("Delete an insurance company")
            .RequirePermission(AdministrationPermissions.InsuranceCompanies.Delete);
    }
}
