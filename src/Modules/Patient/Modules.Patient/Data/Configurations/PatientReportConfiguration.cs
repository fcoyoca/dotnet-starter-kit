using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientReportConfiguration : IEntityTypeConfiguration<PatientReport>
{
    public void Configure(EntityTypeBuilder<PatientReport> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientReports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IncidentId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.ReportTypeId).IsRequired();
        builder.Property(x => x.ReportDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.WorkflowStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SignedByUserId).HasMaxLength(256);
        builder.Property(x => x.SignedByName).HasMaxLength(256);
        builder.Property(x => x.SignatureImagePath).HasMaxLength(512);
        builder.Property(x => x.ReviewRequestedByUserId).HasMaxLength(256);
        builder.Property(x => x.ReviewSignedByUserId).HasMaxLength(256);
        builder.Property(x => x.ReviewSignedByName).HasMaxLength(256);
        builder.Property(x => x.ReviewSignatureImagePath).HasMaxLength(512);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.OwnsOne(x => x.Vitals, v =>
        {
            v.Property(p => p.HeightInches).HasColumnName("VitalsHeightInches").HasColumnType("numeric(6,2)");
            v.Property(p => p.WeightLbs).HasColumnName("VitalsWeightLbs").HasColumnType("numeric(6,2)");
            v.Property(p => p.Bmi).HasColumnName("VitalsBmi").HasColumnType("numeric(5,2)");
            v.Property(p => p.Systolic).HasColumnName("VitalsSystolic");
            v.Property(p => p.Diastolic).HasColumnName("VitalsDiastolic");
            v.Property(p => p.Pulse).HasColumnName("VitalsPulse");
            v.Property(p => p.TemperatureF).HasColumnName("VitalsTemperatureF").HasColumnType("numeric(5,2)");
        });
        builder.Navigation(x => x.Vitals).IsRequired();

        builder.HasIndex(x => x.IncidentId);
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.AppointmentId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.IncidentId, x.IsDeleted });

        builder.HasMany(x => x.FieldValues)
            .WithOne()
            .HasForeignKey(f => f.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Addendums)
            .WithOne()
            .HasForeignKey(a => a.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AssociatedProblems)
            .WithOne()
            .HasForeignKey(p => p.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
