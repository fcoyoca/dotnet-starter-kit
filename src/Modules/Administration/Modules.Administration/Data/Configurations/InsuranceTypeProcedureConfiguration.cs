using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class InsuranceTypeProcedureConfiguration : IEntityTypeConfiguration<InsuranceTypeProcedure>
{
    public void Configure(EntityTypeBuilder<InsuranceTypeProcedure> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("InsuranceTypeProcedures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InsuranceTypeId).IsRequired();
        builder.Property(x => x.ProcedureCodeId).IsRequired();
        builder.Property(x => x.Price).IsRequired().HasPrecision(18, 2);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.InsuranceTypeId);
        builder.HasIndex(x => x.ProcedureCodeId);
        // One association per (insurance type, procedure code) within a tenant.
        builder.HasIndex(x => new { x.InsuranceTypeId, x.ProcedureCodeId }).IsUnique();

        builder.HasOne<InsuranceType>()
            .WithMany()
            .HasForeignKey(x => x.InsuranceTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ProcedureCode>()
            .WithMany()
            .HasForeignKey(x => x.ProcedureCodeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
