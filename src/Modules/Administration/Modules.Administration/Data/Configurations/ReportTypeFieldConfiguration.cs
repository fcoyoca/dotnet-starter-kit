using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ReportTypeFieldConfiguration : IEntityTypeConfiguration<ReportTypeField>
{
    public void Configure(EntityTypeBuilder<ReportTypeField> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReportTypeFields");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasIndex(x => new { x.ReportTypeId, x.ReportFieldId }).IsUnique();
        builder.HasIndex(x => x.ReportTypeId);
        builder.Ignore(x => x.DomainEvents);

        builder.HasData(
            // Initial Evaluation (1), Progress Report (2), Discharge Report (3) share the same 15 fields.
            new { Id = 1, ReportTypeId = 1, ReportFieldId = 1 },
            new { Id = 2, ReportTypeId = 1, ReportFieldId = 2 },
            new { Id = 3, ReportTypeId = 1, ReportFieldId = 3 },
            new { Id = 4, ReportTypeId = 1, ReportFieldId = 4 },
            new { Id = 5, ReportTypeId = 1, ReportFieldId = 5 },
            new { Id = 6, ReportTypeId = 1, ReportFieldId = 6 },
            new { Id = 7, ReportTypeId = 1, ReportFieldId = 7 },
            new { Id = 8, ReportTypeId = 1, ReportFieldId = 14 },
            new { Id = 9, ReportTypeId = 1, ReportFieldId = 15 },
            new { Id = 10, ReportTypeId = 1, ReportFieldId = 24 },
            new { Id = 11, ReportTypeId = 1, ReportFieldId = 27 },
            new { Id = 12, ReportTypeId = 1, ReportFieldId = 29 },
            new { Id = 13, ReportTypeId = 1, ReportFieldId = 30 },
            new { Id = 14, ReportTypeId = 1, ReportFieldId = 28 },
            new { Id = 15, ReportTypeId = 1, ReportFieldId = 13 },
            new { Id = 16, ReportTypeId = 2, ReportFieldId = 1 },
            new { Id = 17, ReportTypeId = 2, ReportFieldId = 2 },
            new { Id = 18, ReportTypeId = 2, ReportFieldId = 3 },
            new { Id = 19, ReportTypeId = 2, ReportFieldId = 4 },
            new { Id = 20, ReportTypeId = 2, ReportFieldId = 5 },
            new { Id = 21, ReportTypeId = 2, ReportFieldId = 6 },
            new { Id = 22, ReportTypeId = 2, ReportFieldId = 7 },
            new { Id = 23, ReportTypeId = 2, ReportFieldId = 14 },
            new { Id = 24, ReportTypeId = 2, ReportFieldId = 15 },
            new { Id = 25, ReportTypeId = 2, ReportFieldId = 24 },
            new { Id = 26, ReportTypeId = 2, ReportFieldId = 27 },
            new { Id = 27, ReportTypeId = 2, ReportFieldId = 29 },
            new { Id = 28, ReportTypeId = 2, ReportFieldId = 30 },
            new { Id = 29, ReportTypeId = 2, ReportFieldId = 28 },
            new { Id = 30, ReportTypeId = 2, ReportFieldId = 13 },
            new { Id = 31, ReportTypeId = 3, ReportFieldId = 1 },
            new { Id = 32, ReportTypeId = 3, ReportFieldId = 2 },
            new { Id = 33, ReportTypeId = 3, ReportFieldId = 3 },
            new { Id = 34, ReportTypeId = 3, ReportFieldId = 4 },
            new { Id = 35, ReportTypeId = 3, ReportFieldId = 5 },
            new { Id = 36, ReportTypeId = 3, ReportFieldId = 6 },
            new { Id = 37, ReportTypeId = 3, ReportFieldId = 7 },
            new { Id = 38, ReportTypeId = 3, ReportFieldId = 14 },
            new { Id = 39, ReportTypeId = 3, ReportFieldId = 15 },
            new { Id = 40, ReportTypeId = 3, ReportFieldId = 24 },
            new { Id = 41, ReportTypeId = 3, ReportFieldId = 27 },
            new { Id = 42, ReportTypeId = 3, ReportFieldId = 29 },
            new { Id = 43, ReportTypeId = 3, ReportFieldId = 30 },
            new { Id = 44, ReportTypeId = 3, ReportFieldId = 28 },
            new { Id = 45, ReportTypeId = 3, ReportFieldId = 13 },
            // Daily Visit (4)
            new { Id = 46, ReportTypeId = 4, ReportFieldId = 17 },
            new { Id = 47, ReportTypeId = 4, ReportFieldId = 20 },
            new { Id = 48, ReportTypeId = 4, ReportFieldId = 21 },
            new { Id = 49, ReportTypeId = 4, ReportFieldId = 24 },
            new { Id = 50, ReportTypeId = 4, ReportFieldId = 18 },
            new { Id = 51, ReportTypeId = 4, ReportFieldId = 22 },
            new { Id = 52, ReportTypeId = 4, ReportFieldId = 19 },
            new { Id = 53, ReportTypeId = 4, ReportFieldId = 31 },
            new { Id = 54, ReportTypeId = 4, ReportFieldId = 33 },
            new { Id = 55, ReportTypeId = 4, ReportFieldId = 34 },
            // No Show (5), NoFieldReport (6)
            new { Id = 56, ReportTypeId = 5, ReportFieldId = 38 },
            new { Id = 57, ReportTypeId = 6, ReportFieldId = 39 });
    }
}
