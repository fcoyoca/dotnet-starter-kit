using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.EmailSettings;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.EmailSettings.GetEmailSettings;

public sealed class GetEmailSettingsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetEmailSettingsQuery, EmailSettingsDto>
{
    public async ValueTask<EmailSettingsDto> Handle(GetEmailSettingsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Domain.TenantEmailSettings? entity = await dbContext.TenantEmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return new EmailSettingsDto(
                UseCustomSmtp: false,
                Host: null, Port: null, UseSsl: false, Username: null, HasPassword: false,
                FromAddress: null, FromName: null, ReplyTo: null, FooterHtml: null,
                PasswordResetSubject: null, PasswordResetBody: null, PasswordResetFooter: null, UpdatedAtUtc: null);
        }

        return new EmailSettingsDto(
            entity.UseCustomSmtp,
            entity.Host,
            entity.Port,
            entity.UseSsl,
            entity.Username,
            HasPassword: !string.IsNullOrEmpty(entity.Password),
            entity.FromAddress,
            entity.FromName,
            entity.ReplyTo,
            entity.FooterHtml,
            entity.PasswordResetSubject,
            entity.PasswordResetBody,
            entity.PasswordResetFooter,
            entity.UpdatedAtUtc);
    }
}
