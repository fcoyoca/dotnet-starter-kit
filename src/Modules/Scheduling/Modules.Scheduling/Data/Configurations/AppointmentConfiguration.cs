using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Appointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.StartUtc).IsRequired();
        builder.Property(x => x.EndUtc).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsReservation).IsRequired();
        builder.Property(x => x.ReservationTitle).HasMaxLength(200);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => new { x.ClinicId, x.StartUtc });
        builder.HasIndex(x => new { x.ProviderId, x.StartUtc });
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.LegacyId);

        builder.Ignore(x => x.DomainEvents);
    }
}
