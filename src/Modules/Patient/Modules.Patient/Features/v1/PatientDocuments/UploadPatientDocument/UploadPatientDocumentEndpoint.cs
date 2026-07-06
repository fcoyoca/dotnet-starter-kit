using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.UploadPatientDocument;

public static class UploadPatientDocumentEndpoint
{
    internal static RouteHandlerBuilder MapUploadPatientDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/documents",
                async (UploadPatientDocumentCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("UploadPatientDocument")
            .WithSummary("Upload a patient chart document")
            .RequirePermission(PatientPermissions.Documents.Create);
    }
}
