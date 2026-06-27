using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class DiagnosticConfiguration : IEntityTypeConfiguration<Diagnostic>
{
    public void Configure(EntityTypeBuilder<Diagnostic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Diagnostics");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.LongDescription).HasMaxLength(1024);
        builder.Property(x => x.CodeSourceId).IsRequired();
        builder.Property(x => x.IsChiropractic).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // Search by code (prefix) and filtering by source; trigram-ish ILIKE handled at query time.
        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.CodeSourceId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.CodeSourceId, x.Code }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Ignore(x => x.DomainEvents);
    }
}
