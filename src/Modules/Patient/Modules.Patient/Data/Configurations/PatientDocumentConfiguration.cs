using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientDocuments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.StoredPath).HasMaxLength(512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(8000);
        builder.Property(x => x.UploadedByUserId).HasMaxLength(256);
        builder.Property(x => x.UploadedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsDeleted });

        builder.Ignore(x => x.DomainEvents);
    }
}
