using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.GetPatientProblemById;

public static class GetPatientProblemByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientProblemByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/problems/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientProblemByIdQuery(id), ct)))
            .WithName("GetPatientProblemById")
            .WithSummary("Get a patient problem by id")
            .RequirePermission(PatientPermissions.Problems.View);
    }
}
