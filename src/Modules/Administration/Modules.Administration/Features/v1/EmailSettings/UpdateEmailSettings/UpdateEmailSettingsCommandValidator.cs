using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.EmailSettings;

namespace FSH.Modules.Administration.Features.v1.EmailSettings.UpdateEmailSettings;

public sealed class UpdateEmailSettingsCommandValidator : AbstractValidator<UpdateEmailSettingsCommand>
{
    public UpdateEmailSettingsCommandValidator()
    {
        RuleFor(x => x.Host).MaximumLength(256);
        RuleFor(x => x.Username).MaximumLength(256);
        RuleFor(x => x.Password).MaximumLength(512);
        RuleFor(x => x.FromAddress).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.FromAddress));
        RuleFor(x => x.FromName).MaximumLength(256);
        RuleFor(x => x.ReplyTo).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.ReplyTo));

        // When the tenant opts into a custom SMTP server, the host and port are required.
        When(x => x.UseCustomSmtp, () =>
        {
            RuleFor(x => x.Host).NotEmpty();
            RuleFor(x => x.Port).NotNull().InclusiveBetween(1, 65535);
        });
    }
}
