using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.DeletePatientDocument;

public static class DeletePatientDocumentEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/documents/{documentId:guid}",
                async (Guid documentId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientDocumentCommand(documentId), ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithName("DeletePatientDocument")
            .WithSummary("Soft-delete a patient document")
            .RequirePermission(PatientPermissions.Documents.Delete);
    }
}
