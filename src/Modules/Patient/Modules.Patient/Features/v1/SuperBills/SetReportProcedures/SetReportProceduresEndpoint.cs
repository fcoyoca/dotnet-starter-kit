using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;

public static class SetReportProceduresEndpoint
{
    internal static RouteHandlerBuilder MapSetReportProceduresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/procedures",
                async (Guid id, SetReportProceduresCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetReportProceduresCommand command = body with { ReportId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetReportProcedures")
            .WithSummary("Replace the procedures performed (super bill) for a report")
            .RequirePermission(PatientPermissions.SuperBills.Manage);
    }
}
