using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.DeletePatientProblem;

public static class DeletePatientProblemEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientProblemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/problems/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientProblemCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatientProblem")
            .WithSummary("Soft-delete a patient problem")
            .RequirePermission(PatientPermissions.Problems.Delete);
    }
}
