namespace SkyTeam.Application.GameSessions;

using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;

public interface IGameSessionRepository
{
    void Create(PersistedGameSession session);
    bool Update(PersistedGameSession session, long expectedVersion);
    PersistedGameSession? GetById(long groupChatId);
    IReadOnlyList<PersistedGameSession> List();
}

public interface IGameSessionPersistence : IGameSessionRepository
{
    PersistedGameSessionStoreState Load();
    void Save(PersistedGameSessionStoreState state);
    int CleanupExpired(DateTimeOffset utcNow);
}

public sealed record PersistedGameSessionStoreState(
    int SchemaVersion,
    IReadOnlyList<PersistedGameSession> Sessions,
    IReadOnlyList<PersistedCockpitMessage> CockpitMessages)
{
    public static PersistedGameSessionStoreState Empty { get; } = new(
        SchemaVersion: 1,
        Sessions: [],
        CockpitMessages: []);
}

public sealed record PersistedGameSession(
    long GroupChatId,
    LobbyPlayer Pilot,
    LobbyPlayer Copilot,
    GameRoundSnapshot Round,
    long Version,
    IReadOnlyList<PersistedRoundLog> RoundLogs,
    DateTimeOffset CreatedAtUtc = default,
    DateTimeOffset UpdatedAtUtc = default,
    DateTimeOffset? ExpiresAtUtc = null);

public sealed record PersistedRoundLog(
    int RoundNumber,
    IReadOnlyList<int> PilotDice,
    IReadOnlyList<int> CopilotDice,
    IReadOnlyList<PersistedLoggedPlacement> Placements,
    bool IsCompleted);

public sealed record PersistedLoggedPlacement(
    string CommandId,
    string CommandDisplayName,
    PlayerSeat Player,
    int DieIndex);

public sealed record PersistedCockpitMessage(long GroupChatId, int CockpitMessageId);

public sealed class NullGameSessionPersistence : IGameSessionPersistence
{
    public static NullGameSessionPersistence Instance { get; } = new();

    private NullGameSessionPersistence()
    {
    }

    public PersistedGameSessionStoreState Load() => PersistedGameSessionStoreState.Empty;

    public void Save(PersistedGameSessionStoreState state)
    {
        ArgumentNullException.ThrowIfNull(state);
    }

    public void Create(PersistedGameSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
    }

    public bool Update(PersistedGameSession session, long expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(session);
        return false;
    }

    public PersistedGameSession? GetById(long groupChatId) => null;

    public IReadOnlyList<PersistedGameSession> List() => [];

    public int CleanupExpired(DateTimeOffset utcNow) => 0;
}
