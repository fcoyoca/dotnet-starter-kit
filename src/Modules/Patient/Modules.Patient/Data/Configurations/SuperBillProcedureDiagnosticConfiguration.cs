using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class SuperBillProcedureDiagnosticConfiguration : IEntityTypeConfiguration<SuperBillProcedureDiagnostic>
{
    public void Configure(EntityTypeBuilder<SuperBillProcedureDiagnostic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SuperBillProcedureDiagnostics");
        builder.HasKey(x => new { x.SuperBillProcedureId, x.DiagnosticId });

        builder.Property(x => x.SuperBillProcedureId).IsRequired();
        builder.Property(x => x.DiagnosticId).IsRequired();
    }
}
