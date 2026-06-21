using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class CustomDiagnosticConfiguration : IEntityTypeConfiguration<CustomDiagnostic>
{
    public void Configure(EntityTypeBuilder<CustomDiagnostic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CustomDiagnostics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.LongDescription).HasMaxLength(2000);
        builder.Property(x => x.IsChiropractic).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Ignore(x => x.DomainEvents);
    }
}
