using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ScheduleConfigConfiguration : IEntityTypeConfiguration<ScheduleConfig>
{
    public void Configure(EntityTypeBuilder<ScheduleConfig> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ScheduleConfig");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.IntervalMinutes).IsRequired();
        builder.Property(x => x.LegacyId);

        // Exactly one schedule-units row per clinic.
        builder.HasIndex(x => x.ClinicId).IsUnique();
        builder.HasIndex(x => x.LegacyId);

        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
