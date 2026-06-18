using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Languages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Ignore(x => x.DomainEvents);
        builder.HasData(
            new { Id = 1, Name = "English", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 2, Name = "Spanish", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 3, Name = "Mandarin", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 4, Name = "Cantonese", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 5, Name = "Vietnamese", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 6, Name = "Tagalog", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 7, Name = "Korean", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 8, Name = "Other", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null });
    }
}
