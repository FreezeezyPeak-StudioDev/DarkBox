using System.Globalization;

namespace DarkBox.Core.Models;

public sealed record VaultItem(
    Guid Id,
    string Service,
    string Username,
    string Password,
    string? Url,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc)
{
    public string UpdatedLocalText => UpdatedUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
}
