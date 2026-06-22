using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class MacroConfiguration : IEntityTypeConfiguration<Macro>
{
    public void Configure(EntityTypeBuilder<Macro> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Macros");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Text).HasMaxLength(8000);
        builder.Property(x => x.ReportFieldId);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        // Macros are scoped to a report field (null = "All (General)"); the field catalog is seeded global
        // reference data, so the FK never cascades.
        builder.HasOne<ReportField>()
            .WithMany()
            .HasForeignKey(x => x.ReportFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.ReportFieldId);
        // Macro names are unique within a field (legacy allows the same name under different fields).
        builder.HasIndex(x => new { x.ReportFieldId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Ignore(x => x.DomainEvents);
    }
}
