using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// Per-tenant outbound email configuration (legacy ClientSettings <c>USE_CUSTOM_EMAIL_SERVER</c> + <c>EMAIL_*</c>).
/// A single row per tenant (tenant-scoped — not <see cref="IGlobalEntity"/>). When <see cref="UseCustomSmtp"/> is
/// false the system/root default mail server is used; when true the tenant's own SMTP fields apply.
/// </summary>
public sealed class TenantEmailSettings : AggregateRoot<Guid>
{
    /// <summary>When false, the system default mail server (root <c>MailOptions</c>) is used.</summary>
    public bool UseCustomSmtp { get; private set; }

    public string? Host { get; private set; }
    public int? Port { get; private set; }
    public bool UseSsl { get; private set; }
    public string? Username { get; private set; }

    /// <summary>SMTP password — write-only via the API (never returned in DTOs).</summary>
    public string? Password { get; private set; }

    public string? FromAddress { get; private set; }
    public string? FromName { get; private set; }
    public string? ReplyTo { get; private set; }

    /// <summary>HTML footer appended to outbound email (legacy <c>EMAIL_FOOTER</c>).</summary>
    public string? FooterHtml { get; private set; }

    /// <summary>Subject line of the BackChart password-reset email (legacy <c>BC_EMAIL_RESETPASS_TITLE</c>).</summary>
    public string? PasswordResetSubject { get; private set; }

    /// <summary>HTML body of the BackChart password-reset email (legacy <c>BC_EMAIL_RESETPASS_BODY</c>).</summary>
    public string? PasswordResetBody { get; private set; }

    /// <summary>HTML footer of the BackChart password-reset email (legacy <c>BC_EMAIL_RESETPASS_FOOTER</c>).</summary>
    public string? PasswordResetFooter { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private TenantEmailSettings() { }

    public static TenantEmailSettings CreateDefault()
    {
        return new TenantEmailSettings
        {
            Id = Guid.CreateVersion7(),
            UseCustomSmtp = false,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        bool useCustomSmtp,
        string? host,
        int? port,
        bool useSsl,
        string? username,
        string? fromAddress,
        string? fromName,
        string? replyTo,
        string? footerHtml,
        string? passwordResetSubject,
        string? passwordResetBody,
        string? passwordResetFooter)
    {
        UseCustomSmtp = useCustomSmtp;
        Host = Trim(host);
        Port = port;
        UseSsl = useSsl;
        Username = Trim(username);
        FromAddress = Trim(fromAddress);
        FromName = Trim(fromName);
        ReplyTo = Trim(replyTo);
        FooterHtml = Blank(footerHtml);
        PasswordResetSubject = Trim(passwordResetSubject);
        PasswordResetBody = Blank(passwordResetBody);
        PasswordResetFooter = Blank(passwordResetFooter);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Sets the SMTP password. Pass null to leave the stored password unchanged.</summary>
    public void SetPassword(string? password)
    {
        if (password is null)
        {
            return;
        }

        Password = string.IsNullOrWhiteSpace(password) ? null : password;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Nulls blank HTML but preserves internal/leading whitespace and markup.</summary>
    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
