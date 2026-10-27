namespace BaseCleanArchitecture.Application.Abstractions.Auditing
{
    public interface IAuditEncryptor
    {
        string Encrypt(string plaintext);

        string Decrypt(string ciphertext);
    }
}
