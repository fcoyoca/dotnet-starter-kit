using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.DeletePatientDocumentType;

public static class DeletePatientDocumentTypeEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientDocumentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/patient-document-types/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientDocumentTypeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatientDocumentType")
            .WithSummary("Delete a patient document type")
            .RequirePermission(AdministrationPermissions.PatientDocumentTypes.Delete);
    }
}
