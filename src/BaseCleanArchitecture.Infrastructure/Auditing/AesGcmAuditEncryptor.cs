using System.Security.Cryptography;
using System.Text;
using BaseCleanArchitecture.Application.Abstractions.Auditing;
using Microsoft.Extensions.Options;

namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    /// <summary>
    /// AES-256-GCM. Output: "v1:" + base64(nonce[12] | tag[16] | ciphertext). The version prefix leaves room for key rotation.
    /// </summary>
    public sealed class AesGcmAuditEncryptor : IAuditEncryptor
    {
        private const string VersionPrefix = "v1:";
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private readonly byte[] _key;

        public AesGcmAuditEncryptor(IOptions<AuditEncryptionOptions> options)
        {
            var key = options.Value.Key;
            if (!AuditEncryptionOptions.IsValidKey(key))
            {
                throw new InvalidOperationException(
                    $"{AuditEncryptionOptions.SectionName}:Key must be a base64 string of {AuditEncryptionOptions.KeySizeBytes} bytes.");
            }

            _key = Convert.FromBase64String(key);
        }

        public string Encrypt(string plaintext)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var payload = new byte[NonceSize + TagSize + plainBytes.Length];

            var nonce = payload.AsSpan(0, NonceSize);
            var tag = payload.AsSpan(NonceSize, TagSize);
            var cipher = payload.AsSpan(NonceSize + TagSize);

            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(_key, TagSize);
            aes.Encrypt(nonce, plainBytes, cipher, tag);

            return VersionPrefix + Convert.ToBase64String(payload);
        }

        public string Decrypt(string ciphertext)
        {
            if (!ciphertext.StartsWith(VersionPrefix, StringComparison.Ordinal))
            {
                throw new CryptographicException("Unknown audit ciphertext version.");
            }

            byte[] payload;
            try
            {
                payload = Convert.FromBase64String(ciphertext[VersionPrefix.Length..]);
            }
            catch (FormatException ex)
            {
                throw new CryptographicException("Audit ciphertext is not valid base64.", ex);
            }

            if (payload.Length < NonceSize + TagSize)
            {
                throw new CryptographicException("Audit ciphertext is too short.");
            }

            var nonce = payload.AsSpan(0, NonceSize);
            var tag = payload.AsSpan(NonceSize, TagSize);
            var cipher = payload.AsSpan(NonceSize + TagSize);
            var plainBytes = new byte[cipher.Length];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, cipher, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
