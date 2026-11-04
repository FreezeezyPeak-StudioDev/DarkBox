using CommunityToolkit.Mvvm.ComponentModel;

namespace DarkBox.App.ViewModels;

public sealed partial class SplashScreenViewModel : ObservableObject
{
    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private string progressText = "Inicializando...";

    [ObservableProperty]
    private string statusMessage = "Cargando DarkBox...";

    public async Task SimulateLoadingAsync()
    {
        var steps = new[]
        {
            ("Iniciando DarkBox...", 20d),
            ("Cargando configuración...", 40d),
            ("Inicializando base de datos...", 60d),
            ("Preparando interfaz...", 80d),
            ("Listo", 100d),
        };

        foreach (var (message, progress) in steps)
        {
            ProgressText = message;
            ProgressValue = progress;
            StatusMessage = message;
            await Task.Delay(250);
        }
    }
}
