using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.GetPatientDocumentTypeById;

public static class GetPatientDocumentTypeByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientDocumentTypeByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/patient-document-types/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetPatientDocumentTypeByIdQuery(id), ct))
            .WithName("GetPatientDocumentTypeById")
            .WithSummary("Get a patient document type by id")
            .RequirePermission(AdministrationPermissions.PatientDocumentTypes.View);
    }
}
