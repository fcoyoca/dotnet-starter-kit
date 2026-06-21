using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class InsuranceTypeConfiguration : IEntityTypeConfiguration<InsuranceType>
{
    public void Configure(EntityTypeBuilder<InsuranceType> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("InsuranceTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);
        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        // Optional default procedure category for this type (legacy lpcID). Types are soft-deleted, so this
        // delete behaviour rarely fires; null-on-delete keeps the column consistent on a hard delete.
        builder.HasOne<ProcedureCategory>()
            .WithMany()
            .HasForeignKey(x => x.ProcedureCategoryId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.ProcedureCategoryId);

        builder.Ignore(x => x.DomainEvents);
    }
}
