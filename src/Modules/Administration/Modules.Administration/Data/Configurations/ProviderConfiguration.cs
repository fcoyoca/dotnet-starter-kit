using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Providers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Prefix).HasMaxLength(20);
        builder.Property(x => x.Suffix).HasMaxLength(20);
        builder.Property(x => x.Specialty).HasMaxLength(150);
        builder.Property(x => x.Npi).HasMaxLength(10);
        builder.Property(x => x.KareoExternalId).HasMaxLength(64);
        builder.Property(x => x.UserId).HasMaxLength(256);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyUserId);

        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.LegacyUserId);
        // NPI unique per tenant among live rows when present (partial index ignores NULLs + soft-deleted).
        builder.HasIndex(x => x.Npi).IsUnique().HasFilter("\"Npi\" IS NOT NULL AND \"IsDeleted\" = FALSE");
        // One Identity user maps to at most one live provider.
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL AND \"IsDeleted\" = FALSE");

        // Optional foreign key to a Clinic in the same module. Because clinics are soft-deleted this
        // delete behaviour rarely fires; null-on-delete keeps the column consistent on a hard delete.
        builder.HasOne<Clinic>()
            .WithMany()
            .HasForeignKey(x => x.PrimaryClinicId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasIndex(x => x.PrimaryClinicId);

        builder.Ignore(x => x.DomainEvents);
    }
}
