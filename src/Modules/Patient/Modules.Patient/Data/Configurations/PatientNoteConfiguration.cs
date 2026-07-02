using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientNoteConfiguration : IEntityTypeConfiguration<PatientNote>
{
    public void Configure(EntityTypeBuilder<PatientNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientNotes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(8000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsDeleted });

        builder.Ignore(x => x.DomainEvents);
    }
}
