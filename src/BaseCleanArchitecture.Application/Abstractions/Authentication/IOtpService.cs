namespace BaseCleanArchitecture.Application.Abstractions.Authentication;

public interface IOtpService
{
    TimeSpan Lifetime { get; }

    /// <summary>Cryptographically random 6-digit code.</summary>
    string GenerateCode();

    string Hash(Guid userId, string code);

    bool Verify(Guid userId, string code, string hash);
}
