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
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Category).HasMaxLength(128);
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);
        builder.Ignore(x => x.DomainEvents);

        builder.HasData(
            new { Id = 1, Name = "Chief Complaint", Category = "Chief Complaint", DisplayOrder = 1, IsActive = true },
            new { Id = 2, Name = "Present Problem", Category = "Present Problem", DisplayOrder = 1, IsActive = true },
            new { Id = 3, Name = "Medical History", Category = "Medical History", DisplayOrder = 1, IsActive = true },
            new { Id = 4, Name = "Personal / Social History", Category = "Personal / Social History", DisplayOrder = 1, IsActive = true },
            new { Id = 5, Name = "Allergies", Category = "Allergies", DisplayOrder = 1, IsActive = true },
            new { Id = 6, Name = "Medications", Category = "Medications", DisplayOrder = 1, IsActive = true },
            new { Id = 7, Name = "Systems Review", Category = "Systems Review", DisplayOrder = 1, IsActive = true },
            new { Id = 13, Name = "Comments", Category = "Clinical Exam", DisplayOrder = 7, IsActive = true },
            new { Id = 14, Name = "Diagnostic Imaging", Category = "Diagnostic Imaging", DisplayOrder = 1, IsActive = true },
            new { Id = 15, Name = "Clinical Impression", Category = "Clinical Impression", DisplayOrder = 1, IsActive = true },
            new { Id = 16, Name = "Therapeutic Care", Category = "Therapeutic Care", DisplayOrder = 1, IsActive = true },
            new { Id = 17, Name = "ADL", Category = "Subjective", DisplayOrder = 1, IsActive = false },
            new { Id = 18, Name = "Pain", Category = "Subjective", DisplayOrder = 2, IsActive = false },
            new { Id = 19, Name = "Subjective", Category = "Subjective", DisplayOrder = 3, IsActive = true },
            new { Id = 20, Name = "Objective", Category = "Objective", DisplayOrder = 1, IsActive = true },
            new { Id = 21, Name = "Assessment", Category = "Assessment", DisplayOrder = 1, IsActive = false },
            new { Id = 22, Name = "Assessment", Category = "Assessment", DisplayOrder = 2, IsActive = true },
            new { Id = 24, Name = "Plan", Category = "Plan", DisplayOrder = 1, IsActive = true },
            new { Id = 27, Name = "Short Term Goals", Category = "Goals", DisplayOrder = 1, IsActive = true },
            new { Id = 28, Name = "Long Term Goals", Category = "Goals", DisplayOrder = 2, IsActive = true },
            new { Id = 29, Name = "Work Status or Restrictions", Category = "Work Status or Restrictions", DisplayOrder = 1, IsActive = true },
            new { Id = 30, Name = "Family History", Category = "Family History", DisplayOrder = 1, IsActive = true },
            new { Id = 31, Name = "Niall Radio Group", Category = "Assessment", DisplayOrder = 3, IsActive = false },
            new { Id = 33, Name = "Niall's yes/no", Category = "Assessment", DisplayOrder = 5, IsActive = false },
            new { Id = 34, Name = "Niall's drop down", Category = "Assessment", DisplayOrder = 6, IsActive = false },
            new { Id = 38, Name = "Comments", Category = "Comments", DisplayOrder = 1, IsActive = true },
            new { Id = 39, Name = "Documentation", Category = "Documentation", DisplayOrder = 1, IsActive = true });
    }
}
