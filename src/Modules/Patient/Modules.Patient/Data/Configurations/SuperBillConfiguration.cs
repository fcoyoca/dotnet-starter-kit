using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class SuperBillConfiguration : IEntityTypeConfiguration<SuperBill>
{
    public void Configure(EntityTypeBuilder<SuperBill> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SuperBills");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.IsBilled).IsRequired();

        // One super bill per report (legacy sbReportID uniqueness).
        builder.HasIndex(x => x.ReportId).IsUnique();
        builder.HasIndex(x => x.PatientId);

        builder.HasMany(x => x.Procedures)
            .WithOne()
            .HasForeignKey(p => p.SuperBillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
