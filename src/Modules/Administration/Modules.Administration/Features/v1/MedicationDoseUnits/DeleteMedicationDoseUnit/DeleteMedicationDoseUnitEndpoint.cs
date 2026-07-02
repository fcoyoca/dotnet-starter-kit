using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.DeleteMedicationDoseUnit;

public static class DeleteMedicationDoseUnitEndpoint
{
    internal static RouteHandlerBuilder MapDeleteMedicationDoseUnitEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/medication-dose-units/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteMedicationDoseUnitCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteMedicationDoseUnit")
            .WithSummary("Delete a medication dose unit")
            .RequirePermission(AdministrationPermissions.MedicationDoseUnits.Delete);
    }
}
