using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientIncidentConfiguration : IEntityTypeConfiguration<PatientIncident>
{
    public void Configure(EntityTypeBuilder<PatientIncident> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientIncidents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.DateOfLoss).HasColumnType("date").IsRequired();
        builder.Property(x => x.DateOfInitialVisit).HasColumnType("date");
        builder.Property(x => x.AccidentType).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.PatientStatus).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.AccidentState).HasMaxLength(2);
        builder.Property(x => x.Comments).HasMaxLength(8000);
        builder.Property(x => x.SummaryOfCare).HasMaxLength(8000);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.PatientId, x.IsClosed });
        builder.HasIndex(x => new { x.PatientId, x.IsDeleted });

        builder.HasMany(x => x.Diagnostics)
            .WithOne()
            .HasForeignKey(d => d.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
