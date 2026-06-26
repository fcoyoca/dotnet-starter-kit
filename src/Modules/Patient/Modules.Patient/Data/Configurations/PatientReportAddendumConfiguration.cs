using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientReportAddendumConfiguration : IEntityTypeConfiguration<PatientReportAddendum>
{
    public void Configure(EntityTypeBuilder<PatientReportAddendum> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientReportAddendums");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(16000).IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.HasIndex(x => x.ReportId);
    }
}
