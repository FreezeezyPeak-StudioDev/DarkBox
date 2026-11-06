using System.Security.Cryptography;
using DarkBox.Core.Models;
using DarkBox.Core.Security;

namespace DarkBox.Tests;

public sealed class VaultPackageTests : IDisposable
{
    private const string Password = "correct horse battery staple";
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"darkbox-test-{Guid.NewGuid():N}.darkbox");

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public async Task WriteAndRead_RoundTripsPayloads()
    {
        var items = new List<VaultItemPayload>
        {
            new("GitHub", "usuario1", "clave-secreta-1", "https://github.com", "nota uno"),
            new("Netflix", "usuario2", "clave-secreta-2"),
        };

        await VaultPackage.WriteAsync(_path, Password, items);
        var result = await VaultPackage.ReadAsync(_path, Password);

        Assert.Equal(items.Count, result.Count);
        Assert.Equal(items[0].Service, result[0].Service);
        Assert.Equal(items[0].Username, result[0].Username);
        Assert.Equal(items[0].Password, result[0].Password);
        Assert.Equal(items[0].Url, result[0].Url);
        Assert.Equal(items[0].Notes, result[0].Notes);
        Assert.Equal(items[1].Service, result[1].Service);
    }

    [Fact]
    public async Task Read_WrongPassword_ThrowsCryptographicException()
    {
        await VaultPackage.WriteAsync(_path, Password, new[] { new VaultItemPayload("Servicio", "user", "pass") });

        await Assert.ThrowsAnyAsync<CryptographicException>(() =>
            VaultPackage.ReadAsync(_path, "otra contraseña distinta"));
    }

    [Fact]
    public async Task Read_InvalidFile_ThrowsFormatException()
    {
        await File.WriteAllTextAsync(_path, "esto no es un paquete darkbox");

        await Assert.ThrowsAsync<FormatException>(() =>
            VaultPackage.ReadAsync(_path, Password));
    }

    [Fact]
    public async Task Write_EmptyPassword_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            VaultPackage.WriteAsync(_path, "", Array.Empty<VaultItemPayload>()));
    }
}
