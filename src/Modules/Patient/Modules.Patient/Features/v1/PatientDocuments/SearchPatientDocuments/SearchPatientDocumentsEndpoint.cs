using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientDocuments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientDocuments.SearchPatientDocuments;

public static class SearchPatientDocumentsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientDocumentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/documents",
                async (
                    Guid patientId,
                    Guid? documentTypeId,
                    bool? includeDeleted,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientDocumentsQuery(
                            patientId,
                            documentTypeId,
                            includeDeleted ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientDocuments")
            .WithSummary("Search a patient's uploaded documents")
            .RequirePermission(PatientPermissions.Documents.View);
    }
}
