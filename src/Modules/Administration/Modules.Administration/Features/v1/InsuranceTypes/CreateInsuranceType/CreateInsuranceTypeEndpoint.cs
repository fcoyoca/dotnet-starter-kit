using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.CreateInsuranceType;

public static class CreateInsuranceTypeEndpoint
{
    internal static RouteHandlerBuilder MapCreateInsuranceTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/insurance-types",
                async (CreateInsuranceTypeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateInsuranceType")
            .WithSummary("Create an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Create)
            .WithIdempotency();
    }
}
