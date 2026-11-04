using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
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
    private readonly VaultRepository _vault;
    private readonly ClipboardService _clipboard;
    private readonly DialogService _dialogs;
    private readonly Action _onLocked;
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

    public VaultViewModel(
        VaultSession session,
        VaultRepository vault,
        ClipboardService clipboard,
        DialogService dialogs,
        Action onLocked)
    {
        _session = session;
        _vault = vault;
        _clipboard = clipboard;
        _dialogs = dialogs;
        _onLocked = onLocked;
    }

    public ObservableCollection<VaultItem> Items { get; } = new();

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

        ErrorMessage = string.Empty;

        try
        {
            var items = await _vault.LoadAllAsync(_session);

            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

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

        var confirmed = _dialogs.Confirm(
            $"¿Eliminar la credencial de \"{SelectedItem.Service}\"?",
            "Eliminar credencial");

        if (!confirmed)
            return;

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
            ? "Contraseña copiada. Se borrará del portapapeles en 30 segundos."
            : "No se pudo copiar la contraseña.";
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
            ? "Usuario copiado. Se borrará del portapapeles en 30 segundos."
            : "No se pudo copiar el usuario.";
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
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(character =>
            invalidCharacters.Contains(character) ? '-' : character).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "credencial" : sanitized;
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
        Items.Clear();
        _session.Dispose();
    }
}
