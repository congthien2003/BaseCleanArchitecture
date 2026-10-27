namespace BaseCleanArchitecture.Infrastructure.Auditing
{
    public class AuditEncryptionOptions
    {
        public const string SectionName = "AuditEncryption";

        public const int KeySizeBytes = 32;

        /// <summary>Base64 of a 32-byte AES-256 key.</summary>
        public string Key { get; set; } = string.Empty;

        public static bool IsValidKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            Span<byte> buffer = stackalloc byte[KeySizeBytes + 4];
            return Convert.TryFromBase64String(key, buffer, out var written) && written == KeySizeBytes;
        }
    }
}
