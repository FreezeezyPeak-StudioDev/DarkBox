using System.Globalization;
using Microsoft.Data.Sqlite;

namespace DarkBox.Data;

public sealed class DarkBoxDatabase
{
    private const int SchemaVersion = 1;
    private readonly string _connectionString;

    public DarkBoxDatabase(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
            throw new ArgumentException("La ruta de la base de datos no es válida.", nameof(databasePath));

        DatabasePath = Path.GetFullPath(databasePath);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
        }.ToString();
    }

    public string DatabasePath { get; }

    public static string DefaultDatabasePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DarkBox",
        "darkbox.db");

    public SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA busy_timeout = 5000;";
        command.ExecuteNonQuery();

        return connection;
    }

    public void Initialize()
    {
        var directory = Path.GetDirectoryName(DatabasePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        using var connection = OpenConnection();

        using (var versionCommand = connection.CreateCommand())
        {
            versionCommand.CommandText = "PRAGMA user_version;";
            var version = Convert.ToInt32(versionCommand.ExecuteScalar(), CultureInfo.InvariantCulture);

            if (version > SchemaVersion)
                throw new InvalidOperationException(
                    $"La base de datos usa una versión más reciente ({version}) que esta aplicación ({SchemaVersion}).");

            if (version == 0)
                CreateSchema(connection);
        }

        using (var versionCommand = connection.CreateCommand())
        {
            versionCommand.CommandText = $"PRAGMA user_version = {SchemaVersion};";
            versionCommand.ExecuteNonQuery();
        }

        using var walCommand = connection.CreateCommand();
        walCommand.CommandText = "PRAGMA journal_mode = WAL;";
        walCommand.ExecuteNonQuery();
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS users (
                id                 INTEGER PRIMARY KEY AUTOINCREMENT,
                vault_id           TEXT    NOT NULL UNIQUE,
                username           TEXT    NOT NULL UNIQUE COLLATE NOCASE,
                salt               BLOB    NOT NULL,
                kdf_parameters     TEXT    NOT NULL,
                wrapped_vault_key  BLOB    NOT NULL,
                created_utc        TEXT    NOT NULL,
                updated_utc        TEXT    NOT NULL
            );

            CREATE TABLE IF NOT EXISTS vault_items (
                id          TEXT    NOT NULL PRIMARY KEY,
                user_id     INTEGER NOT NULL,
                cipher_blob BLOB    NOT NULL,
                created_utc TEXT    NOT NULL,
                updated_utc TEXT    NOT NULL,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_vault_items_user_id
                ON vault_items(user_id);";
        command.ExecuteNonQuery();
    }
}
