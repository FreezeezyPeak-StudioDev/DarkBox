namespace DarkBox.Core.Models;

public static class VaultItemRules
{
    public const int MaxServiceLength = 256;
    public const int MaxUsernameLength = 512;
    public const int MaxPasswordLength = 4096;
    public const int MaxUrlLength = 2048;
    public const int MaxNotesLength = 8000;

    public static string? Validate(VaultItemPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Service))
            return "El servicio es obligatorio.";

        if (payload.Service.Trim().Length > MaxServiceLength)
            return $"El servicio no puede superar los {MaxServiceLength} caracteres.";

        if (payload.Username.Length > MaxUsernameLength)
            return $"El usuario no puede superar los {MaxUsernameLength} caracteres.";

        if (string.IsNullOrEmpty(payload.Password))
            return "La contraseña es obligatoria.";

        if (payload.Password.Length > MaxPasswordLength)
            return $"La contraseña no puede superar los {MaxPasswordLength} caracteres.";

        if (payload.Url is { Length: > MaxUrlLength })
            return $"La URL no puede superar los {MaxUrlLength} caracteres.";

        if (payload.Notes is { Length: > MaxNotesLength })
            return $"Las notas no pueden superar los {MaxNotesLength} caracteres.";

        return null;
    }
}
