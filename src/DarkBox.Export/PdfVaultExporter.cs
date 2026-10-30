using DarkBox.Core.Models;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace DarkBox.Export;

public static class PdfVaultExporter
{
    static PdfVaultExporter()
    {
        GlobalFontSettings.FontResolver = new WindowsFontResolver();
    }

    public static void Export(string path, IReadOnlyList<VaultItem> items, string owner)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(items);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var document = new PdfDocument();
        document.Info.Title = "DarkBox - Credenciales";
        document.Info.Author = "DarkBox";
        document.Info.Subject = $"Bóveda de {owner}";

        using var layout = new PdfVaultLayout(document, owner);
        layout.Write(items);
        layout.Finish();

        document.Save(fullPath);
    }
}
