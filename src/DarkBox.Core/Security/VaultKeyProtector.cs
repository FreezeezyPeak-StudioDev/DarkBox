using System.Security.Cryptography;

namespace DarkBox.Core.Security;

public static class VaultKeyProtector
{
    public const int VaultKeySize = 32;

    public static byte[] CreateVaultKey()
    {
        var key = new byte[VaultKeySize];
        RandomNumberGenerator.Fill(key);
        return key;
    }

    public static byte[] Wrap(ReadOnlySpan<byte> masterKey, ReadOnlySpan<byte> vaultKey, Guid vaultId)
    {
        if (vaultKey.Length != VaultKeySize)
            throw new ArgumentException("La clave de bóveda debe tener 32 bytes.", nameof(vaultKey));

        return AeadCipher.Encrypt(masterKey, vaultKey, BuildAssociatedData(vaultId));
    }

    public static bool TryUnwrap(
        ReadOnlySpan<byte> masterKey,
        ReadOnlySpan<byte> wrappedVaultKey,
        Guid vaultId,
        out byte[] vaultKey)
    {
        try
        {
            vaultKey = AeadCipher.Decrypt(masterKey, wrappedVaultKey, BuildAssociatedData(vaultId));

            if (vaultKey.Length == VaultKeySize)
                return true;

            CryptographicOperations.ZeroMemory(vaultKey);
            vaultKey = Array.Empty<byte>();
            return false;
        }
        catch (CryptographicException)
        {
            vaultKey = Array.Empty<byte>();
            return false;
        }
    }

    private static byte[] BuildAssociatedData(Guid vaultId)
    {
        return AeadCipher.BuildAssociatedData("vault-key", vaultId.ToString("D"));
    }
}
