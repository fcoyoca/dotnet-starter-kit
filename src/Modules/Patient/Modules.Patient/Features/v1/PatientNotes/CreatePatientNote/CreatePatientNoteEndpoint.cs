using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;

public static class CreatePatientNoteEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientNoteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/notes",
                async (CreatePatientNoteCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientNote")
            .WithSummary("Add a note to a patient's chart")
            .RequirePermission(PatientPermissions.Notes.Create);
    }
}
