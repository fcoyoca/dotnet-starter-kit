using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.NextPatientCodePreview;

public static class GetNextPatientCodePreviewEndpoint
{
    internal static RouteHandlerBuilder MapGetNextPatientCodePreviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/patients/next-code-preview",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new NextPatientCodePreviewQuery(), ct)))
            .WithName("NextPatientCodePreview")
            .WithSummary("Peek the next auto-generated patient code (does not consume it)")
            .RequirePermission(PatientPermissions.Patients.Create);
    }
}
