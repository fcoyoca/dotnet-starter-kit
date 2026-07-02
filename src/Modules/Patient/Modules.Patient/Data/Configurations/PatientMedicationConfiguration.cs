using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientMedicationConfiguration : IEntityTypeConfiguration<PatientMedication>
{
    public void Configure(EntityTypeBuilder<PatientMedication> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientMedications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.DrugName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RxAui).HasMaxLength(12);
        builder.Property(x => x.RxCode).HasMaxLength(12);
        builder.Property(x => x.Ndc).HasMaxLength(24);
        builder.Property(x => x.Prescriber).HasMaxLength(256);
        builder.Property(x => x.StartDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.DoseValue).HasPrecision(12, 3);
        builder.Property(x => x.DosePeriodValue).HasPrecision(12, 3);
        builder.Property(x => x.DosePeriodUnit).HasMaxLength(16);
        builder.Property(x => x.Instructions).HasMaxLength(4000);
        builder.Property(x => x.Indication).HasMaxLength(4000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsActive });

        builder.Ignore(x => x.DomainEvents);
    }
}
