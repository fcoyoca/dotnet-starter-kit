using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class InsuranceCompanyConfiguration : IEntityTypeConfiguration<InsuranceCompany>
{
    public void Configure(EntityTypeBuilder<InsuranceCompany> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("InsuranceCompanies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FormularyTiers).IsRequired();
        builder.Property(x => x.Address1).HasMaxLength(100);
        builder.Property(x => x.Address2).HasMaxLength(100);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.State).HasMaxLength(50);
        builder.Property(x => x.Zip).HasMaxLength(10);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        // Optional foreign key to an InsuranceType in the same module. Companies are soft-deleted so
        // this delete behaviour rarely fires; null-on-delete keeps the column consistent on a hard delete.
        builder.HasOne<InsuranceType>()
            .WithMany()
            .HasForeignKey(x => x.InsuranceTypeId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.InsuranceTypeId);

        builder.Ignore(x => x.DomainEvents);
    }
}
