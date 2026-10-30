using System.Security.Cryptography;
using DarkBox.Core.Models;
using DarkBox.Core.Security;

namespace DarkBox.Tests;

public sealed class CoreCryptographyTests
{
    private static ProtectionParameters FastParameters => new() { Iterations = 10_000 };

    [Fact]
    public void DeriveKey_IsDeterministic()
    {
        var parameters = FastParameters;
        var salt = RandomNumberGenerator.GetBytes(parameters.SaltSize);

        var first = MasterKeyDerivation.DeriveKey("correct horse battery staple", salt, parameters);
        var second = MasterKeyDerivation.DeriveKey("correct horse battery staple", salt, parameters);

        Assert.Equal(first, second);
        Assert.Equal(32, first.Length);
    }

    [Fact]
    public void DeriveKey_DifferentSalts_ProduceDifferentKeys()
    {
        var parameters = FastParameters;
        var firstSalt = RandomNumberGenerator.GetBytes(parameters.SaltSize);
        var secondSalt = RandomNumberGenerator.GetBytes(parameters.SaltSize);

        var first = MasterKeyDerivation.DeriveKey("correct horse battery staple", firstSalt, parameters);
        var second = MasterKeyDerivation.DeriveKey("correct horse battery staple", secondSalt, parameters);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void VaultKey_WrapAndUnwrap_RoundTrips()
    {
        var masterKey = RandomNumberGenerator.GetBytes(32);
        var vaultId = Guid.NewGuid();
        var vaultKey = VaultKeyProtector.CreateVaultKey();

        var wrapped = VaultKeyProtector.Wrap(masterKey, vaultKey, vaultId);

        Assert.True(VaultKeyProtector.TryUnwrap(masterKey, wrapped, vaultId, out var unwrapped));
        Assert.Equal(vaultKey, unwrapped);
    }

    [Fact]
    public void VaultKey_WrongMasterKey_Fails()
    {
        var masterKey = RandomNumberGenerator.GetBytes(32);
        var wrongKey = RandomNumberGenerator.GetBytes(32);
        var vaultId = Guid.NewGuid();
        var vaultKey = VaultKeyProtector.CreateVaultKey();

        var wrapped = VaultKeyProtector.Wrap(masterKey, vaultKey, vaultId);

        Assert.False(VaultKeyProtector.TryUnwrap(wrongKey, wrapped, vaultId, out _));
    }

    [Fact]
    public void VaultKey_WrongVaultId_Fails()
    {
        var masterKey = RandomNumberGenerator.GetBytes(32);
        var vaultId = Guid.NewGuid();
        var vaultKey = VaultKeyProtector.CreateVaultKey();

        var wrapped = VaultKeyProtector.Wrap(masterKey, vaultKey, vaultId);

        Assert.False(VaultKeyProtector.TryUnwrap(masterKey, wrapped, Guid.NewGuid(), out _));
    }

    [Fact]
    public void VaultItem_RoundTrips()
    {
        var vaultKey = VaultKeyProtector.CreateVaultKey();
        var itemId = Guid.NewGuid();
        var payload = new VaultItemPayload(
            "GitHub",
            "jarvio",
            "super-secreta",
            "https://github.com",
            "Cuenta principal");

        var blob = VaultItemCipher.Encrypt(vaultKey, 7, itemId, payload);
        var result = VaultItemCipher.Decrypt(vaultKey, 7, itemId, blob);

        Assert.Equal(payload, result);
    }

    [Fact]
    public void VaultItem_TamperedBlob_Fails()
    {
        var vaultKey = VaultKeyProtector.CreateVaultKey();
        var itemId = Guid.NewGuid();
        var payload = new VaultItemPayload("GitHub", "jarvio", "super-secreta");

        var blob = VaultItemCipher.Encrypt(vaultKey, 7, itemId, payload);
        blob[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() =>
            VaultItemCipher.Decrypt(vaultKey, 7, itemId, blob));
    }

    [Fact]
    public void VaultItem_WrongAssociatedData_Fails()
    {
        var vaultKey = VaultKeyProtector.CreateVaultKey();
        var itemId = Guid.NewGuid();
        var payload = new VaultItemPayload("GitHub", "jarvio", "super-secreta");

        var blob = VaultItemCipher.Encrypt(vaultKey, 7, itemId, payload);

        Assert.ThrowsAny<CryptographicException>(() =>
            VaultItemCipher.Decrypt(vaultKey, 8, itemId, blob));

        Assert.ThrowsAny<CryptographicException>(() =>
            VaultItemCipher.Decrypt(vaultKey, 7, Guid.NewGuid(), blob));
    }

    [Fact]
    public void VaultItem_UnknownFormatVersion_Fails()
    {
        var vaultKey = VaultKeyProtector.CreateVaultKey();
        var itemId = Guid.NewGuid();
        var payload = new VaultItemPayload("GitHub", "jarvio", "super-secreta");
        var blob = VaultItemCipher.Encrypt(vaultKey, 7, itemId, payload);

        blob[0] = 99;

        Assert.Throws<CryptographicException>(() =>
            VaultItemCipher.Decrypt(vaultKey, 7, itemId, blob));
    }

    [Fact]
    public void VaultSession_Dispose_ZeroesKey()
    {
        var vaultKey = VaultKeyProtector.CreateVaultKey();
        var session = new VaultSession(1, "jarvio", Guid.NewGuid(), vaultKey);

        session.Dispose();

        Assert.All(vaultKey, value => Assert.Equal(0, value));
        Assert.Throws<ObjectDisposedException>(() => session.VaultKey);
    }

    [Theory]
    [InlineData("abc", true)]
    [InlineData("usuario valido", false)]
    [InlineData("jarvio", true)]
    [InlineData("jarvio.dev", true)]
    [InlineData("ab", false)]
    public void AccountPolicy_ValidatesUsernames(string username, bool expectedValid)
    {
        Assert.Equal(expectedValid, AccountPolicy.ValidateUsername(username) is null);
    }

    [Theory]
    [InlineData("corta", false)]
    [InlineData("una contraseña maestra", true)]
    public void AccountPolicy_ValidatesPasswordLength(string password, bool expectedValid)
    {
        Assert.Equal(expectedValid, AccountPolicy.ValidatePassword(password) is null);
    }
}
