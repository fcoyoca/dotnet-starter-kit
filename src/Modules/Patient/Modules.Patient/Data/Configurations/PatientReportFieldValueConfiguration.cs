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

        // Id is app-assigned (Guid.CreateVersion7) and values attach only via the PatientReport aggregate's
        // nav collection. Without ValueGeneratedNever, EF tracks the populated Guid as Modified → UPDATE-0-rows.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.ReportFieldId).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(16000).IsRequired();
        builder.HasIndex(x => new { x.ReportId, x.ReportFieldId }).IsUnique();
    }
}
