using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Appointments;

/// <summary>
/// Validates an appointment's cross-module references exist. Clinic/Provider/AppointmentType live in the
/// Administration module and are checked via its Contracts queries (which throw <see cref="NotFoundException"/>
/// when absent). <c>PatientId</c> is intentionally NOT validated here: <c>GetPatientByIdQuery</c> emits a HIPAA
/// PHI-access audit and decrypts PHI, which is inappropriate for a booking existence check — the patient id is
/// selected from the patient search UI.
/// </summary>
internal static class AppointmentRefValidator
{
    public static async ValueTask EnsureRefsExistAsync(
        IMediator mediator, Guid clinicId, Guid providerId, Guid? appointmentTypeId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(mediator);

        _ = await mediator.Send(new GetClinicByIdQuery(clinicId), ct).ConfigureAwait(false);
        _ = await mediator.Send(new GetProviderByIdQuery(providerId), ct).ConfigureAwait(false);

        if (appointmentTypeId is { } typeId)
        {
            IReadOnlyList<AppointmentTypeDto> types =
                await mediator.Send(new ListAppointmentTypesQuery(), ct).ConfigureAwait(false);
            if (types.All(t => t.Id != typeId))
            {
                throw new NotFoundException($"Appointment type {typeId} not found.");
            }
        }
    }
}
