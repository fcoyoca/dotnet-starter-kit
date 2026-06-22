using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.CreatePatientDocumentType;

public static class CreatePatientDocumentTypeEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientDocumentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/patient-document-types",
                async (CreatePatientDocumentTypeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientDocumentType")
            .WithSummary("Create a patient document type")
            .RequirePermission(AdministrationPermissions.PatientDocumentTypes.Create)
            .WithIdempotency();
    }
}
