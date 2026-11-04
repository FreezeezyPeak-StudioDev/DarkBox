using System.IO;
using System.Windows;
using System.Windows.Threading;
using DarkBox.App.Services;
using DarkBox.App.ViewModels;
using DarkBox.Data;

namespace DarkBox.App;

public partial class App : Application
{
    private Window? _splashWindow;
    private MainWindowViewModel? _mainViewModel;
    private ClipboardService? _clipboard;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var splashViewModel = new SplashScreenViewModel();
            ShowSplashScreen(splashViewModel);
            await splashViewModel.SimulateLoadingAsync();

            var database = new DarkBoxDatabase(DarkBoxDatabase.DefaultDatabasePath);
            database.Initialize();

            var users = new UserRepository(database);
            var vault = new VaultRepository(database);
            _clipboard = new ClipboardService();
            var dialogs = new DialogService();

            _mainViewModel = new MainWindowViewModel(users, vault, _clipboard, dialogs);

            if (!await _mainViewModel.InitializeAsync())
            {
                CloseSplashScreen();
                Shutdown(1);
                return;
            }

            var mainWindow = new MainWindow
            {
                DataContext = _mainViewModel,
            };

            MainWindow = mainWindow;
            mainWindow.Show();
            CloseSplashScreen();
        }
        catch (Exception exception)
        {
            LogCrash(exception);
            CloseSplashScreen();
            MessageBox.Show(
                "No se pudo iniciar DarkBox.\n\n" + exception.Message,
                "DarkBox",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    private void ShowSplashScreen(SplashScreenViewModel viewModel)
    {
        _splashWindow = new Window
        {
            Title = "DarkBox",
            WindowStyle = WindowStyle.None,
            Width = 550,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            ShowInTaskbar = false,
            ResizeMode = ResizeMode.NoResize,
        };

        _splashWindow.Content = new Views.SplashScreenView
        {
            DataContext = viewModel,
        };

        _splashWindow.Show();
    }

    private void CloseSplashScreen()
    {
        if (_splashWindow is null)
            return;

        _splashWindow.Close();
        _splashWindow = null;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        MessageBox.Show(
            "DarkBox encontró un error inesperado.\n\n" + e.Exception.Message,
            "DarkBox",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
        Shutdown(1);
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
            LogCrash(exception);
    }

    private static void LogCrash(Exception exception)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DarkBox");

            Directory.CreateDirectory(directory);
            File.AppendAllText(
                Path.Combine(directory, "crash.log"),
                $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        _clipboard?.Dispose();
        base.OnExit(e);
    }
}
