using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DarkBox.Core.Security;
using DarkBox.Data;

namespace DarkBox.App.ViewModels;

public sealed partial class UnlockViewModel : ObservableObject
{
    private readonly UserRepository _users;
    private readonly Func<VaultSession, Task> _onUnlocked;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModeTitle))]
    [NotifyPropertyChangedFor(nameof(SubmitButtonText))]
    [NotifyPropertyChangedFor(nameof(ToggleModeText))]
    private bool isCreateMode;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmPassword = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    public UnlockViewModel(
        UserRepository users,
        bool hasExistingUser,
        Func<VaultSession, Task> onUnlocked)
    {
        _users = users;
        _onUnlocked = onUnlocked;
        IsCreateMode = !hasExistingUser;
    }

    public bool IsNotBusy => !IsBusy;

    public string ModeTitle => IsCreateMode ? "Crea tu bóveda" : "Desbloquea tu bóveda";

    public string SubmitButtonText => IsCreateMode ? "Crear bóveda" : "Desbloquear";

    public string ToggleModeText => IsCreateMode ? "Ya tengo una bóveda" : "Crear otra bóveda";

    [RelayCommand]
    private void ToggleMode()
    {
        IsCreateMode = !IsCreateMode;
        ErrorMessage = string.Empty;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Escribe tu nombre de usuario.";
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Escribe la contraseña maestra.";
            return;
        }

        IsBusy = true;

        try
        {
            if (IsCreateMode)
            {
                if (Password != ConfirmPassword)
                {
                    ErrorMessage = "Las contraseñas no coinciden.";
                    return;
                }

                var result = await _users.CreateAsync(Username, Password);

                if (!result.IsSuccess)
                {
                    ErrorMessage = result.Error ?? "No se pudo crear la bóveda.";
                    return;
                }

                Password = string.Empty;
                ConfirmPassword = string.Empty;
                await _onUnlocked(result.Session!);
            }
            else
            {
                var session = await _users.UnlockAsync(Username, Password);

                if (session is null)
                {
                    ErrorMessage = "Usuario o contraseña incorrectos.";
                    return;
                }

                Password = string.Empty;
                await _onUnlocked(session);
            }
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudo completar la operación: " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
