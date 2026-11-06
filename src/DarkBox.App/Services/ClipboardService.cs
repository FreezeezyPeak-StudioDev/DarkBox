using System.Windows;
using System.Windows.Threading;

namespace DarkBox.App.Services;

public sealed class ClipboardService : IDisposable
{
    private readonly DispatcherTimer _timer;
    private string? _copiedText;
    private bool _disposed;

    public ClipboardService()
    {
        ClearAfter = TimeSpan.FromSeconds(30);
        _timer = new DispatcherTimer();
        _timer.Tick += (_, _) => ClearIfOwned();
    }

    public TimeSpan ClearAfter { get; set; }

    public bool Copy(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        try
        {
            Clipboard.SetText(text);
            _copiedText = text;
            _timer.Stop();
            _timer.Interval = ClearAfter;
            _timer.Start();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void ClearIfOwned()
    {
        _timer.Stop();

        try
        {
            if (_copiedText is not null &&
                Clipboard.ContainsText() &&
                Clipboard.GetText() == _copiedText)
            {
                Clipboard.Clear();
            }
        }
        catch (Exception)
        {
            // El portapapeles puede estar bloqueado por otra aplicación.
        }
        finally
        {
            _copiedText = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ClearIfOwned();
        _disposed = true;
    }
}
