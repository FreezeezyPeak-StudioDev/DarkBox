using System.Globalization;
using DarkBox.Core.Models;
using DarkBox.Core.Security;
using Microsoft.Data.Sqlite;

namespace DarkBox.Data;

public sealed class VaultRepository
{
    private readonly DarkBoxDatabase _database;

    public VaultRepository(DarkBoxDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<IReadOnlyList<VaultItem>> LoadAllAsync(
        VaultSession session,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var connection = _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT id, cipher_blob, created_utc, updated_utc
            FROM vault_items
            WHERE user_id = @userId
            ORDER BY updated_utc DESC, id;";
        command.Parameters.AddWithValue("@userId", session.UserId);

        var items = new List<VaultItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            var itemId = Guid.Parse(reader.GetString(0));
            var blob = reader.GetFieldValue<byte[]>(1);
            var createdAt = ParseTimestamp(reader.GetString(2));
            var updatedAt = ParseTimestamp(reader.GetString(3));

            var payload = VaultItemCipher.Decrypt(session.VaultKey, session.UserId, itemId, blob);
            items.Add(new VaultItem(
                itemId,
                payload.Service,
                payload.Username,
                payload.Password,
                payload.Url,
                payload.Notes,
                createdAt,
                updatedAt));
        }

        return items;
    }

    public async Task<VaultItem> AddAsync(
        VaultSession session,
        VaultItemPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(payload);

        var validationError = VaultItemRules.Validate(payload);
        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(payload));

        var itemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var timestamp = now.ToString("O", CultureInfo.InvariantCulture);
        var blob = VaultItemCipher.Encrypt(session.VaultKey, session.UserId, itemId, payload);

        await using var connection = _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO vault_items (id, user_id, cipher_blob, created_utc, updated_utc)
            VALUES (@id, @userId, @cipherBlob, @createdUtc, @updatedUtc);";
        command.Parameters.AddWithValue("@id", itemId.ToString("D"));
        command.Parameters.AddWithValue("@userId", session.UserId);
        command.Parameters.AddWithValue("@cipherBlob", blob);
        command.Parameters.AddWithValue("@createdUtc", timestamp);
        command.Parameters.AddWithValue("@updatedUtc", timestamp);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return new VaultItem(
            itemId,
            payload.Service,
            payload.Username,
            payload.Password,
            payload.Url,
            payload.Notes,
            now,
            now);
    }

    public async Task UpdateAsync(
        VaultSession session,
        Guid itemId,
        VaultItemPayload payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(payload);

        var validationError = VaultItemRules.Validate(payload);
        if (validationError is not null)
            throw new ArgumentException(validationError, nameof(payload));

        var blob = VaultItemCipher.Encrypt(session.VaultKey, session.UserId, itemId, payload);
        var updatedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

        await using var connection = _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            UPDATE vault_items
            SET cipher_blob = @cipherBlob, updated_utc = @updatedUtc
            WHERE id = @id AND user_id = @userId;";
        command.Parameters.AddWithValue("@cipherBlob", blob);
        command.Parameters.AddWithValue("@updatedUtc", updatedAt);
        command.Parameters.AddWithValue("@id", itemId.ToString("D"));
        command.Parameters.AddWithValue("@userId", session.UserId);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
            throw new InvalidOperationException("La credencial no existe o no pertenece a este usuario.");
    }

    public async Task DeleteAsync(
        VaultSession session,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await using var connection = _database.OpenConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM vault_items WHERE id = @id AND user_id = @userId;";
        command.Parameters.AddWithValue("@id", itemId.ToString("D"));
        command.Parameters.AddWithValue("@userId", session.UserId);

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affected != 1)
            throw new InvalidOperationException("La credencial no existe o no pertenece a este usuario.");
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        return DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }
}
