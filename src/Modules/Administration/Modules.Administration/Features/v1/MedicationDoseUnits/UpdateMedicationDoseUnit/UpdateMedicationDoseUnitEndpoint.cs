using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.UpdateMedicationDoseUnit;

public static class UpdateMedicationDoseUnitEndpoint
{
    internal static RouteHandlerBuilder MapUpdateMedicationDoseUnitEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/medication-dose-units/{id:int}",
                async (int id, UpdateMedicationDoseUnitCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateMedicationDoseUnitCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateMedicationDoseUnit")
            .WithSummary("Update a medication dose unit")
            .RequirePermission(AdministrationPermissions.MedicationDoseUnits.Update);
    }
}
