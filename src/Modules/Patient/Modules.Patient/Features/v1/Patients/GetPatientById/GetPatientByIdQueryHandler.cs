using FSH.Framework.Core.Exceptions;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Infrastructure;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.GetPatientById;

public sealed class GetPatientByIdQueryHandler(
    PatientDbContext dbContext,
    IPhiEncryptor phi,
    IAuditPublisher auditPublisher,
    IMediator mediator)
    : IQueryHandler<GetPatientByIdQuery, PatientDetailDto>
{
    public async ValueTask<PatientDetailDto> Handle(GetPatientByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var patient = await dbContext.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {query.PatientId} not found.");

        // HIPAA audit: every PHI access is logged
        await auditPublisher.PublishAsync(new AuditEnvelope(
            id: Guid.NewGuid(),
            occurredAtUtc: DateTime.UtcNow,
            receivedAtUtc: DateTime.UtcNow,
            eventType: AuditEventType.Activity,
            severity: AuditSeverity.Information,
            tenantId: auditPublisher.CurrentScope.TenantId,
            userId: auditPublisher.CurrentScope.UserId,
            userName: auditPublisher.CurrentScope.UserName,
            traceId: auditPublisher.CurrentScope.TraceId,
            spanId: auditPublisher.CurrentScope.SpanId,
            correlationId: auditPublisher.CurrentScope.CorrelationId,
            requestId: auditPublisher.CurrentScope.RequestId,
            source: "Patient",
            tags: AuditTag.PiiMasked,
            payload: new ActivityEventPayload(
                Kind: ActivityKind.Query,
                Name: $"PHI_ACCESS:Patient:{query.PatientId}",
                StatusCode: 200,
                DurationMs: 0,
                Captured: BodyCapture.None,
                RequestSize: 0,
                ResponseSize: 0,
                RequestPreview: null,
                ResponsePreview: null)),
            cancellationToken)
            .ConfigureAwait(false);

        // Decrypt SSN for masking — never return plaintext in response
        string? decryptedSsn = phi.Decrypt(patient.PHI.Ssn);
        string? maskedSsn = MaskSsn(decryptedSsn);

        // Derive the chart's Last/Next visit from the Scheduling module's appointments
        // (cross-module read via Contracts). This is separate from the manually-entered
        // LastVisitDate/NextVisitDate columns, which the patient form still owns.
        var visits = await mediator
            .Send(new GetPatientVisitSummaryQuery(patient.Id), cancellationToken)
            .ConfigureAwait(false);

        return new PatientDetailDto(
            patient.Id,
            patient.PatientCode,
            patient.IsActive,
            patient.CreatedAtUtc,
            patient.UpdatedAtUtc,
            new PatientDemographicsDto(
                patient.Demographics.FirstName, patient.Demographics.LastName, patient.Demographics.MiddleInitial,
                patient.Demographics.DateOfBirth, patient.Demographics.Gender, patient.Demographics.MaritalStatus,
                patient.Demographics.IsMinor, patient.Demographics.RaceId, patient.Demographics.EthnicityId,
                patient.Demographics.LanguageId, patient.Demographics.SmokingStatusId,
                patient.Demographics.SmokingStartDate, patient.Demographics.SmokingEndDate,
                patient.Demographics.MedicalAlertNotes),
            new PatientContactDto(
                patient.Contact.Address1, patient.Contact.Address2,
                patient.Contact.City, patient.Contact.State, patient.Contact.ZipCode,
                patient.Contact.Phone, patient.Contact.PhoneExtension, patient.Contact.CellPhone,
                patient.Contact.Email, patient.Contact.PreferredContactMethodId),
            new PatientPhiDto(maskedSsn),
            patient.Employment is null ? null : new PatientEmploymentDto(
                patient.Employment.Occupation, patient.Employment.EmployerName,
                patient.Employment.EmployerAddress1, patient.Employment.EmployerAddress2,
                patient.Employment.EmployerCity, patient.Employment.EmployerState, patient.Employment.EmployerZipCode,
                patient.Employment.EmployerPhone, patient.Employment.EmployerPhoneExtension),
            patient.Guardian is null ? null : new PatientGuardianDto(
                patient.Guardian.FirstName, patient.Guardian.LastName, patient.Guardian.MiddleInitial,
                patient.Guardian.DateOfBirth, patient.Guardian.Gender, patient.Guardian.MaritalStatus,
                patient.Guardian.Address1, patient.Guardian.Address2,
                patient.Guardian.City, patient.Guardian.State, patient.Guardian.ZipCode,
                patient.Guardian.Phone, patient.Guardian.CellPhone,
                patient.Guardian.EmployerName, patient.Guardian.EmployerAddress1, patient.Guardian.EmployerAddress2,
                patient.Guardian.EmployerCity, patient.Guardian.EmployerState, patient.Guardian.EmployerZipCode),
            patient.NextOfKin is null ? null : new PatientNextOfKinDto(
                patient.NextOfKin.FirstName, patient.NextOfKin.LastName,
                patient.NextOfKin.Phone, patient.NextOfKin.Relation, patient.NextOfKin.RelationRoleCode),
            patient.ReferralTypeId,
            patient.HasNoKnownProblems,
            patient.HasNoKnownMedications,
            patient.HasNoKnownAllergies,
            patient.ReceivesEmailReminders,
            patient.LastVisitDate,
            patient.NextVisitDate,
            visits.LastVisit is null ? null : new PatientVisitRefDto(visits.LastVisit.AppointmentId, visits.LastVisit.ClinicId, visits.LastVisit.StartUtc),
            visits.NextVisit is null ? null : new PatientVisitRefDto(visits.NextVisit.AppointmentId, visits.NextVisit.ClinicId, visits.NextVisit.StartUtc));
    }

    private static string? MaskSsn(string? ssn)
    {
        if (string.IsNullOrEmpty(ssn) || ssn.Length < 4) return ssn;
        return $"***-**-{ssn[^4..]}";
    }
}
