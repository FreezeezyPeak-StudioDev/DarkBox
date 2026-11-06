using System.Security.Cryptography;

namespace DarkBox.Core.Security;

public sealed class VaultSession : IDisposable
{
    private readonly byte[] _vaultKey;
    private bool _disposed;

    public VaultSession(long userId, string username, Guid vaultId, byte[] vaultKey)
    {
        if (vaultKey is null || vaultKey.Length != VaultKeyProtector.VaultKeySize)
            throw new ArgumentException("La clave de bóveda no es válida.", nameof(vaultKey));

        UserId = userId;
        Username = username;
        VaultId = vaultId;
        _vaultKey = vaultKey;
    }

    public long UserId { get; }

    public string Username { get; }

    public Guid VaultId { get; }

    public byte[] VaultKey => _disposed
        ? throw new ObjectDisposedException(nameof(VaultSession))
        : _vaultKey;

    public void Dispose()
    {
        if (_disposed)
            return;

        CryptographicOperations.ZeroMemory(_vaultKey);
        _disposed = true;
    }
}
