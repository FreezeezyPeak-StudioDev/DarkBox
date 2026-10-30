namespace DarkBox.Core.Models;

public sealed record VaultItemPayload(
    string Service,
    string Username,
    string Password,
    string? Url = null,
    string? Notes = null);
