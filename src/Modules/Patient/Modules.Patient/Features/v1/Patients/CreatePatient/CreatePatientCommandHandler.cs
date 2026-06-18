using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Infrastructure;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.CreatePatient;

public sealed class CreatePatientCommandHandler(PatientDbContext dbContext, IPhiEncryptor phi)
    : ICommandHandler<CreatePatientCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool codeTaken = await dbContext.Patients
            .AnyAsync(p => p.PatientCode == command.PatientCode, cancellationToken)
            .ConfigureAwait(false);
        if (codeTaken)
        {
            throw new CustomException(
                $"A patient with code '{command.PatientCode}' already exists.",
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

        var phiValue = PatientPhi.Create(
            phi.Encrypt(command.Ssn),
            phi.HashForSearch(command.Ssn),
            phi.Encrypt(command.GuardianSsn));

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

        PatientInsurance? insurance = HasInsurance(command)
            ? PatientInsurance.Create(
                command.InsuredFullName, command.InsuredDateOfBirth,
                command.InsuredEmployerName, command.ReferralTypeId)
            : null;

        var patient = Domain.Patient.Create(
            command.PatientCode, command.IsActive,
            demographics, contact, phiValue,
            employment, guardian, nextOfKin, insurance,
            command.HasNoKnownProblems, command.HasNoKnownMedications, command.HasNoKnownAllergies,
            command.ReceivesEmailReminders, command.LastVisitDate, command.NextVisitDate,
            command.LegacyUniqueId);

        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return patient.Id;
    }

    private static bool HasEmployment(CreatePatientCommand c) =>
        !string.IsNullOrWhiteSpace(c.EmployerName) || !string.IsNullOrWhiteSpace(c.Occupation);

    private static bool HasNextOfKin(CreatePatientCommand c) =>
        !string.IsNullOrWhiteSpace(c.NextOfKinFirstName) || !string.IsNullOrWhiteSpace(c.NextOfKinLastName);

    private static bool HasInsurance(CreatePatientCommand c) =>
        !string.IsNullOrWhiteSpace(c.InsuredFullName) || c.ReferralTypeId.HasValue;
}
