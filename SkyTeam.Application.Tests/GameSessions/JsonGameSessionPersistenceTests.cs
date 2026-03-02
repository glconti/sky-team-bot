namespace SkyTeam.Application.Tests.GameSessions;

using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.TelegramBot.Persistence;

public sealed class JsonGameSessionPersistenceTests
{
    [Fact]
    public void RepositoryContract_ShouldSupportCreateUpdateGetByIdAndList_WhenUsingJsonPersistence()
    {
        // Arrange
        var fixture = CreateFixture();

        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var created = CreatePersistedSession(groupChatId: 101, version: 1, nowUtc, nowUtc);
            fixture.Persistence.Create(created);

            // Act
            var listed = fixture.Persistence.List();
            var loaded = fixture.Persistence.GetById(101);
            var updated = fixture.Persistence.Update(
                created with { Version = 2, Round = new GameRoundSnapshot(1, GameRoundStatus.AwaitingPlacements), UpdatedAtUtc = DateTimeOffset.UtcNow },
                expectedVersion: 1);
            var staleUpdate = fixture.Persistence.Update(
                created with { Version = 3, UpdatedAtUtc = DateTimeOffset.UtcNow },
                expectedVersion: 1);

            // Assert
            listed.Should().ContainSingle(session => session.GroupChatId == 101);
            loaded.Should().NotBeNull();
            updated.Should().BeTrue();
            staleUpdate.Should().BeFalse();
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public void CleanupExpired_ShouldRemoveStaleSessions_WhenRetentionWindowIsExceeded()
    {
        // Arrange
        var fixture = CreateFixture(completedRetentionDays: 1, abandonedRetentionDays: 1);

        try
        {
            var nowUtc = DateTimeOffset.UtcNow;
            fixture.Persistence.Create(CreatePersistedSession(
                groupChatId: 202,
                version: 1,
                createdAtUtc: nowUtc.AddHours(-1),
                updatedAtUtc: nowUtc.AddHours(-1)));
            fixture.Persistence.Create(CreatePersistedSession(
                groupChatId: 201,
                version: 1,
                createdAtUtc: nowUtc.AddDays(-3),
                updatedAtUtc: nowUtc.AddDays(-3)));

            // Act
            var removedCount = fixture.Persistence.CleanupExpired(nowUtc);
            var remainingGroupIds = fixture.Persistence.List().Select(session => session.GroupChatId).ToArray();

            // Assert
            removedCount.Should().Be(1);
            remainingGroupIds.Should().BeEquivalentTo([202]);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Fact]
    public void Load_ShouldApplyGameSessionsSchemaMigration_WhenPersistenceIsInitialized()
    {
        // Arrange
        var fixture = CreateFixture();

        try
        {
            // Act
            _ = fixture.Persistence.Load();
            using var connection = new SqliteConnection($"Data Source={fixture.DatabasePath}");
            connection.Open();

            var migrationCount = GetMigrationCount(connection, "0001_game_sessions_schema");
            var columns = GetGameSessionsColumns(connection);
            var hasActiveGroupChatIndex = HasIndex(connection, "UX_GameSessions_Active_GroupChatId");

            // Assert
            migrationCount.Should().Be(1);
            columns.Should().Contain([
                "GameId",
                "GroupChatId",
                "PilotUserId",
                "CopilotUserId",
                "StateJson",
                "Status",
                "Version",
                "CreatedAtUtc",
                "UpdatedAtUtc",
                "ExpiresAtUtc"]);
            hasActiveGroupChatIndex.Should().BeTrue();
        }
        finally
        {
            fixture.Dispose();
        }
    }

    private static PersistedGameSession CreatePersistedSession(
        long groupChatId,
        long version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
        => new(
            GroupChatId: groupChatId,
            Pilot: new LobbyPlayer(11, "Pilot"),
            Copilot: new LobbyPlayer(22, "Copilot"),
            Round: GameRoundSnapshot.StartNew(roundNumber: 1),
            Version: version,
            RoundLogs: [],
            CreatedAtUtc: createdAtUtc,
            UpdatedAtUtc: updatedAtUtc);

    private static PersistenceFixture CreateFixture(int completedRetentionDays = 30, int abandonedRetentionDays = 30)
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"skyteam-json-persistence-{Guid.NewGuid():N}");
        Directory.CreateDirectory(rootPath);
        var filePath = Path.Combine(rootPath, "game-sessions.json");
        var databasePath = Path.Combine(rootPath, "game-sessions.db");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:GameSessionsFilePath"] = filePath,
                ["Persistence:GameSessionsDatabasePath"] = databasePath,
                ["Persistence:CompletedSessionRetentionDays"] = completedRetentionDays.ToString(),
                ["Persistence:AbandonedSessionRetentionDays"] = abandonedRetentionDays.ToString()
            })
            .Build();

        var hostEnvironment = new TestHostEnvironment(rootPath);
        return new(new JsonGameSessionPersistence(configuration, hostEnvironment), rootPath, databasePath);
    }

    private static long GetMigrationCount(SqliteConnection connection, string migrationId)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM SchemaMigrations
            WHERE Id = $id;
            """;
        command.Parameters.AddWithValue("$id", migrationId);
        return command.ExecuteScalar() switch
        {
            long count => count,
            int count => count,
            _ => 0
        };
    }

    private static string[] GetGameSessionsColumns(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA table_info(GameSessions);";
        using var reader = command.ExecuteReader();

        var columns = new List<string>();
        while (reader.Read())
            columns.Add(reader.GetString(1));

        return [.. columns];
    }

    private static bool HasIndex(SqliteConnection connection, string indexName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'index'
              AND name = $indexName;
            """;
        command.Parameters.AddWithValue("$indexName", indexName);

        return command.ExecuteScalar() switch
        {
            long count => count > 0,
            int count => count > 0,
            _ => false
        };
    }

    private sealed record PersistenceFixture(JsonGameSessionPersistence Persistence, string RootPath, string DatabasePath) : IDisposable
    {
        public void Dispose()
        {
            SqliteConnection.ClearAllPools();

            if (Directory.Exists(RootPath))
                Directory.Delete(RootPath, recursive: true);
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "SkyTeam.Application.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
