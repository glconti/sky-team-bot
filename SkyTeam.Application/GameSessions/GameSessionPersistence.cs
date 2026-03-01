namespace SkyTeam.Application.GameSessions;

using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;

public interface IGameSessionPersistence
{
    PersistedGameSessionStoreState Load();
    void Save(PersistedGameSessionStoreState state);
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
    IReadOnlyList<PersistedRoundLog> RoundLogs);

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
}
