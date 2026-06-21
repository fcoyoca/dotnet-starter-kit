using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ProcedureCodeConfiguration : IEntityTypeConfiguration<ProcedureCode>
{
    public void Configure(EntityTypeBuilder<ProcedureCode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ProcedureCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.CodeSource).HasMaxLength(50);
        builder.Property(x => x.MacroText).HasMaxLength(4000);
        builder.Property(x => x.ProcedureCategoryId).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        // Required foreign key to the owning ProcedureCategory in the same module. Restrict on delete so a
        // category that still has codes cannot be hard-deleted out from under them.
        builder.HasOne<ProcedureCategory>()
            .WithMany()
            .HasForeignKey(x => x.ProcedureCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ProcedureCategoryId);

        builder.Ignore(x => x.DomainEvents);
    }
}
