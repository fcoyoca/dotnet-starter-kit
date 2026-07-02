using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;

public static class UpdatePatientNoteEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientNoteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/notes/{id:guid}",
                async (Guid id, UpdatePatientNoteCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientNoteCommand command = body with { NoteId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientNote")
            .WithSummary("Update a patient chart note")
            .RequirePermission(PatientPermissions.Notes.Update);
    }
}
