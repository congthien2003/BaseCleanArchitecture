using System.Security.Cryptography;
using System.Text;
using BaseCleanArchitecture.Application.Abstractions.Authentication;

namespace BaseCleanArchitecture.Infrastructure.Auth;

public sealed class OtpService : IOtpService
{
    public TimeSpan Lifetime => TimeSpan.FromMinutes(10);

    public string GenerateCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public string Hash(Guid userId, string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{userId:N}:{code}")));

    public bool Verify(Guid userId, string code, string hash)
    {
        var expected = Encoding.UTF8.GetBytes(Hash(userId, code));
        var actual = Encoding.UTF8.GetBytes(hash);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
