using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class MedicationReconciledDateConfiguration : IEntityTypeConfiguration<MedicationReconciledDate>
{
    public void Configure(EntityTypeBuilder<MedicationReconciledDate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MedicationReconciledDates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.ReconciledOn).HasColumnType("date").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.HasIndex(x => x.PatientId);
        builder.Ignore(x => x.DomainEvents);
    }
}
