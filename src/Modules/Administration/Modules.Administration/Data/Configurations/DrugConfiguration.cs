using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Drugs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();

        builder.Property(x => x.Name).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.RxAui).HasMaxLength(12);
        builder.Property(x => x.RxCui).HasMaxLength(12);
        builder.Property(x => x.Tty).HasMaxLength(20);
        builder.Property(x => x.Sab).HasMaxLength(40);
        builder.Property(x => x.Code).HasMaxLength(64);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.RxAui).IsUnique().HasFilter("\"RxAui\" IS NOT NULL");
        builder.HasIndex(x => x.RxCui);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
