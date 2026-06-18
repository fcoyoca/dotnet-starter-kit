using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ReferralTypeConfiguration : IEntityTypeConfiguration<ReferralType>
{
    public void Configure(EntityTypeBuilder<ReferralType> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReferralTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Ignore(x => x.DomainEvents);
        builder.HasData(
            new { Id = 1, Name = "Physician referral", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 2, Name = "Self-referral", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 3, Name = "Insurance referral", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 4, Name = "Online/web", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 5, Name = "Word of mouth", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 6, Name = "Other", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null });
    }
}
