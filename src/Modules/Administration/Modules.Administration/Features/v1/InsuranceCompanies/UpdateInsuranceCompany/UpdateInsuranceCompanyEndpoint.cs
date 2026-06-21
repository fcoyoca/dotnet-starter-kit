using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.UpdateInsuranceCompany;

public static class UpdateInsuranceCompanyEndpoint
{
    internal static RouteHandlerBuilder MapUpdateInsuranceCompanyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/insurance-companies/{id:guid}",
                async (Guid id, UpdateInsuranceCompanyCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateInsuranceCompanyCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateInsuranceCompany")
            .WithSummary("Update an insurance company")
            .RequirePermission(AdministrationPermissions.InsuranceCompanies.Update);
    }
}
