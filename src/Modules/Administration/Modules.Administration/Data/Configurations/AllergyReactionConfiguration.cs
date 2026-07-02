using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class AllergyReactionConfiguration : IEntityTypeConfiguration<AllergyReaction>
{
    public void Configure(EntityTypeBuilder<AllergyReaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AllergyReactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        builder.Property(x => x.Term).IsRequired().HasMaxLength(256);
        builder.Property(x => x.SnomedCode).HasMaxLength(32);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.Term).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.Ignore(x => x.DomainEvents);
        builder.HasData(
            new { Id = 1, Term = "Rash", SnomedCode = (string?)"271807003", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 2, Term = "Hives", SnomedCode = (string?)"126485001", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 3, Term = "Anaphylaxis", SnomedCode = (string?)"39579001", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 4, Term = "Nausea", SnomedCode = (string?)"422587007", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 5, Term = "Vomiting", SnomedCode = (string?)"422400008", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 6, Term = "Swelling", SnomedCode = (string?)"65124004", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 7, Term = "Itching", SnomedCode = (string?)"418290006", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 8, Term = "Shortness of breath", SnomedCode = (string?)"267036007", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 9, Term = "Diarrhea", SnomedCode = (string?)"62315008", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null },
            new { Id = 10, Term = "Cough", SnomedCode = (string?)"49727002", IsActive = true, IsDeleted = false, DeletedOnUtc = (DateTimeOffset?)null, DeletedBy = (string?)null });
    }
}
