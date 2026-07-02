using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;

public static class DeletePatientNoteEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientNoteEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/notes/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientNoteCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatientNote")
            .WithSummary("Soft-delete a patient chart note")
            .RequirePermission(PatientPermissions.Notes.Delete);
    }
}
