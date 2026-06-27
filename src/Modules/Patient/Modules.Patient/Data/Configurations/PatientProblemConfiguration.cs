using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientProblemConfiguration : IEntityTypeConfiguration<PatientProblem>
{
    public void Configure(EntityTypeBuilder<PatientProblem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientProblems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.DiagnosticId).IsRequired();
        builder.Property(x => x.DiagnosticCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DiagnosticDescription).HasMaxLength(1024);
        builder.Property(x => x.DiagnosisDate).HasColumnType("date");
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.PatientId, x.IsDeleted });

        builder.Ignore(x => x.DomainEvents);
    }
}
