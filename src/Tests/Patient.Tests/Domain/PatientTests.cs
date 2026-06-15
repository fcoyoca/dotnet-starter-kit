using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Domain.Events;

namespace Patient.Tests.Domain;

public sealed class PatientTests
{
    private static PatientDemographics AdultDemographics() =>
        PatientDemographics.Create("John", "Doe", null,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            "M", null, isMinor: false,
            null, null, null, null, null, null, null);

    private static PatientDemographics MinorDemographics() =>
        PatientDemographics.Create("Jane", "Doe", null,
            new DateTime(2015, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            "F", null, isMinor: true,
            null, null, null, null, null, null, null);

    private static PatientContact DefaultContact() =>
        PatientContact.Create(null, null, null, null, null,
            "555-1234", null, null, "john@example.com", null);

    private static PatientPhi DefaultPhi() =>
        PatientPhi.Create(null, null, null);

    private static PatientGuardian DefaultGuardian() =>
        PatientGuardian.Create("Mary", "Doe", null,
            new DateTime(1975, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            "F", null,
            null, null, null, null, null, null, null,
            null, null, null, null, null, null);

    #region Create — happy path

    [Fact]
    public void Create_Should_SetAllRequiredFields_When_Valid()
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-001", true,
            AdultDemographics(), DefaultContact(), DefaultPhi(),
            null, null, null, null,
            false, false, false, true, null, null);

        patient.PatientCode.ShouldBe("P-001");
        patient.IsActive.ShouldBeTrue();
        patient.IsDeleted.ShouldBeFalse();
        patient.Id.ShouldNotBe(Guid.Empty);
        patient.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimPatientCode()
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "  P-001  ", true,
            AdultDemographics(), DefaultContact(), DefaultPhi(),
            null, null, null, null,
            false, false, false, false, null, null);

        patient.PatientCode.ShouldBe("P-001");
    }

    [Fact]
    public void Create_Should_RaiseDomainEvent()
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-002", true,
            AdultDemographics(), DefaultContact(), DefaultPhi(),
            null, null, null, null,
            false, false, false, false, null, null);

        var events = ((IHasDomainEvents)patient).DomainEvents;
        events.ShouldNotBeEmpty();
        events.ShouldContain(e => e is PatientCreatedDomainEvent);

        var created = (PatientCreatedDomainEvent)events.First(e => e is PatientCreatedDomainEvent);
        created.PatientId.ShouldBe(patient.Id);
        created.PatientCode.ShouldBe("P-002");
    }

    #endregion

    #region Create — invariants

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_PatientCodeIsBlank(string code)
    {
        Should.Throw<ArgumentException>(() =>
            FSH.Modules.Patient.Domain.Patient.Create(
                code, true,
                AdultDemographics(), DefaultContact(), DefaultPhi(),
                null, null, null, null,
                false, false, false, false, null, null));
    }

    [Fact]
    public void Create_Should_Throw_When_DemographicsIsNull()
    {
        Should.Throw<ArgumentNullException>(() =>
            FSH.Modules.Patient.Domain.Patient.Create(
                "P-003", true,
                null!, DefaultContact(), DefaultPhi(),
                null, null, null, null,
                false, false, false, false, null, null));
    }

    [Fact]
    public void Create_Should_Throw_When_MinorHasNoGuardian()
    {
        Should.Throw<ArgumentException>(() =>
            FSH.Modules.Patient.Domain.Patient.Create(
                "P-004", true,
                MinorDemographics(), DefaultContact(), DefaultPhi(),
                null, guardian: null, null, null,
                false, false, false, false, null, null));
    }

    [Fact]
    public void Create_Should_Succeed_When_MinorHasGuardian()
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-005", true,
            MinorDemographics(), DefaultContact(), DefaultPhi(),
            null, DefaultGuardian(), null, null,
            false, false, false, false, null, null);

        patient.Guardian.ShouldNotBeNull();
        patient.Demographics.IsMinor.ShouldBeTrue();
    }

    #endregion

    #region Update

    [Fact]
    public void Update_Should_RaiseDomainEvent_And_StampUpdatedAt()
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-006", true,
            AdultDemographics(), DefaultContact(), DefaultPhi(),
            null, null, null, null,
            false, false, false, false, null, null);

        ((IHasDomainEvents)patient).ClearDomainEvents();

        patient.Update(
            "P-006-UPDATED", false,
            AdultDemographics(), DefaultContact(), DefaultPhi(),
            null, null, null, null,
            true, false, false, false, null, null);

        patient.PatientCode.ShouldBe("P-006-UPDATED");
        patient.IsActive.ShouldBeFalse();
        patient.HasNoKnownProblems.ShouldBeTrue();
        patient.UpdatedAtUtc.ShouldNotBeNull();

        var events = ((IHasDomainEvents)patient).DomainEvents;
        events.ShouldContain(e => e is PatientUpdatedDomainEvent);
    }

    #endregion

    #region Restore

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-007", true,
            AdultDemographics(), DefaultContact(), DefaultPhi(),
            null, null, null, null,
            false, false, false, false, null, null);

        patient.Restore();

        patient.IsDeleted.ShouldBeFalse();
        patient.UpdatedAtUtc.ShouldBeNull();
    }

    #endregion
}
