using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class RaceConfiguration : IEntityTypeConfiguration<Race>
{
    public void Configure(EntityTypeBuilder<Race> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Races");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Ignore(x => x.DomainEvents);
        builder.HasData(
            new { Id = 1, Name = "White", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 2, Name = "Black or African American", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 3, Name = "American Indian or Alaska Native", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 4, Name = "Asian", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 5, Name = "Native Hawaiian or Other Pacific Islander", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 6, Name = "Other", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 7, Name = "Declined to specify", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null });
    }
}
