using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ReportTypeConfiguration : IEntityTypeConfiguration<ReportType>
{
    public void Configure(EntityTypeBuilder<ReportType> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReportTypes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Ignore(x => x.DomainEvents);

        builder.HasData(
            new { Id = 1, Name = "Initial Evaluation" },
            new { Id = 2, Name = "Progress Report" },
            new { Id = 3, Name = "Discharge Report" },
            new { Id = 4, Name = "Daily Visit" },
            new { Id = 5, Name = "No Show" },
            new { Id = 6, Name = "NoFieldReport" });
    }
}
