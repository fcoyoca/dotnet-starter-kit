using FSH.Modules.Administration.Contracts.v1.EmailSettings;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.EmailSettings.UpdateEmailSettings;

public sealed class UpdateEmailSettingsCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateEmailSettingsCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateEmailSettingsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        TenantEmailSettings? entity = await dbContext.TenantEmailSettings
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = TenantEmailSettings.CreateDefault();
            dbContext.TenantEmailSettings.Add(entity);
        }

        entity.Update(
            command.UseCustomSmtp,
            command.Host,
            command.Port,
            command.UseSsl,
            command.Username,
            command.FromAddress,
            command.FromName,
            command.ReplyTo);
        entity.SetPassword(command.Password);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
