using System.Text;
using DarkBox.Core.Models;
using DarkBox.Export;

namespace DarkBox.Tests;

public sealed class PdfExportTests : IDisposable
{
    private readonly string _directory;

    public PdfExportTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "DarkBoxPdfTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void ExportAll_CreatesReadablePdf()
    {
        var items = new[]
        {
            CreateItem("GitHub", "jarvio", "password-segura", "https://github.com", "Cuenta principal"),
            CreateItem("Correo", "jarvio@example.com", "otra-password", null, null),
        };

        var path = Path.Combine(_directory, "todas.pdf");

        PdfVaultExporter.Export(path, items, "jarvio");

        AssertValidPdf(path);
    }

    [Fact]
    public void ExportOne_CreatesReadablePdf()
    {
        var path = Path.Combine(_directory, "una.pdf");

        PdfVaultExporter.Export(
            path,
            new[] { CreateItem("GitHub", "jarvio", "password-segura", null, "Nota con acentos: ñáéíóú") },
            "jarvio");

        AssertValidPdf(path);
    }

    private static VaultItem CreateItem(
        string service,
        string username,
        string password,
        string? url,
        string? notes)
    {
        var now = DateTimeOffset.UtcNow;
        return new VaultItem(Guid.NewGuid(), service, username, password, url, notes, now, now);
    }

    private static void AssertValidPdf(string path)
    {
        Assert.True(File.Exists(path));

        var bytes = File.ReadAllBytes(path);

        Assert.True(bytes.Length > 1_000);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes, 0, 5));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
