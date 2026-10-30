using DarkBox.Core.Security;

namespace DarkBox.Data;

public sealed record UserCreationResult(VaultSession? Session, string? Error)
{
    public bool IsSuccess => Session is not null;

    public static UserCreationResult Success(VaultSession session) => new(session, null);

    public static UserCreationResult Failed(string error) => new(null, error);
}
