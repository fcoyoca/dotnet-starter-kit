using FSH.Modules.Claims.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Claims.Data.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SuperBillId).IsRequired();
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.InsuranceTypeId);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.TotalCharge).HasPrecision(18, 4);
        builder.Property(x => x.ControlNumber).HasMaxLength(64);

        // One claim per super bill — idempotency key for the event handler upsert.
        builder.HasIndex(x => x.SuperBillId).IsUnique().HasDatabaseName("ux_claims_superbill");
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Claim.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
