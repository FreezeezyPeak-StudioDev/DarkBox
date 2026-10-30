using System.Text.Json;

namespace DarkBox.Core.Security;

public sealed record ProtectionParameters
{
    public const int CurrentVersion = 1;
    public const int DefaultIterations = 600_000;
    public const int MinimumIterations = 10_000;
    public const int MaximumIterations = 4_000_000;
    public const int DefaultSaltSize = 16;
    public const int DerivedKeySize = 32;

    public int Version { get; init; } = CurrentVersion;
    public int Iterations { get; init; } = DefaultIterations;
    public int SaltSize { get; init; } = DefaultSaltSize;
    public int KeySize { get; init; } = DerivedKeySize;

    public static ProtectionParameters Default => new();

    public string ToStorageString() => JsonSerializer.Serialize(this);

    public static ProtectionParameters FromStorageString(string value)
    {
        var parameters = JsonSerializer.Deserialize<ProtectionParameters>(value)
            ?? throw new FormatException("No se pudieron leer los parámetros de protección.");

        parameters.Validate();
        return parameters;
    }

    public void Validate()
    {
        if (Version != CurrentVersion)
            throw new NotSupportedException($"La versión del KDF no es compatible: {Version}.");

        if (Iterations < MinimumIterations || Iterations > MaximumIterations)
            throw new InvalidOperationException("El número de iteraciones del KDF está fuera del rango permitido.");

        if (SaltSize != DefaultSaltSize)
            throw new InvalidOperationException("El tamaño de la sal no es válido.");

        if (KeySize != DerivedKeySize)
            throw new InvalidOperationException("El tamaño de la clave derivada no es válido.");
    }
}
