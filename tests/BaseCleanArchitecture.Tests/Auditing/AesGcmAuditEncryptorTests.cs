using System.Security.Cryptography;
using BaseCleanArchitecture.Infrastructure.Auditing;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Tests.Auditing;

public class AesGcmAuditEncryptorTests
{
    public static readonly string TestKey = Convert.ToBase64String(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray());

    private static AesGcmAuditEncryptor Create(string? key = null) =>
        new(Options.Create(new AuditEncryptionOptions { Key = key ?? TestKey }));

    [Theory]
    [InlineData("hello")]
    [InlineData("Xin chào, đây là tiếng Việt có dấu ✓")]
    [InlineData("")]
    public void Encrypt_Then_Decrypt_RoundTrips(string plaintext)
    {
        var encryptor = Create();

        Assert.Equal(plaintext, encryptor.Decrypt(encryptor.Encrypt(plaintext)));
    }

    [Fact]
    public void Encrypt_Output_Starts_With_v1_And_Is_Different_Each_Call()
    {
        var encryptor = Create();

        var first = encryptor.Encrypt("same");
        var second = encryptor.Encrypt("same");

        Assert.StartsWith("v1:", first);
        Assert.NotEqual(first, second);
        Assert.DoesNotContain("same", first);
    }

    [Fact]
    public void Decrypt_Throws_On_Tampered_Ciphertext()
    {
        var encryptor = Create();
        var payload = Convert.FromBase64String(encryptor.Encrypt("secret")["v1:".Length..]);
        payload[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => encryptor.Decrypt("v1:" + Convert.ToBase64String(payload)));
    }

    [Fact]
    public void Decrypt_Throws_On_Unknown_Version_Prefix()
    {
        var encryptor = Create();
        var encrypted = encryptor.Encrypt("secret");

        Assert.ThrowsAny<CryptographicException>(() => encryptor.Decrypt("v2:" + encrypted["v1:".Length..]));
    }

    [Fact]
    public void Decrypt_Throws_With_A_Different_Key()
    {
        var encrypted = Create().Encrypt("secret");
        var otherKey = Convert.ToBase64String(new byte[32]);

        Assert.ThrowsAny<CryptographicException>(() => Create(otherKey).Decrypt(encrypted));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not base64 !!")]
    [InlineData("AAAA")] // valid base64, 3 bytes
    public void IsValidKey_Rejects_Empty_NonBase64_And_Wrong_Length(string? key)
    {
        Assert.False(AuditEncryptionOptions.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_Accepts_32_Byte_Base64()
    {
        Assert.True(AuditEncryptionOptions.IsValidKey(TestKey));
    }

    [Fact]
    public void Constructor_Throws_On_Invalid_Key()
    {
        Assert.Throws<InvalidOperationException>(() => Create("AAAA"));
    }
}
