using FSH.Modules.Claims.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Claims.Data.Configurations;

public sealed class ClaimLineConfiguration : IEntityTypeConfiguration<ClaimLine>
{
    public void Configure(EntityTypeBuilder<ClaimLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ClaimLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClaimId).IsRequired();
        builder.Property(x => x.ProcedureCodeId).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Charge).HasPrecision(18, 4);

        // DiagnosticIds is IReadOnlyList<Guid> with no public setter, backed by the private
        // `_diagnosticIds` field — point EF at the field explicitly so it can read/write the
        // primitive collection. EF Core 10 + Npgsql maps it to a `uuid[]` column.
        builder.PrimitiveCollection(x => x.DiagnosticIds)
            .HasField("_diagnosticIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.ClaimId);
    }
}
