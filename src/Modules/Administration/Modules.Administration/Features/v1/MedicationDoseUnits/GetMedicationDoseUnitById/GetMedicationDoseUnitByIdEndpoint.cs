using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.GetMedicationDoseUnitById;

public static class GetMedicationDoseUnitByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetMedicationDoseUnitByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medication-dose-units/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetMedicationDoseUnitByIdQuery(id), ct))
            .WithName("GetMedicationDoseUnitById")
            .WithSummary("Get a medication dose unit by id")
            .RequirePermission(AdministrationPermissions.MedicationDoseUnits.View);
    }
}
