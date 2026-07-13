using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientInsurancePolicyConfiguration : IEntityTypeConfiguration<PatientInsurancePolicy>
{
    private readonly IPhiEncryptor _phi;

    public PatientInsurancePolicyConfiguration(IPhiEncryptor phi)
    {
        ArgumentNullException.ThrowIfNull(phi);
        _phi = phi;
    }

    public void Configure(EntityTypeBuilder<PatientInsurancePolicy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("PatientInsurancePolicies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        // Payer/plan references live in the Administration module — no cross-module FK constraint.
        builder.Property(x => x.InsuranceCompanyId).IsRequired();
        builder.Property(x => x.InsuranceTypeId);

        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(32).IsRequired();

        builder.Property(x => x.PolicyNumber).HasMaxLength(50);
        builder.Property(x => x.GroupNumber).HasMaxLength(50);
        builder.Property(x => x.MemberId).HasMaxLength(50);

        builder.Property(x => x.CoPay).HasPrecision(18, 2);
        builder.Property(x => x.Deductible).HasPrecision(18, 2);

        // Calendar dates — see PatientConfiguration for why these must not be timestamptz.
        builder.Property(x => x.EffectiveDate).HasColumnType("date");
        builder.Property(x => x.ExpirationDate).HasColumnType("date");
        builder.Property(x => x.SubscriberDateOfBirth).HasColumnType("date");

        builder.Property(x => x.SubscriberRelationship).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SubscriberFirstName).HasMaxLength(100);
        builder.Property(x => x.SubscriberLastName).HasMaxLength(100);
        builder.Property(x => x.SubscriberGender).HasMaxLength(10);
        builder.Property(x => x.SubscriberEmployerName).HasMaxLength(200);
        builder.Property(x => x.SubscriberAddress1).HasMaxLength(200);
        builder.Property(x => x.SubscriberAddress2).HasMaxLength(200);
        builder.Property(x => x.SubscriberCity).HasMaxLength(100);
        builder.Property(x => x.SubscriberState).HasMaxLength(2);
        builder.Property(x => x.SubscriberZipCode).HasMaxLength(10);

        // The subscriber's SSN is PHI — encrypted at rest, exactly as the patient's own SSN is.
        var encryptConverter = new ValueConverter<string?, string?>(
            v => _phi.Encrypt(v),
            v => _phi.Decrypt(v));

        builder.Property(x => x.SubscriberSsn)
            .HasMaxLength(512)
            .HasColumnName("SubscriberSsnEncrypted")
            .HasConversion(encryptConverter);

        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsActive });

        // Coordination of benefits: a patient may hold at most one *active* policy per priority.
        // The create/update handlers check this too — that's the enforcement the tests exercise
        // (EF InMemory ignores indexes); this partial index is the backstop against a race.
        builder.HasIndex(x => new { x.PatientId, x.Priority })
            .IsUnique()
            .HasFilter("\"IsActive\" = TRUE");

        builder.Ignore(x => x.DomainEvents);
    }
}
