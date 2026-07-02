using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientAllergies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.DrugName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RxAui).HasMaxLength(12);
        builder.Property(x => x.Reaction).HasMaxLength(1000);
        builder.Property(x => x.Comments).HasMaxLength(4000);
        builder.Property(x => x.DateNoted).HasColumnType("date").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsActive });

        builder.Ignore(x => x.DomainEvents);
    }
}
