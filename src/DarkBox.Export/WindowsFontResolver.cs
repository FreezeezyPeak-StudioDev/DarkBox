using PdfSharp.Fonts;

namespace DarkBox.Export;

internal sealed class WindowsFontResolver : IFontResolver
{
    private const string RegularFace = "SegoeUI#Regular";
    private const string BoldFace = "SegoeUI#Bold";
    private const string ItalicFace = "SegoeUI#Italic";
    private const string BoldItalicFace = "SegoeUI#BoldItalic";

    private static readonly Dictionary<string, string> FontFiles = new(StringComparer.Ordinal)
    {
        [RegularFace] = "segoeui.ttf",
        [BoldFace] = "segoeuib.ttf",
        [ItalicFace] = "segoeuii.ttf",
        [BoldItalicFace] = "segoeuiz.ttf",
    };

    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var face = (isBold, isItalic) switch
        {
            (true, true) => BoldItalicFace,
            (true, false) => BoldFace,
            (false, true) => ItalicFace,
            _ => RegularFace,
        };

        return new FontResolverInfo(face);
    }

    public byte[] GetFont(string faceName)
    {
        lock (_sync)
        {
            if (_cache.TryGetValue(faceName, out var cached))
                return cached;

            if (!FontFiles.TryGetValue(faceName, out var fileName))
                fileName = FontFiles[RegularFace];

            var fontsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
            var path = Path.Combine(fontsDirectory, fileName);

            if (!File.Exists(path))
            {
                path = Path.Combine(fontsDirectory, FontFiles[RegularFace]);
            }

            var bytes = File.ReadAllBytes(path);
            _cache[faceName] = bytes;
            return bytes;
        }
    }
}
