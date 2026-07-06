using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;

public static class GetReportProceduresEndpoint
{
    internal static RouteHandlerBuilder MapGetReportProceduresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/reports/{id:guid}/procedures",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetReportProceduresQuery(id), ct)))
            .WithName("GetReportProcedures")
            .WithSummary("Get the procedures performed (super bill) for a report")
            .RequirePermission(PatientPermissions.SuperBills.View)
            .Produces<SuperBillDto>();
    }
}
