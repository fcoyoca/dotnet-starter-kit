using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class TenantEmailSettingsTests
{
    [Fact]
    public void CreateDefault_Should_StartWithSystemServer()
    {
        var settings = TenantEmailSettings.CreateDefault();

        settings.Id.ShouldNotBe(Guid.Empty);
        settings.UseCustomSmtp.ShouldBeFalse();
        settings.Host.ShouldBeNull();
        settings.Password.ShouldBeNull();
        settings.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Update_Should_SetFields_And_TrimStrings()
    {
        var settings = TenantEmailSettings.CreateDefault();

        settings.Update(
            useCustomSmtp: true,
            host: "  smtp.example.com  ",
            port: 587,
            useSsl: true,
            username: "  mailer  ",
            fromAddress: "  clinic@example.com  ",
            fromName: "  Clinic  ",
            replyTo: "  reply@example.com  ",
            footerHtml: "<p>Sent by the clinic</p>");

        settings.UseCustomSmtp.ShouldBeTrue();
        settings.Host.ShouldBe("smtp.example.com");
        settings.Port.ShouldBe(587);
        settings.UseSsl.ShouldBeTrue();
        settings.Username.ShouldBe("mailer");
        settings.FromAddress.ShouldBe("clinic@example.com");
        settings.FromName.ShouldBe("Clinic");
        settings.ReplyTo.ShouldBe("reply@example.com");
        settings.FooterHtml.ShouldBe("<p>Sent by the clinic</p>");
        settings.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void SetPassword_Null_Should_KeepExisting()
    {
        var settings = TenantEmailSettings.CreateDefault();
        settings.SetPassword("secret");

        settings.SetPassword(null);

        settings.Password.ShouldBe("secret");
    }

    [Fact]
    public void SetPassword_Value_Should_Replace()
    {
        var settings = TenantEmailSettings.CreateDefault();
        settings.SetPassword("secret");

        settings.SetPassword("rotated");

        settings.Password.ShouldBe("rotated");
    }

    [Fact]
    public void SetPassword_Blank_Should_Clear()
    {
        var settings = TenantEmailSettings.CreateDefault();
        settings.SetPassword("secret");

        settings.SetPassword("   ");

        settings.Password.ShouldBeNull();
    }
}
