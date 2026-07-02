using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.ListMedicationDoseUnits;

public static class ListMedicationDoseUnitsEndpoint
{
    internal static RouteHandlerBuilder MapListMedicationDoseUnitsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medication-dose-units",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListMedicationDoseUnitsQuery(isActive), ct))
            .WithName("ListMedicationDoseUnits")
            .WithSummary("List all medication dose units")
            .RequirePermission(AdministrationPermissions.MedicationDoseUnits.View);
    }
}
