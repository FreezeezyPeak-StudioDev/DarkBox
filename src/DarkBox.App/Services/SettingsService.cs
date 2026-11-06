using System.IO;
using System.Text.Json;

namespace DarkBox.App.Services;

public sealed class AppSettings
{
    public int ClipboardSeconds { get; set; } = 30;
    public int AutoLockMinutes { get; set; } = 0;
    public bool ConfirmBeforeDelete { get; set; } = true;
}

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    public SettingsService()
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DarkBox",
            "settings.json");

        Settings = Load();
    }

    public AppSettings Settings { get; private set; }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        settings.ClipboardSeconds = Math.Clamp(settings.ClipboardSeconds, 5, 300);
        settings.AutoLockMinutes = Math.Clamp(settings.AutoLockMinutes, 0, 240);

        Settings = settings;
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_path, JsonSerializer.Serialize(Settings, JsonOptions));
    }

    private AppSettings Load()
    {
        try
        {
            if (!File.Exists(_path))
                return new AppSettings();

            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_path)) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }
}
