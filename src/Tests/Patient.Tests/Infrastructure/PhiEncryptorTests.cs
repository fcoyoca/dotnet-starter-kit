using FSH.Modules.Patient.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Patient.Tests.Infrastructure;

public sealed class PhiEncryptorTests
{
    private static PhiEncryptor Build()
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var sp = services.BuildServiceProvider();
        return new PhiEncryptor(sp.GetRequiredService<IDataProtectionProvider>());
    }

    #region Encrypt / Decrypt roundtrip

    [Fact]
    public void EncryptDecrypt_Should_Roundtrip_Plaintext()
    {
        var phi = Build();
        string original = "123-45-6789";

        string? encrypted = phi.Encrypt(original);
        string? decrypted = phi.Decrypt(encrypted);

        encrypted.ShouldNotBe(original);
        decrypted.ShouldBe(original);
    }

    [Fact]
    public void Encrypt_Should_ReturnNull_When_InputIsNull()
    {
        var phi = Build();
        phi.Encrypt(null).ShouldBeNull();
    }

    [Fact]
    public void Encrypt_Should_ReturnNull_When_InputIsEmpty()
    {
        var phi = Build();
        phi.Encrypt(string.Empty).ShouldBeNull();
    }

    [Fact]
    public void Decrypt_Should_ReturnNull_When_InputIsNull()
    {
        var phi = Build();
        phi.Decrypt(null).ShouldBeNull();
    }

    [Fact]
    public void Encrypt_Should_ProduceDifferentCiphertext_ForSamePlaintext()
    {
        // ASP.NET Data Protection uses random IV — same input produces different ciphertext
        var phi = Build();
        string original = "999-99-9999";

        string? a = phi.Encrypt(original);
        string? b = phi.Encrypt(original);

        a.ShouldNotBe(b);
    }

    #endregion

    #region HashForSearch

    [Fact]
    public void HashForSearch_Should_ReturnSameHash_ForSameInput()
    {
        var phi = Build();

        string? h1 = phi.HashForSearch("123-45-6789");
        string? h2 = phi.HashForSearch("123-45-6789");

        h1.ShouldBe(h2);
    }

    [Fact]
    public void HashForSearch_Should_ReturnDifferentHash_ForDifferentInput()
    {
        var phi = Build();

        string? h1 = phi.HashForSearch("111-11-1111");
        string? h2 = phi.HashForSearch("222-22-2222");

        h1.ShouldNotBe(h2);
    }

    [Fact]
    public void HashForSearch_Should_ReturnLowercaseHex()
    {
        var phi = Build();
        string? hash = phi.HashForSearch("123-45-6789");

        hash.ShouldNotBeNull();
#pragma warning disable CA1308 // verifying hash is canonical lowercase, not security-sensitive
        hash.ShouldBe(hash!.ToLowerInvariant());
#pragma warning restore CA1308
        hash.Length.ShouldBe(64); // HMAC-SHA256 → 32 bytes → 64 hex chars
    }

    [Fact]
    public void HashForSearch_Should_ReturnNull_When_InputIsNull()
    {
        var phi = Build();
        phi.HashForSearch(null).ShouldBeNull();
    }

    [Fact]
    public void HashForSearch_Should_TrimInputBeforeHashing()
    {
        var phi = Build();

        string? h1 = phi.HashForSearch("  123-45-6789  ");
        string? h2 = phi.HashForSearch("123-45-6789");

        h1.ShouldBe(h2);
    }

    #endregion
}
