using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.UpdatePatientProblem;

public static class UpdatePatientProblemEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientProblemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/problems/{id:guid}",
                async (Guid id, UpdatePatientProblemCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientProblemCommand command = body with { ProblemId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientProblem")
            .WithSummary("Update a patient problem")
            .RequirePermission(PatientPermissions.Problems.Update);
    }
}
