using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.UpdatePatientDocumentType;

public static class UpdatePatientDocumentTypeEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientDocumentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/patient-document-types/{id:guid}",
                async (Guid id, UpdatePatientDocumentTypeCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientDocumentTypeCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientDocumentType")
            .WithSummary("Update a patient document type")
            .RequirePermission(AdministrationPermissions.PatientDocumentTypes.Update);
    }
}
