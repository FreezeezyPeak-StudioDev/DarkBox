using System.Globalization;
using DarkBox.Core.Models;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DarkBox.Export;

internal sealed class PdfVaultLayout : IDisposable
{
    private const double Margin = 42;
    private const double FooterSpace = 30;

    private readonly PdfDocument _document;
    private readonly string _owner;
    private readonly XFont _titleFont = new("Segoe UI", 22, XFontStyleEx.Bold);
    private readonly XFont _subtitleFont = new("Segoe UI", 12, XFontStyleEx.Regular);
    private readonly XFont _serviceFont = new("Segoe UI", 13, XFontStyleEx.Bold);
    private readonly XFont _labelFont = new("Segoe UI", 8.5, XFontStyleEx.Bold);
    private readonly XFont _bodyFont = new("Segoe UI", 10, XFontStyleEx.Regular);
    private readonly XFont _footnoteFont = new("Segoe UI", 8, XFontStyleEx.Regular);
    private readonly XBrush _textBrush = new XSolidBrush(XColor.FromArgb(0x1F, 0x24, 0x2E));
    private readonly XBrush _mutedBrush = new XSolidBrush(XColor.FromArgb(0x66, 0x6D, 0x7A));
    private readonly XBrush _accentBrush = new XSolidBrush(XColor.FromArgb(0x2F, 0x6F, 0xD9));
    private readonly XBrush _dangerBrush = new XSolidBrush(XColor.FromArgb(0xB8, 0x3A, 0x3A));
    private readonly XPen _linePen = new(XColor.FromArgb(0xD8, 0xDC, 0xE3), 0.7);

    private PdfPage _page;
    private XGraphics _graphics;
    private double _y;
    private bool _disposed;
    private bool _finished;
    private bool _graphicsClosed;

    public PdfVaultLayout(PdfDocument document, string owner)
    {
        _document = document;
        _owner = owner;
        _page = CreatePage();
        _graphics = XGraphics.FromPdfPage(_page);
        _y = Margin;
    }

    private double ContentWidth => _page.Width.Point - (Margin * 2);

    private double ContentBottom => _page.Height.Point - Margin - FooterSpace;

    public void Write(IReadOnlyList<VaultItem> items)
    {
        DrawHeader();

        if (items.Count == 0)
        {
            DrawWrapped("La bóveda no contiene credenciales.", _bodyFont, _mutedBrush, Margin, ContentWidth, 15);
            return;
        }

        foreach (var item in items)
            DrawItem(item);
    }

    public void Finish()
    {
        if (_finished)
            return;

        _finished = true;
        CloseGraphics();

        for (var index = 0; index < _document.PageCount; index++)
        {
            var page = _document.Pages[index];
            using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
            var footer = $"{index + 1} / {_document.PageCount}";
            graphics.DrawString(
                footer,
                _footnoteFont,
                _mutedBrush,
                new XRect(Margin, page.Height.Point - 26, page.Width.Point - (Margin * 2), 14),
                XStringFormats.Center);
        }
    }

    private void DrawHeader()
    {
        _graphics.DrawString(
            "DarkBox",
            _titleFont,
            _textBrush,
            new XRect(Margin, _y, ContentWidth, 30),
            XStringFormats.TopLeft);
        _y += 34;

        _graphics.DrawString(
            "Credenciales exportadas",
            _subtitleFont,
            _accentBrush,
            new XRect(Margin, _y, ContentWidth, 18),
            XStringFormats.TopLeft);
        _y += 20;

        var ownerLine = $"Bóveda de {_owner}  ·  {DateTime.Now:g}";
        DrawWrapped(ownerLine, _footnoteFont, _mutedBrush, Margin, ContentWidth, 13);
        _y += 4;

        DrawWrapped(
            "Documento sin cifrar. Contiene contraseñas en texto plano; guárdalo en un lugar seguro.",
            _footnoteFont,
            _dangerBrush,
            Margin,
            ContentWidth,
            13);
        _y += 10;

        DrawDivider();
    }

    private void DrawItem(VaultItem item)
    {
        EnsureSpace(48);

        _graphics.DrawRectangle(_accentBrush, Margin, _y + 2, 3, 15);
        _graphics.DrawString(
            item.Service,
            _serviceFont,
            _textBrush,
            new XRect(Margin + 11, _y, ContentWidth - 11, 20),
            XStringFormats.TopLeft);
        _y += 25;

        DrawField("Usuario", item.Username);
        DrawField("Contraseña", item.Password);
        DrawField("URL", item.Url);
        DrawField("Notas", item.Notes);
        DrawField("Actualizado", item.UpdatedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));

        DrawDivider();
    }

    private void DrawField(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        EnsureSpace(28);

        const double labelWidth = 72;
        _graphics.DrawString(
            label.ToUpperInvariant(),
            _labelFont,
            _mutedBrush,
            new XRect(Margin, _y + 2, labelWidth, 13),
            XStringFormats.TopLeft);

        DrawWrapped(
            value.Trim(),
            _bodyFont,
            _textBrush,
            Margin + labelWidth,
            ContentWidth - labelWidth,
            14);

        _y += 3;
    }

    private void DrawWrapped(
        string text,
        XFont font,
        XBrush brush,
        double x,
        double width,
        double lineHeight)
    {
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                EnsureSpace(lineHeight);
                _y += lineHeight;
                continue;
            }

            var line = string.Empty;

            foreach (var word in words)
            {
                var candidate = line.Length == 0 ? word : line + " " + word;

                if (_graphics.MeasureString(candidate, font).Width <= width)
                {
                    line = candidate;
                    continue;
                }

                if (line.Length > 0)
                {
                    DrawLine(line, font, brush, x, width, lineHeight);
                    line = string.Empty;
                }

                if (_graphics.MeasureString(word, font).Width <= width)
                {
                    line = word;
                    continue;
                }

                var chunks = SplitLongWord(word, font, width).ToList();

                for (var index = 0; index < chunks.Count; index++)
                {
                    if (index < chunks.Count - 1)
                        DrawLine(chunks[index], font, brush, x, width, lineHeight);
                    else
                        line = chunks[index];
                }
            }

            if (line.Length > 0)
                DrawLine(line, font, brush, x, width, lineHeight);
        }
    }

    private IEnumerable<string> SplitLongWord(string word, XFont font, double width)
    {
        if (_graphics.MeasureString(word, font).Width <= width)
        {
            yield return word;
            yield break;
        }

        var chunk = string.Empty;

        foreach (var character in word)
        {
            var candidate = chunk + character;

            if (chunk.Length > 0 && _graphics.MeasureString(candidate, font).Width > width)
            {
                yield return chunk;
                chunk = character.ToString();
            }
            else
            {
                chunk = candidate;
            }
        }

        if (chunk.Length > 0)
            yield return chunk;
    }

    private void DrawLine(string text, XFont font, XBrush brush, double x, double width, double lineHeight)
    {
        EnsureSpace(lineHeight);
        _graphics.DrawString(
            text,
            font,
            brush,
            new XRect(x, _y, width, lineHeight),
            XStringFormats.TopLeft);
        _y += lineHeight;
    }

    private void DrawDivider()
    {
        EnsureSpace(18);
        _graphics.DrawLine(_linePen, Margin, _y, Margin + ContentWidth, _y);
        _y += 16;
    }

    private void EnsureSpace(double requiredHeight)
    {
        if (_y + requiredHeight <= ContentBottom)
            return;

        CloseGraphics();
        _page = CreatePage();
        _graphics = XGraphics.FromPdfPage(_page);
        _graphicsClosed = false;
        _y = Margin;

        _graphics.DrawString(
            "DarkBox  ·  Credenciales",
            _footnoteFont,
            _mutedBrush,
            new XRect(Margin, _y, ContentWidth, 14),
            XStringFormats.TopLeft);
        _y += 22;
    }

    private PdfPage CreatePage()
    {
        var page = _document.AddPage();
        page.Size = PageSize.A4;
        return page;
    }

    private void CloseGraphics()
    {
        if (_graphicsClosed)
            return;

        _graphics.Dispose();
        _graphicsClosed = true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CloseGraphics();
    }
}
