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

        // Id is app-assigned (Guid.CreateVersion7) and join rows attach only via the PatientReport aggregate's
        // nav collection. Without ValueGeneratedNever, EF tracks the populated Guid as Modified → UPDATE-0-rows.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.ProblemId).IsRequired();

        builder.HasIndex(x => new { x.ReportId, x.ProblemId }).IsUnique();
    }
}
