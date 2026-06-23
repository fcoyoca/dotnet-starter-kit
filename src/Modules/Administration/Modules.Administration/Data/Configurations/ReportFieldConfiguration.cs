using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class ReportFieldConfiguration : IEntityTypeConfiguration<ReportField>
{
    public void Configure(EntityTypeBuilder<ReportField> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ReportFields");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Category).HasMaxLength(128);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasOne<ReportType>()
            .WithMany()
            .HasForeignKey(x => x.ReportTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ReportTypeId);
        builder.HasIndex(x => x.LegacyId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => new { x.ReportTypeId, x.Name }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");

        builder.Ignore(x => x.DomainEvents);
    }
}
