using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.UpdatePatientDocument;

public static class UpdatePatientDocumentEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/documents/{documentId:guid}",
                async (Guid documentId, UpdatePatientDocumentCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (documentId != command.DocumentId)
                    {
                        throw new CustomException("Route id and body id do not match.", (IEnumerable<string>?)null, HttpStatusCode.BadRequest);
                    }

                    await mediator.Send(command, ct).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientDocument")
            .WithSummary("Update a patient document's type and notes")
            .RequirePermission(PatientPermissions.Documents.Update);
    }
}
