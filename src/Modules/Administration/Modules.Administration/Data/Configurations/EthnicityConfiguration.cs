using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class EthnicityConfiguration : IEntityTypeConfiguration<Ethnicity>
{
    public void Configure(EntityTypeBuilder<Ethnicity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Ethnicities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Ignore(x => x.DomainEvents);
        builder.HasData(
            new { Id = 1, Name = "Hispanic or Latino", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 2, Name = "Not Hispanic or Latino", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 3, Name = "Declined to specify", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null });
    }
}
