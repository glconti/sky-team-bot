namespace SkyTeam.TelegramBot.Persistence;

using System.Text.Json;
using SkyTeam.Application.GameSessions;

public sealed class JsonGameSessionPersistence(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment) : IGameSessionPersistence
{
    private const int DefaultCompletedRetentionDays = 30;
    private const int DefaultAbandonedRetentionDays = 30;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly string _filePath = ResolveFilePath(
        configuration["Persistence:GameSessionsFilePath"],
        hostEnvironment.ContentRootPath);
    private readonly TimeSpan _completedRetention = TimeSpan.FromDays(ResolveRetentionDays(
        configuration["Persistence:CompletedSessionRetentionDays"],
        DefaultCompletedRetentionDays));
    private readonly TimeSpan _abandonedRetention = TimeSpan.FromDays(ResolveRetentionDays(
        configuration["Persistence:AbandonedSessionRetentionDays"],
        DefaultAbandonedRetentionDays));

    public PersistedGameSessionStoreState Load()
    {
        lock (_sync)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            return ReadState(nowUtc, persistChanges: true);
        }
    }

    public void Save(PersistedGameSessionStoreState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_sync)
        {
            var normalizedState = NormalizeAndCleanup(state, DateTimeOffset.UtcNow, out _, out _);
            WriteState(normalizedState);
        }
    }

    public void Create(PersistedGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_sync)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var currentState = ReadState(nowUtc, persistChanges: true);

            if (currentState.Sessions.Any(existing => existing.GroupChatId == session.GroupChatId))
                throw new InvalidOperationException($"A game session already exists for group chat {session.GroupChatId}.");

            var createdSession = NormalizeSession(session, nowUtc);
            var updatedState = currentState with { Sessions = [.. currentState.Sessions, createdSession] };
            WriteState(updatedState);
        }
    }

    public bool Update(PersistedGameSession session, long expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_sync)
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var currentState = ReadState(nowUtc, persistChanges: true);
            var sessions = currentState.Sessions.ToArray();
            var existingIndex = Array.FindIndex(sessions, existing => existing.GroupChatId == session.GroupChatId);

            if (existingIndex < 0)
                return false;

            var existing = sessions[existingIndex];
            if (existing.Version != expectedVersion)
                return false;

            var createdAtUtc = session.CreatedAtUtc == default ? existing.CreatedAtUtc : session.CreatedAtUtc;
            sessions[existingIndex] = NormalizeSession(session with { CreatedAtUtc = createdAtUtc }, nowUtc);
            WriteState(currentState with { Sessions = sessions });
            return true;
        }
    }

    public PersistedGameSession? GetById(long groupChatId)
    {
        lock (_sync)
            return ReadState(DateTimeOffset.UtcNow, persistChanges: true)
                .Sessions
                .SingleOrDefault(session => session.GroupChatId == groupChatId);
    }

    public IReadOnlyList<PersistedGameSession> List()
    {
        lock (_sync)
            return ReadState(DateTimeOffset.UtcNow, persistChanges: true)
                .Sessions
                .OrderBy(session => session.GroupChatId)
                .ToArray();
    }

    public int CleanupExpired(DateTimeOffset utcNow)
    {
        lock (_sync)
        {
            var state = ReadStateRaw();
            var cleaned = NormalizeAndCleanup(state, utcNow, out var removedCount, out var changed);
            if (changed)
                WriteState(cleaned);

            return removedCount;
        }
    }

    private PersistedGameSessionStoreState ReadState(DateTimeOffset nowUtc, bool persistChanges)
    {
        var state = ReadStateRaw();
        var normalizedState = NormalizeAndCleanup(state, nowUtc, out _, out var changed);

        if (persistChanges && changed)
            WriteState(normalizedState);

        return normalizedState;
    }

    private PersistedGameSessionStoreState ReadStateRaw()
    {
        if (!File.Exists(_filePath))
            return PersistedGameSessionStoreState.Empty;

        using var stream = File.OpenRead(_filePath);
        return JsonSerializer.Deserialize<PersistedGameSessionStoreState>(stream, SerializerOptions)
            ?? PersistedGameSessionStoreState.Empty;
    }

    private PersistedGameSessionStoreState NormalizeAndCleanup(
        PersistedGameSessionStoreState state,
        DateTimeOffset nowUtc,
        out int removedCount,
        out bool changed)
    {
        ArgumentNullException.ThrowIfNull(state);

        var normalizedSessions = new List<PersistedGameSession>(state.Sessions.Count);
        var removed = 0;
        var hasChanges = false;

        foreach (var session in state.Sessions)
        {
            var normalized = NormalizeSession(session, nowUtc);
            if (normalized.ExpiresAtUtc.HasValue && normalized.ExpiresAtUtc.Value <= nowUtc)
            {
                removed++;
                hasChanges = true;
                continue;
            }

            if (!Equals(session, normalized))
                hasChanges = true;

            normalizedSessions.Add(normalized);
        }

        removedCount = removed;
        changed = hasChanges;
        return hasChanges
            ? state with { Sessions = normalizedSessions }
            : state;
    }

    private PersistedGameSession NormalizeSession(PersistedGameSession session, DateTimeOffset nowUtc)
    {
        var createdAtUtc = session.CreatedAtUtc == default ? nowUtc : session.CreatedAtUtc;
        var updatedAtUtc = session.UpdatedAtUtc == default ? createdAtUtc : session.UpdatedAtUtc;
        var retention = session.Round.Status == GameRoundStatus.GameOver
            ? _completedRetention
            : _abandonedRetention;
        var expiresAtUtc = updatedAtUtc.Add(retention);

        return session with
        {
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    private void WriteState(PersistedGameSessionStoreState state)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempFilePath = $"{_filePath}.{Guid.NewGuid():N}.tmp";

        try
        {
            using (var stream = File.Create(tempFilePath))
            {
                JsonSerializer.Serialize(stream, state, SerializerOptions);
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempFilePath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);
        }
    }

    private static string ResolveFilePath(string? configuredPath, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(Path.Combine(contentRootPath, "data", "game-sessions.json"));

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }

    private static int ResolveRetentionDays(string? configuredDays, int defaultValue)
    {
        if (!int.TryParse(configuredDays, out var parsedDays) || parsedDays < 1)
            return defaultValue;

        return parsedDays;
    }
}
