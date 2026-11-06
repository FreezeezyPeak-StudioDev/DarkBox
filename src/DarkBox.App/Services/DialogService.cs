using System.Windows;
using DarkBox.App.Views;
using Microsoft.Win32;

namespace DarkBox.App.Services;

public sealed class DialogService
{
    public bool Confirm(string message, string title)
    {
        return MessageBox.Show(
            message,
            title,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    public bool ConfirmPdfExport()
    {
        return MessageBox.Show(
            "El PDF se guardará sin cifrar y mostrará las contraseñas en texto plano.\n\n" +
            "Guárdalo en un lugar seguro. ¿Deseas continuar?",
            "Exportar PDF",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    public string? AskPdfPath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Guardar PDF",
            Filter = "Documento PDF (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            FileName = suggestedFileName,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? AskDarkBoxSavePath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Exportar bóveda",
            Filter = "Paquete DarkBox (*.darkbox)|*.darkbox",
            DefaultExt = ".darkbox",
            AddExtension = true,
            FileName = suggestedFileName,
            OverwritePrompt = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? AskDarkBoxOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar bóveda",
            Filter = "Paquete DarkBox (*.darkbox)|*.darkbox",
            CheckFileExists = true,
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? AskPassword(string title, string message, bool requireConfirmation = false)
    {
        var window = new PasswordPromptWindow(title, message, requireConfirmation)
        {
            Owner = Application.Current?.MainWindow,
        };

        return window.ShowDialog() == true ? window.Password : null;
    }

    public void ShowError(string message, string title)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
