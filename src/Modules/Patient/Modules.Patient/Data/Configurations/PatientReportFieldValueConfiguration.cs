using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientReportFieldValueConfiguration : IEntityTypeConfiguration<PatientReportFieldValue>
{
    public void Configure(EntityTypeBuilder<PatientReportFieldValue> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientReportFieldValues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.ReportFieldId).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(16000).IsRequired();
        builder.HasIndex(x => new { x.ReportId, x.ReportFieldId }).IsUnique();
    }
}
