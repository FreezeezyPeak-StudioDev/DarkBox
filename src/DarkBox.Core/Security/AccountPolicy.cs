namespace DarkBox.Core.Security;

public static class AccountPolicy
{
    public const int MinimumPasswordLength = 12;
    public const int MaximumPasswordLength = 1024;
    public const int MinimumUsernameLength = 3;
    public const int MaximumUsernameLength = 32;

    public static string? ValidateUsername(string? username)
    {
        var value = username?.Trim() ?? string.Empty;

        if (value.Length < MinimumUsernameLength)
            return $"El usuario debe tener al menos {MinimumUsernameLength} caracteres.";

        if (value.Length > MaximumUsernameLength)
            return $"El usuario no puede superar los {MaximumUsernameLength} caracteres.";

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character) && character != '.' && character != '_' && character != '-')
                return "El usuario solo puede contener letras, números, punto, guion o guion bajo.";
        }

        return null;
    }

    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return "La contraseña maestra es obligatoria.";

        if (password.Length < MinimumPasswordLength)
            return $"La contraseña maestra debe tener al menos {MinimumPasswordLength} caracteres.";

        if (password.Length > MaximumPasswordLength)
            return $"La contraseña maestra no puede superar los {MaximumPasswordLength} caracteres.";

        return null;
    }
}
