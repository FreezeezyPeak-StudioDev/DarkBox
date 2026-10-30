using CommunityToolkit.Mvvm.ComponentModel;
using DarkBox.App.Services;
using DarkBox.Core.Security;
using DarkBox.Data;

namespace DarkBox.App.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly UserRepository _users;
    private readonly VaultRepository _vault;
    private readonly ClipboardService _clipboard;
    private readonly DialogService _dialogs;
    private bool _initialized;
    private bool _disposed;

    [ObservableProperty]
    private ObservableObject? currentViewModel;

    public MainWindowViewModel(
        UserRepository users,
        VaultRepository vault,
        ClipboardService clipboard,
        DialogService dialogs)
    {
        _users = users;
        _vault = vault;
        _clipboard = clipboard;
        _dialogs = dialogs;
    }

    public async Task<bool> InitializeAsync()
    {
        if (_initialized || _disposed)
            return CurrentViewModel is not null;

        _initialized = true;

        try
        {
            var hasUsers = await _users.HasAnyUserAsync();
            CurrentViewModel = CreateUnlockViewModel(hasUsers);
            return true;
        }
        catch (Exception exception)
        {
            _dialogs.ShowError(
                "No se pudo abrir la base de datos de DarkBox.\n\n" + exception.Message,
                "DarkBox");

            return false;
        }
    }

    private UnlockViewModel CreateUnlockViewModel(bool hasUsers)
    {
        return new UnlockViewModel(_users, hasUsers, OnUnlockedAsync);
    }

    private async Task OnUnlockedAsync(VaultSession session)
    {
        var vaultViewModel = new VaultViewModel(
            session,
            _vault,
            _clipboard,
            _dialogs,
            OnLocked);

        CurrentViewModel = vaultViewModel;
        await vaultViewModel.LoadAsync();
    }

    private void OnLocked()
    {
        if (_disposed)
            return;

        CurrentViewModel = CreateUnlockViewModel(hasUsers: true);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (CurrentViewModel is IDisposable disposable)
            disposable.Dispose();

        CurrentViewModel = null;
    }
}
