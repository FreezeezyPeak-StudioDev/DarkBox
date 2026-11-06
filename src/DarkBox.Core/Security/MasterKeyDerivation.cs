using System.Security.Cryptography;

namespace DarkBox.Core.Security;

public static class MasterKeyDerivation
{
    public static byte[] DeriveKey(string password, ReadOnlySpan<byte> salt, ProtectionParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(parameters);
        parameters.Validate();

        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("La contraseña no puede estar vacía.", nameof(password));

        if (salt.Length != parameters.SaltSize)
            throw new ArgumentException("El tamaño de la sal no es válido.", nameof(salt));

        return Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            parameters.Iterations,
            HashAlgorithmName.SHA256,
            parameters.KeySize);
    }
}
