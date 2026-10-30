using System.Security.Cryptography;
using System.Text;

namespace DarkBox.Core.Security;

public static class AeadCipher
{
    public const int KeySize = 32;
    public const int NonceSize = 12;
    public const int TagSize = 16;

    private const byte CurrentFormatVersion = 1;
    private const int HeaderSize = 1 + NonceSize + TagSize;

    public static byte[] Encrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> associatedData)
    {
        if (key.Length != KeySize)
            throw new ArgumentException("La clave AES-GCM debe tener 32 bytes.", nameof(key));

        var blob = new byte[HeaderSize + plaintext.Length];
        blob[0] = CurrentFormatVersion;

        var nonce = blob.AsSpan(1, NonceSize);
        var tag = blob.AsSpan(1 + NonceSize, TagSize);
        var ciphertext = blob.AsSpan(HeaderSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);

        return blob;
    }

    public static byte[] Decrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> blob, ReadOnlySpan<byte> associatedData)
    {
        if (key.Length != KeySize)
            throw new ArgumentException("La clave AES-GCM debe tener 32 bytes.", nameof(key));

        if (blob.Length < HeaderSize)
            throw new CryptographicException("El dato cifrado está incompleto.");

        if (blob[0] != CurrentFormatVersion)
            throw new CryptographicException("La versión del formato cifrado no es compatible.");

        var nonce = blob.Slice(1, NonceSize);
        var tag = blob.Slice(1 + NonceSize, TagSize);
        var ciphertext = blob.Slice(HeaderSize);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            return plaintext;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }
    }

    public static byte[] BuildAssociatedData(params string[] parts)
    {
        return Encoding.UTF8.GetBytes("DarkBox|v1|" + string.Join("|", parts));
    }
}
