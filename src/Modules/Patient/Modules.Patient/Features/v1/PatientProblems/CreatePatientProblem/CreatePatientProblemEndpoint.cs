using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.CreatePatientProblem;

public static class CreatePatientProblemEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientProblemEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/problems",
                async (CreatePatientProblemCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientProblem")
            .WithSummary("Add a problem to a patient's problem list")
            .RequirePermission(PatientPermissions.Problems.Create);
    }
}
