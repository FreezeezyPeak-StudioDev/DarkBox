using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using DarkBox.Core.Models;

namespace DarkBox.Core.Security;

public static class VaultItemCipher
{
    public static byte[] Encrypt(ReadOnlySpan<byte> vaultKey, long userId, Guid itemId, VaultItemPayload payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);

        try
        {
            return AeadCipher.Encrypt(vaultKey, json, BuildAssociatedData(userId, itemId));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }
    }

    public static VaultItemPayload Decrypt(
        ReadOnlySpan<byte> vaultKey,
        long userId,
        Guid itemId,
        ReadOnlySpan<byte> blob)
    {
        var json = AeadCipher.Decrypt(vaultKey, blob, BuildAssociatedData(userId, itemId));

        try
        {
            return JsonSerializer.Deserialize<VaultItemPayload>(json)
                ?? throw new CryptographicException("El contenido descifrado no es válido.");
        }
        catch (JsonException exception)
        {
            throw new CryptographicException("El contenido descifrado no es válido.", exception);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(json);
        }
    }

    private static byte[] BuildAssociatedData(long userId, Guid itemId)
    {
        return AeadCipher.BuildAssociatedData(
            "vault-item",
            userId.ToString(CultureInfo.InvariantCulture),
            itemId.ToString("D"));
    }
}
