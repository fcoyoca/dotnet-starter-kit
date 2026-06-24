using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Clinics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Address1).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Address2).HasMaxLength(100);
        builder.Property(x => x.City).IsRequired().HasMaxLength(100);
        builder.Property(x => x.State).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Zip).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        // Legacy linkage to the source BackChart/Bronston cID (null for native records).
        builder.Property(x => x.LegacyId);
        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        // Code is unique per tenant (tenant isolation already scopes the table) among live rows.
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Ignore(x => x.DomainEvents);
    }
}
