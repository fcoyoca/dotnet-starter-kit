using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;

public static class SearchPatientNotesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientNotesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/notes",
                async (
                    Guid patientId,
                    bool? medicalAlertsOnly,
                    bool? includeDeleted,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientNotesQuery(
                            patientId,
                            medicalAlertsOnly ?? false,
                            includeDeleted ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientNotes")
            .WithSummary("Search a patient's chart notes")
            .RequirePermission(PatientPermissions.Notes.View);
    }
}
