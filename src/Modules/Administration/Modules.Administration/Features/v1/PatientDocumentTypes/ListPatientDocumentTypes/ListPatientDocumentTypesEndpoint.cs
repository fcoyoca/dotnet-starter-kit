using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PatientDocumentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PatientDocumentTypes.ListPatientDocumentTypes;

public static class ListPatientDocumentTypesEndpoint
{
    internal static RouteHandlerBuilder MapListPatientDocumentTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/patient-document-types",
                async (
                    string? search,
                    bool? isActive,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListPatientDocumentTypesQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListPatientDocumentTypes")
            .WithSummary("Search and list patient document types")
            .RequirePermission(AdministrationPermissions.PatientDocumentTypes.View);
    }
}
