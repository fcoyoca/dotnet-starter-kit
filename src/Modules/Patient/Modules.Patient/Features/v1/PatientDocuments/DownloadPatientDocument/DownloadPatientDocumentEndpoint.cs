using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.DownloadPatientDocument;

public static class DownloadPatientDocumentEndpoint
{
    internal static RouteHandlerBuilder MapDownloadPatientDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/documents/{documentId:guid}/download",
                async (Guid documentId, IMediator mediator, CancellationToken ct) =>
                {
                    var result = await mediator.Send(new DownloadPatientDocumentQuery(documentId), ct).ConfigureAwait(false);
                    return Results.File(result.Content, result.ContentType, result.FileName);
                })
            .WithName("DownloadPatientDocument")
            .WithSummary("Download a patient document's file")
            .RequirePermission(PatientPermissions.Documents.Download)
            .Produces(StatusCodes.Status200OK, contentType: "application/octet-stream")
            .Produces(StatusCodes.Status404NotFound);
    }
}
