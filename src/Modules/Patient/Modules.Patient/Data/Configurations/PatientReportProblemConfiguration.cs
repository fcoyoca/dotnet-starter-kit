using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientReportProblemConfiguration : IEntityTypeConfiguration<PatientReportProblem>
{
    public void Configure(EntityTypeBuilder<PatientReportProblem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientReportProblems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.ProblemId).IsRequired();

        builder.HasIndex(x => new { x.ReportId, x.ProblemId }).IsUnique();
    }
}
