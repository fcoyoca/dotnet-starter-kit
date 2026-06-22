namespace FSH.Modules.Administration.Contracts.Dtos;

/// <summary>
/// The tenant's outbound email configuration. <see cref="HasPassword"/> reflects whether a custom SMTP password is
/// stored; the password itself is write-only and never returned.
/// </summary>
public sealed record EmailSettingsDto(
    bool UseCustomSmtp,
    string? Host,
    int? Port,
    bool UseSsl,
    string? Username,
    bool HasPassword,
    string? FromAddress,
    string? FromName,
    string? ReplyTo,
    DateTime? UpdatedAtUtc);
