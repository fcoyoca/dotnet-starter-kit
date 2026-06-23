using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class TenantEmailSettingsConfiguration : IEntityTypeConfiguration<TenantEmailSettings>
{
    public void Configure(EntityTypeBuilder<TenantEmailSettings> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TenantEmailSettings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UseCustomSmtp).IsRequired();
        builder.Property(x => x.Host).HasMaxLength(256);
        builder.Property(x => x.Username).HasMaxLength(256);
        builder.Property(x => x.Password).HasMaxLength(512);
        builder.Property(x => x.FromAddress).HasMaxLength(256);
        builder.Property(x => x.FromName).HasMaxLength(256);
        builder.Property(x => x.ReplyTo).HasMaxLength(256);
        builder.Property(x => x.FooterHtml).HasMaxLength(16000);
        builder.Property(x => x.PasswordResetSubject).HasMaxLength(256);
        builder.Property(x => x.PasswordResetBody).HasMaxLength(16000);
        builder.Property(x => x.PasswordResetFooter).HasMaxLength(16000);

        builder.Ignore(x => x.DomainEvents);
    }
}
