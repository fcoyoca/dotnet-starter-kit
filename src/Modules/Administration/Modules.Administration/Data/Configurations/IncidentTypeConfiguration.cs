using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class IncidentTypeConfiguration : IEntityTypeConfiguration<IncidentType>
{
    public void Configure(EntityTypeBuilder<IncidentType> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("IncidentTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Ignore(x => x.DomainEvents);
    }
}
