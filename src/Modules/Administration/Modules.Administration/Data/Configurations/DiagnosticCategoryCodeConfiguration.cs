using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class DiagnosticCategoryCodeConfiguration : IEntityTypeConfiguration<DiagnosticCategoryCode>
{
    public void Configure(EntityTypeBuilder<DiagnosticCategoryCode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("DiagnosticCategoryCodes");
        builder.HasKey(x => new { x.DiagnosticCategoryId, x.DiagnosticId });

        builder.Property(x => x.DiagnosticCategoryId).IsRequired();
        builder.Property(x => x.DiagnosticId).IsRequired();

        builder.HasIndex(x => x.DiagnosticCategoryId);
    }
}
