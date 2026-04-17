namespace SkyTeam.TelegramBot.Persistence;

using System.Reflection;
using Microsoft.Data.Sqlite;

internal static class GameSessionsSchemaMigrator
{
    private const string MigrationId = "0001_game_sessions_schema";
    private const string MigrationsTableName = "SchemaMigrations";
    private const string MigrationResourceName = "SkyTeam.TelegramBot.Persistence.Migrations.0001_game_sessions_schema.sql";

    public static void EnsureMigrated(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var directoryPath = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
            Directory.CreateDirectory(directoryPath);

        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();

        EnsureMigrationsTable(connection);
        if (IsMigrationApplied(connection))
            return;

        var migrationSql = LoadMigrationSqlScript();
        using var transaction = connection.BeginTransaction();

        using (var applyMigrationCommand = connection.CreateCommand())
        {
            applyMigrationCommand.Transaction = transaction;
            applyMigrationCommand.CommandText = migrationSql;
            applyMigrationCommand.ExecuteNonQuery();
        }

        using (var insertMigrationCommand = connection.CreateCommand())
        {
            insertMigrationCommand.Transaction = transaction;
            insertMigrationCommand.CommandText = $"""
                INSERT INTO {MigrationsTableName} (Id, AppliedAtUtc)
                VALUES ($id, $appliedAtUtc);
                """;
            insertMigrationCommand.Parameters.AddWithValue("$id", MigrationId);
            insertMigrationCommand.Parameters.AddWithValue("$appliedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
            insertMigrationCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void EnsureMigrationsTable(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE TABLE IF NOT EXISTS {MigrationsTableName} (
                Id TEXT NOT NULL PRIMARY KEY,
                AppliedAtUtc TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    private static bool IsMigrationApplied(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT COUNT(1)
            FROM {MigrationsTableName}
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", MigrationId);

        return command.ExecuteScalar() switch
        {
            long count => count > 0,
            int count => count > 0,
            _ => false
        };
    }

    private static string LoadMigrationSqlScript()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(MigrationResourceName);
        if (stream is null)
            throw new InvalidOperationException($"Migration resource '{MigrationResourceName}' was not found.");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
