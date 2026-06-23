using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.EmailSettings;

/// <summary>
/// Upserts the current tenant's email settings. <see cref="Password"/> is optional: pass null to keep the stored
/// password unchanged; pass a value to replace it.
/// </summary>
public sealed record UpdateEmailSettingsCommand(
    bool UseCustomSmtp,
    string? Host = null,
    int? Port = null,
    bool UseSsl = false,
    string? Username = null,
    string? Password = null,
    string? FromAddress = null,
    string? FromName = null,
    string? ReplyTo = null,
    string? FooterHtml = null,
    string? PasswordResetSubject = null,
    string? PasswordResetBody = null,
    string? PasswordResetFooter = null) : ICommand<Unit>;
