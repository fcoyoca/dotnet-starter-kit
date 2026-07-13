using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Infrastructure;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.UpdatePatient;

public sealed class UpdatePatientCommandHandler(PatientDbContext dbContext, IPhiEncryptor phi)
    : ICommandHandler<UpdatePatientCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        bool codeTaken = await dbContext.Patients
            .AnyAsync(p => p.PatientCode == command.PatientCode && p.Id != command.PatientId, cancellationToken)
            .ConfigureAwait(false);
        if (codeTaken)
        {
            throw new CustomException(
                $"Another patient with code '{command.PatientCode}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        var demographics = PatientDemographics.Create(
            command.FirstName, command.LastName, command.MiddleInitial,
            command.DateOfBirth, command.Gender, command.MaritalStatus, command.IsMinor,
            command.RaceId, command.EthnicityId, command.LanguageId,
            command.SmokingStatusId, command.SmokingStartDate, command.SmokingEndDate,
            command.MedicalAlertNotes);

        var contact = PatientContact.Create(
            command.Address1, command.Address2, command.City, command.State, command.ZipCode,
            command.Phone, command.PhoneExtension, command.CellPhone, command.Email,
            command.PreferredContactMethodId);

        // The API never echoes plaintext SSN back to the client (PHI never leaves
        // encrypted-at-rest except masked), so the edit form's SSN fields are always
        // blank. Treat blank as "leave unchanged" instead of overwriting with null.
        string? ssnEncrypted = string.IsNullOrWhiteSpace(command.Ssn) ? patient.PHI.Ssn : phi.Encrypt(command.Ssn);
        string? ssnSearchHash = string.IsNullOrWhiteSpace(command.Ssn)
            ? patient.PHI.SsnSearchHash
            : phi.HashForSearch(command.Ssn);
        string? guardianSsnEncrypted = string.IsNullOrWhiteSpace(command.GuardianSsn)
            ? patient.PHI.GuardianSsn
            : phi.Encrypt(command.GuardianSsn);

        var phiValue = PatientPhi.Create(ssnEncrypted, ssnSearchHash, guardianSsnEncrypted);

        PatientEmployment? employment = HasEmployment(command)
            ? PatientEmployment.Create(
                command.Occupation, command.EmployerName,
                command.EmployerAddress1, command.EmployerAddress2,
                command.EmployerCity, command.EmployerState, command.EmployerZipCode,
                command.EmployerPhone, command.EmployerPhoneExtension)
            : null;

        PatientGuardian? guardian = command.IsMinor
            ? PatientGuardian.Create(
                command.GuardianFirstName, command.GuardianLastName, command.GuardianMiddleInitial,
                command.GuardianDateOfBirth, command.GuardianGender, command.GuardianMaritalStatus,
                command.GuardianAddress1, command.GuardianAddress2,
                command.GuardianCity, command.GuardianState, command.GuardianZipCode,
                command.GuardianPhone, command.GuardianCellPhone,
                command.GuardianEmployerName, command.GuardianEmployerAddress1, command.GuardianEmployerAddress2,
                command.GuardianEmployerCity, command.GuardianEmployerState, command.GuardianEmployerZipCode)
            : null;

        PatientNextOfKin? nextOfKin = HasNextOfKin(command)
            ? PatientNextOfKin.Create(
                command.NextOfKinFirstName, command.NextOfKinLastName, command.NextOfKinPhone,
                command.NextOfKinRelation, command.NextOfKinRelationRoleCode)
            : null;

        patient.Update(
            command.PatientCode, command.IsActive,
            demographics, contact, phiValue,
            employment, guardian, nextOfKin, command.ReferralTypeId,
            command.HasNoKnownProblems, command.HasNoKnownMedications, command.HasNoKnownAllergies,
            command.ReceivesEmailReminders, command.LastVisitDate, command.NextVisitDate);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return patient.Id;
    }

    private static bool HasEmployment(UpdatePatientCommand c) =>
        !string.IsNullOrWhiteSpace(c.EmployerName) || !string.IsNullOrWhiteSpace(c.Occupation);

    private static bool HasNextOfKin(UpdatePatientCommand c) =>
        !string.IsNullOrWhiteSpace(c.NextOfKinFirstName) || !string.IsNullOrWhiteSpace(c.NextOfKinLastName);
}
