using System.Globalization;
using System.Security.Cryptography;
using DarkBox.Core.Security;
using Microsoft.Data.Sqlite;

namespace DarkBox.Data;

public sealed class UserRepository
{
    private readonly DarkBoxDatabase _database;
    private readonly ProtectionParameters _parameters;

    public UserRepository(DarkBoxDatabase database, ProtectionParameters? parameters = null)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _parameters = parameters ?? ProtectionParameters.Default;
        _parameters.Validate();
    }

    public async Task<bool> HasAnyUserAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM users;";

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture) > 0;
    }

    public async Task<UserCreationResult> CreateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        username = (username ?? string.Empty).Trim();

        var usernameError = AccountPolicy.ValidateUsername(username);
        if (usernameError is not null)
            return UserCreationResult.Failed(usernameError);

        var passwordError = AccountPolicy.ValidatePassword(password);
        if (passwordError is not null)
            return UserCreationResult.Failed(passwordError);

        var salt = RandomNumberGenerator.GetBytes(_parameters.SaltSize);
        var masterKey = MasterKeyDerivation.DeriveKey(password, salt, _parameters);
        var vaultId = Guid.NewGuid();
        var vaultKey = VaultKeyProtector.CreateVaultKey();
        var wrappedVaultKey = VaultKeyProtector.Wrap(masterKey, vaultKey, vaultId);
        var now = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        try
        {
            await using var connection = _database.OpenConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO users (vault_id, username, salt, kdf_parameters, wrapped_vault_key, created_utc, updated_utc)
                VALUES (@vaultId, @username, @salt, @kdfParameters, @wrappedVaultKey, @createdUtc, @updatedUtc);
                SELECT last_insert_rowid();";

            command.Parameters.AddWithValue("@vaultId", vaultId.ToString("D"));
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@salt", salt);
            command.Parameters.AddWithValue("@kdfParameters", _parameters.ToStorageString());
            command.Parameters.AddWithValue("@wrappedVaultKey", wrappedVaultKey);
            command.Parameters.AddWithValue("@createdUtc", now);
            command.Parameters.AddWithValue("@updatedUtc", now);

            var id = Convert.ToInt64(
                await command.ExecuteScalarAsync(cancellationToken),
                CultureInfo.InvariantCulture);

            return UserCreationResult.Success(new VaultSession(id, username, vaultId, vaultKey));
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            CryptographicOperations.ZeroMemory(vaultKey);
            return UserCreationResult.Failed("El nombre de usuario ya existe.");
        }
        catch
        {
            CryptographicOperations.ZeroMemory(vaultKey);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(masterKey);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    public async Task<VaultSession?> UnlockAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        username = (username ?? string.Empty).Trim();

        if (username.Length == 0 || string.IsNullOrEmpty(password))
            return null;

        var record = await FindByUsernameAsync(username, cancellationToken);

        if (record is null)
        {
            var dummySalt = RandomNumberGenerator.GetBytes(_parameters.SaltSize);
            var dummyKey = MasterKeyDerivation.DeriveKey(password, dummySalt, _parameters);

            CryptographicOperations.ZeroMemory(dummyKey);
            CryptographicOperations.ZeroMemory(dummySalt);
            return null;
        }

        ProtectionParameters parameters;
        try
        {
            parameters = ProtectionParameters.FromStorageString(record.KdfParameters);
        }
        catch (Exception)
        {
            return null;
        }

        var masterKey = MasterKeyDerivation.DeriveKey(password, record.Salt, parameters);

        try
        {
            if (!VaultKeyProtector.TryUnwrap(
                    masterKey,
                    record.WrappedVaultKey,
                    record.VaultId,
                    out var vaultKey))
            {
                return null;
            }

            return new VaultSession(record.Id, record.Username, record.VaultId, vaultKey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(masterKey);
        }
    }

    private async Task<UserRecord?> FindByUsernameAsync(
        string username,
        CancellationToken cancellationToken)
    {
        await using var connection = _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, vault_id, username, salt, kdf_parameters, wrapped_vault_key
            FROM users
            WHERE username = @username
            LIMIT 1;";
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new UserRecord(
            reader.GetInt64(0),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.GetFieldValue<byte[]>(3),
            reader.GetString(4),
            reader.GetFieldValue<byte[]>(5));
    }

    private sealed record UserRecord(
        long Id,
        Guid VaultId,
        string Username,
        byte[] Salt,
        string KdfParameters,
        byte[] WrappedVaultKey);
}
