using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientIncidentDiagnosticConfiguration : IEntityTypeConfiguration<PatientIncidentDiagnostic>
{
    public void Configure(EntityTypeBuilder<PatientIncidentDiagnostic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientIncidentDiagnostics");
        builder.HasKey(x => new { x.IncidentId, x.DiagnosticId });

        builder.Property(x => x.IncidentId).IsRequired();
        builder.Property(x => x.DiagnosticId).IsRequired();
    }
}
