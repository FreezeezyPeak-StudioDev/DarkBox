using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DarkBox.App.Services;
using DarkBox.Core.Models;
using DarkBox.Core.Security;
using DarkBox.Data;
using DarkBox.Export;

namespace DarkBox.App.ViewModels;

public sealed partial class VaultViewModel : ObservableObject, IDisposable
{
    private readonly VaultSession _session;
    private readonly UserRepository _users;
    private readonly VaultRepository _vault;
    private readonly ClipboardService _clipboard;
    private readonly DialogService _dialogs;
    private readonly SettingsService _settings;
    private readonly Action _onLocked;
    private DispatcherTimer? _autoLockTimer;
    private int _autoLockMinutes;
    private bool _disposed;

    [ObservableProperty]
    private VaultItem? selectedItem;

    [ObservableProperty]
    private bool isEditorOpen;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorTitle))]
    private bool isEditing;

    [ObservableProperty]
    private string editorService = string.Empty;

    [ObservableProperty]
    private string editorUsername = string.Empty;

    [ObservableProperty]
    private string editorPassword = string.Empty;

    [ObservableProperty]
    private string editorUrl = string.Empty;

    [ObservableProperty]
    private string editorNotes = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordVisibilityButtonText))]
    private bool arePasswordsVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EditorPasswordToggleText))]
    private bool isEditorPasswordVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool isBusy;

    [ObservableProperty]
    private bool hasItems;

    [ObservableProperty]
    private bool isSettingsOpen;

    [ObservableProperty]
    private string settingClipboardSecondsText = "30";

    [ObservableProperty]
    private string settingAutoLockMinutesText = "0";

    [ObservableProperty]
    private bool settingConfirmBeforeDelete = true;

    [ObservableProperty]
    private bool isVaultManagerOpen;

    public VaultViewModel(
        VaultSession session,
        UserRepository users,
        VaultRepository vault,
        ClipboardService clipboard,
        DialogService dialogs,
        SettingsService settings,
        Action onLocked)
    {
        _session = session;
        _users = users;
        _vault = vault;
        _clipboard = clipboard;
        _dialogs = dialogs;
        _settings = settings;
        _onLocked = onLocked;
        _autoLockMinutes = settings.Settings.AutoLockMinutes;
        LoadSettingsToFields();
        ResetAutoLockTimer();
    }

    public ObservableCollection<VaultItem> Items { get; } = new();

    public ObservableCollection<VaultSummary> Vaults { get; } = new();

    public string Username => _session.Username;

    public bool IsNotBusy => !IsBusy;

    public string EditorTitle => IsEditing ? "Editar credencial" : "Nueva credencial";

    public string PasswordVisibilityButtonText =>
        ArePasswordsVisible ? "Ocultar" : "Mostrar";

    public string EditorPasswordToggleText =>
        IsEditorPasswordVisible ? "Ocultar" : "Mostrar";

    public async Task LoadAsync()
    {
        if (_disposed)
            return;

        ResetAutoLockTimer();
        ErrorMessage = string.Empty;

        try
        {
            var items = await _vault.LoadAllAsync(_session);

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            HasItems = items.Count > 0;
            SelectedItem = null;
            StatusMessage = items.Count == 0
                ? "La bóveda está vacía."
                : $"{items.Count} credencial(es) en la bóveda.";
        }
        catch (CryptographicException)
        {
            ErrorMessage = "No se pudo descifrar la bóveda. Los datos pueden estar dañados.";
        }
        catch (Exception exception)
        {
            ErrorMessage = "Error al cargar la bóveda: " + exception.Message;
        }
    }

    [RelayCommand]
    private void NewItem()
    {
        IsEditing = false;
        IsEditorOpen = true;
        IsEditorPasswordVisible = false;
        ErrorMessage = string.Empty;
        ClearEditor();
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedItem is null)
        {
            StatusMessage = "Selecciona una credencial para editar.";
            return;
        }

        IsEditing = true;
        IsEditorOpen = true;
        IsEditorPasswordVisible = false;
        ErrorMessage = string.Empty;

        EditorService = SelectedItem.Service;
        EditorUsername = SelectedItem.Username;
        EditorPassword = SelectedItem.Password;
        EditorUrl = SelectedItem.Url ?? string.Empty;
        EditorNotes = SelectedItem.Notes ?? string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditorOpen = false;
        IsEditorPasswordVisible = false;
        ErrorMessage = string.Empty;
        ClearEditor();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
            return;

        ErrorMessage = string.Empty;

        var payload = new VaultItemPayload(
            EditorService.Trim(),
            EditorUsername.Trim(),
            EditorPassword,
            string.IsNullOrWhiteSpace(EditorUrl) ? null : EditorUrl.Trim(),
            string.IsNullOrWhiteSpace(EditorNotes) ? null : EditorNotes.Trim());

        var validationError = VaultItemRules.Validate(payload);
        if (validationError is not null)
        {
            ErrorMessage = validationError;
            return;
        }

        IsBusy = true;

        try
        {
            if (IsEditing)
            {
                if (SelectedItem is null)
                {
                    ErrorMessage = "Selecciona una credencial para editar.";
                    return;
                }

                await _vault.UpdateAsync(_session, SelectedItem.Id, payload);
            }
            else
            {
                await _vault.AddAsync(_session, payload);
            }

            IsEditorOpen = false;
            IsEditorPasswordVisible = false;
            ClearEditor();
            await LoadAsync();
            StatusMessage = "Credencial guardada.";
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudo guardar la credencial: " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        if (IsBusy)
            return;

        if (SelectedItem is null)
        {
            StatusMessage = "Selecciona una credencial para eliminar.";
            return;
        }

        if (_settings.Settings.ConfirmBeforeDelete &&
            !_dialogs.Confirm(
                $"¿Eliminar la credencial de \"{SelectedItem.Service}\"?",
                "Eliminar credencial"))
        {
            return;
        }

        IsBusy = true;

        try
        {
            await _vault.DeleteAsync(_session, SelectedItem.Id);
            await LoadAsync();
            StatusMessage = "Credencial eliminada.";
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudo eliminar la credencial: " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CopyPassword()
    {
        if (SelectedItem is null)
        {
            StatusMessage = "Selecciona una credencial para copiar su contraseña.";
            return;
        }

        StatusMessage = _clipboard.Copy(SelectedItem.Password)
            ? $"Contraseña copiada. Se borrará del portapapeles en {(int)_clipboard.ClearAfter.TotalSeconds} segundos."
            : "No se pudo copiar la contraseña.";
        ResetAutoLockTimer();
    }

    [RelayCommand]
    private void CopyUsername()
    {
        if (SelectedItem is null)
        {
            StatusMessage = "Selecciona una credencial para copiar su usuario.";
            return;
        }

        StatusMessage = _clipboard.Copy(SelectedItem.Username)
            ? $"Usuario copiado. Se borrará del portapapeles en {(int)_clipboard.ClearAfter.TotalSeconds} segundos."
            : "No se pudo copiar el usuario.";
        ResetAutoLockTimer();
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        ArePasswordsVisible = !ArePasswordsVisible;
    }

    [RelayCommand]
    private void ToggleEditorPasswordVisibility()
    {
        IsEditorPasswordVisible = !IsEditorPasswordVisible;
    }

    [RelayCommand]
    private void ExportAllToPdf()
    {
        if (Items.Count == 0)
        {
            StatusMessage = "No hay credenciales para exportar.";
            return;
        }

        ExportToPdf(Items.ToList(), "DarkBox-credenciales");
    }

    [RelayCommand]
    private void ExportSelectedToPdf()
    {
        if (SelectedItem is null)
        {
            StatusMessage = "Selecciona una credencial para exportar.";
            return;
        }

        var baseName = "DarkBox-" + SanitizeFileName(SelectedItem.Service);
        ExportToPdf(new[] { SelectedItem }, baseName);
    }

    private void ExportToPdf(IReadOnlyList<VaultItem> items, string baseName)
    {
        if (IsBusy)
            return;

        if (!_dialogs.ConfirmPdfExport())
            return;

        var suggestedName = $"{baseName}-{DateTime.Now:yyyyMMdd-HHmm}.pdf";
        var path = _dialogs.AskPdfPath(suggestedName);

        if (string.IsNullOrWhiteSpace(path))
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            PdfVaultExporter.Export(path, items, Username);
            StatusMessage = "PDF guardado en " + path;
            ResetAutoLockTimer();
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudo generar el PDF: " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        const int maxLength = 80;
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(character =>
            invalidCharacters.Contains(character) ? '-' : character).ToArray());

        sanitized = sanitized.Trim().TrimEnd('.', ' ');
        if (sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength].TrimEnd('.', ' ');

        return string.IsNullOrWhiteSpace(sanitized) ? "credencial" : sanitized;
    }

    [RelayCommand]
    private async Task ExportVaultToDarkBoxAsync()
    {
        if (IsBusy)
            return;

        if (Items.Count == 0)
        {
            StatusMessage = "No hay credenciales para exportar.";
            return;
        }

        var suggestedName = $"DarkBox-{SanitizeFileName(Username)}-{DateTime.Now:yyyyMMdd-HHmm}.darkbox";
        var path = _dialogs.AskDarkBoxSavePath(suggestedName);
        if (string.IsNullOrWhiteSpace(path))
            return;

        var password = _dialogs.AskPassword(
            "Exportar bóveda",
            "Escribe una contraseña para cifrar el archivo .darkbox.",
            requireConfirmation: true);
        if (password is null)
            return;

        var passwordError = AccountPolicy.ValidatePassword(password);
        if (passwordError is not null)
        {
            ErrorMessage = passwordError;
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var payloads = Items
                .Select(item => new VaultItemPayload(item.Service, item.Username, item.Password, item.Url, item.Notes))
                .ToList();

            await VaultPackage.WriteAsync(path, password, payloads);
            StatusMessage = "Bóveda exportada en " + path;
            ResetAutoLockTimer();
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudo exportar la bóveda: " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportVaultFromDarkBoxAsync()
    {
        if (IsBusy)
            return;

        var path = _dialogs.AskDarkBoxOpenPath();
        if (string.IsNullOrWhiteSpace(path))
            return;

        var password = _dialogs.AskPassword(
            "Importar bóveda",
            "Escribe la contraseña del archivo .darkbox.");
        if (password is null)
            return;

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var payloads = await VaultPackage.ReadAsync(path, password);
            var imported = 0;

            foreach (var payload in payloads)
            {
                await _vault.AddAsync(_session, payload);
                imported++;
            }

            await LoadAsync();
            StatusMessage = imported == 0
                ? "El archivo no contiene credenciales."
                : $"{imported} credencial(es) importada(s).";
            ResetAutoLockTimer();
        }
        catch (CryptographicException)
        {
            ErrorMessage = "No se pudo descifrar el archivo. Revisa la contraseña o el archivo.";
        }
        catch (FormatException exception)
        {
            ErrorMessage = "El archivo no es válido: " + exception.Message;
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudo importar la bóveda: " + exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Lock()
    {
        if (_disposed)
            return;

        ArePasswordsVisible = false;
        IsEditorPasswordVisible = false;
        _clipboard.ClearIfOwned();
        Dispose();
        _onLocked();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        LoadSettingsToFields();
        IsSettingsOpen = true;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private void SaveSettings()
    {
        ErrorMessage = string.Empty;

        if (!int.TryParse(SettingClipboardSecondsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var clipboardSeconds) ||
            clipboardSeconds < 5 || clipboardSeconds > 300)
        {
            ErrorMessage = "Los segundos del portapapeles deben estar entre 5 y 300.";
            return;
        }

        if (!int.TryParse(SettingAutoLockMinutesText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var autoLockMinutes) ||
            autoLockMinutes < 0 || autoLockMinutes > 240)
        {
            ErrorMessage = "El auto-bloqueo debe estar entre 0 y 240 minutos.";
            return;
        }

        _settings.Save(new AppSettings
        {
            ClipboardSeconds = clipboardSeconds,
            AutoLockMinutes = autoLockMinutes,
            ConfirmBeforeDelete = SettingConfirmBeforeDelete,
        });

        _clipboard.ClearAfter = TimeSpan.FromSeconds(clipboardSeconds);
        _autoLockMinutes = autoLockMinutes;
        ResetAutoLockTimer();
        IsSettingsOpen = false;
        StatusMessage = "Ajustes guardados.";
    }

    private void LoadSettingsToFields()
    {
        var settings = _settings.Settings;
        SettingClipboardSecondsText = settings.ClipboardSeconds.ToString(CultureInfo.InvariantCulture);
        SettingAutoLockMinutesText = settings.AutoLockMinutes.ToString(CultureInfo.InvariantCulture);
        SettingConfirmBeforeDelete = settings.ConfirmBeforeDelete;
    }

    [RelayCommand]
    private async Task OpenVaultManagerAsync()
    {
        IsVaultManagerOpen = true;
        await LoadVaultsAsync();
    }

    [RelayCommand]
    private void CloseVaultManager()
    {
        IsVaultManagerOpen = false;
    }

    private async Task LoadVaultsAsync()
    {
        try
        {
            var summaries = await _users.GetVaultsAsync();
            var adminId = summaries.FirstOrDefault(vault => vault.IsAdmin)?.Id;
            var isAdminSession = adminId == _session.UserId;

            Vaults.Clear();
            foreach (var vault in summaries)
            {
                vault.CanRename = vault.Id == _session.UserId || isAdminSession;
                vault.CanDelete = !vault.IsAdmin && (vault.Id == _session.UserId || isAdminSession);
                Vaults.Add(vault);
            }

            StatusMessage = $"{Vaults.Count} bóveda(s) encontrada(s).";
        }
        catch (Exception exception)
        {
            ErrorMessage = "No se pudieron cargar las bóvedas: " + exception.Message;
        }
    }

    [RelayCommand]
    private void StartRenameVault(VaultSummary? vault)
    {
        if (vault is null)
            return;

        foreach (var item in Vaults)
            item.IsEditing = false;

        vault.IsEditing = true;
        vault.EditUsername = vault.Username;
    }

    [RelayCommand]
    private async Task SaveRenameVaultAsync(VaultSummary? vault)
    {
        if (vault is null)
            return;

        var error = await _users.RenameAsync(_session, vault.Id, vault.EditUsername);
        if (error is not null)
        {
            ErrorMessage = error;
            return;
        }

        vault.Username = vault.EditUsername.Trim();
        vault.IsEditing = false;
        StatusMessage = "Bóveda renombrada.";
        await LoadVaultsAsync();
    }

    [RelayCommand]
    private void CancelRenameVault(VaultSummary? vault)
    {
        if (vault is null)
            return;

        vault.IsEditing = false;
        vault.EditUsername = vault.Username;
    }

    [RelayCommand]
    private async Task DeleteVaultAsync(VaultSummary? vault)
    {
        if (vault is null)
            return;

        if (vault.Id == _session.UserId)
        {
            StatusMessage = "No puedes eliminar la bóveda que está abierta.";
            return;
        }

        if (_settings.Settings.ConfirmBeforeDelete &&
            !_dialogs.Confirm(
                $"¿Eliminar la bóveda \"{vault.Username}\" y todas sus credenciales?",
                "Eliminar bóveda"))
        {
            return;
        }

        var error = await _users.DeleteAsync(_session, vault.Id);
        if (error is not null)
        {
            ErrorMessage = error;
            return;
        }

        StatusMessage = "Bóveda eliminada.";
        await LoadVaultsAsync();
    }

    private void ResetAutoLockTimer()
    {
        if (_disposed)
            return;

        if (_autoLockMinutes <= 0)
        {
            _autoLockTimer?.Stop();
            return;
        }

        _autoLockTimer ??= CreateAutoLockTimer();
        _autoLockTimer.Interval = TimeSpan.FromMinutes(_autoLockMinutes);
        _autoLockTimer.Stop();
        _autoLockTimer.Start();
    }

    private DispatcherTimer CreateAutoLockTimer()
    {
        var timer = new DispatcherTimer();
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Lock();
        };
        return timer;
    }

    private void StopAutoLockTimer()
    {
        _autoLockTimer?.Stop();
    }

    private void ClearEditor()
    {
        EditorService = string.Empty;
        EditorUsername = string.Empty;
        EditorPassword = string.Empty;
        EditorUrl = string.Empty;
        EditorNotes = string.Empty;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        ArePasswordsVisible = false;
        IsEditorPasswordVisible = false;
        SelectedItem = null;
        ClearEditor();
        StopAutoLockTimer();
        Items.Clear();
        Vaults.Clear();
        IsSettingsOpen = false;
        IsVaultManagerOpen = false;
        _session.Dispose();
    }
}
