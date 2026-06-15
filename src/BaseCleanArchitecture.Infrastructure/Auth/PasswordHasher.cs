using System.Security.Cryptography;
using BaseCleanArchitecture.Application.Abstractions.Authentication;

namespace BaseCleanArchitecture.Infrastructure.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 32;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public (string hash, string salt) Hash(string password)
    {
        var saltBytes = new byte[SaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);

        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool Verify(string password, string hash, string salt)
    {
        var hashBytes = Convert.FromBase64String(hash);
        var saltBytes = Convert.FromBase64String(salt);

        var computedBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(computedBytes, hashBytes);
    }
}
