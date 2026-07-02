using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.CreateMedicationDoseUnit;

public static class CreateMedicationDoseUnitEndpoint
{
    internal static RouteHandlerBuilder MapCreateMedicationDoseUnitEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/medication-dose-units",
                async (CreateMedicationDoseUnitCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateMedicationDoseUnit")
            .WithSummary("Create a medication dose unit")
            .RequirePermission(AdministrationPermissions.MedicationDoseUnits.Create)
            .WithIdempotency();
    }
}
