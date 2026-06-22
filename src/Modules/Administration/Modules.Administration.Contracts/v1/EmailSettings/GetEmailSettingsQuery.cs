using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.EmailSettings;

public sealed record GetEmailSettingsQuery : IQuery<EmailSettingsDto>;
