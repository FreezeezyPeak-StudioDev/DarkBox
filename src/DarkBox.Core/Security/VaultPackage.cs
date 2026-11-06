using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DarkBox.Core.Models;

namespace DarkBox.Core.Security;

/// <summary>
/// Formato de archivo .darkbox: bóveda exportada cifrada con una contraseña independiente.
/// Permite mover credenciales entre equipos sin exponer la contraseña maestra de la bóveda.
/// </summary>
public static class VaultPackage
{
    private static readonly byte[] Magic = "DBX1"u8.ToArray();
    private const byte FormatVersion = 1;
    private const int SaltLength = ProtectionParameters.DefaultSaltSize;
    private const int HeaderFixedLength = 4 + 1 + SaltLength + sizeof(int);

    public static async Task WriteAsync(
        string path,
        string password,
        IReadOnlyList<VaultItemPayload> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(items);

        var passwordError = AccountPolicy.ValidatePassword(password);
        if (passwordError is not null)
            throw new ArgumentException(passwordError, nameof(password));

        var parameters = ProtectionParameters.Default;
        parameters.Validate();

        var salt = RandomNumberGenerator.GetBytes(parameters.SaltSize);
        byte[]? masterKey = null;
        byte[]? plaintext = null;
        byte[]? blob = null;

        try
        {
            masterKey = MasterKeyDerivation.DeriveKey(password, salt, parameters);
            plaintext = JsonSerializer.SerializeToUtf8Bytes(items);
            blob = AeadCipher.Encrypt(masterKey, plaintext, BuildAssociatedData());

            var header = BuildHeader(parameters, salt);

            await using var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous | FileOptions.WriteThrough);

            await stream.WriteAsync(header, cancellationToken);
            await stream.WriteAsync(blob, cancellationToken);
            await stream.FlushAsync(cancellationToken);
        }
        finally
        {
            if (plaintext is not null)
                CryptographicOperations.ZeroMemory(plaintext);
            if (blob is not null)
                CryptographicOperations.ZeroMemory(blob);
            if (masterKey is not null)
                CryptographicOperations.ZeroMemory(masterKey);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    public static async Task<IReadOnlyList<VaultItemPayload>> ReadAsync(
        string path,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña del archivo es obligatoria.", nameof(password));

        var data = await File.ReadAllBytesAsync(path, cancellationToken);
        return Parse(data, password);
    }

    private static IReadOnlyList<VaultItemPayload> Parse(byte[] data, string password)
    {
        if (data.Length < HeaderFixedLength)
            throw new FormatException("El archivo está incompleto.");

        var offset = 0;

        for (var index = 0; index < Magic.Length; index++)
        {
            if (data[offset + index] != Magic[index])
                throw new FormatException("El archivo no es un paquete DarkBox válido.");
        }
        offset += Magic.Length;

        if (data[offset++] != FormatVersion)
            throw new FormatException("La versión del archivo no es compatible.");

        var salt = data.AsSpan(offset, SaltLength).ToArray();
        offset += SaltLength;

        var parametersLength = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset));
        offset += sizeof(int);

        if (parametersLength <= 0 || parametersLength > 4096 || offset + parametersLength > data.Length)
            throw new FormatException("Los parámetros del archivo no son válidos.");

        var parametersJson = Encoding.UTF8.GetString(data, offset, parametersLength);
        offset += parametersLength;

        var blob = data.AsSpan(offset).ToArray();

        ProtectionParameters parameters;
        try
        {
            parameters = ProtectionParameters.FromStorageString(parametersJson);
        }
        catch (Exception exception)
        {
            throw new FormatException("Los parámetros del archivo no son válidos.", exception);
        }

        byte[]? masterKey = null;
        byte[]? plaintext = null;

        try
        {
            masterKey = MasterKeyDerivation.DeriveKey(password, salt, parameters);
            plaintext = AeadCipher.Decrypt(masterKey, blob, BuildAssociatedData());

            var items = JsonSerializer.Deserialize<List<VaultItemPayload>>(plaintext)
                ?? throw new FormatException("El contenido del archivo no es válido.");

            foreach (var item in items)
            {
                if (item is null)
                    throw new FormatException("El contenido del archivo no es válido.");

                var validationError = VaultItemRules.Validate(item);
                if (validationError is not null)
                    throw new FormatException("El archivo contiene datos no válidos: " + validationError);
            }

            return items;
        }
        catch (JsonException exception)
        {
            throw new FormatException("El contenido del archivo no es válido.", exception);
        }
        finally
        {
            if (plaintext is not null)
                CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(blob);
            if (masterKey is not null)
                CryptographicOperations.ZeroMemory(masterKey);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    private static byte[] BuildHeader(ProtectionParameters parameters, byte[] salt)
    {
        var parametersJson = Encoding.UTF8.GetBytes(parameters.ToStorageString());
        var header = new byte[Magic.Length + 1 + salt.Length + sizeof(int) + parametersJson.Length];
        var offset = 0;

        Buffer.BlockCopy(Magic, 0, header, offset, Magic.Length);
        offset += Magic.Length;
        header[offset++] = FormatVersion;
        Buffer.BlockCopy(salt, 0, header, offset, salt.Length);
        offset += salt.Length;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset), parametersJson.Length);
        offset += sizeof(int);
        Buffer.BlockCopy(parametersJson, 0, header, offset, parametersJson.Length);

        CryptographicOperations.ZeroMemory(parametersJson);
        return header;
    }

    private static byte[] BuildAssociatedData()
    {
        return AeadCipher.BuildAssociatedData("darkbox-package", "v1");
    }
}
