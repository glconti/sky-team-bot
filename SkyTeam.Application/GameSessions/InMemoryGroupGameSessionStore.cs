namespace SkyTeam.Application.GameSessions;

using SkyTeam.Application.Lobby;

public enum GameSessionStartStatus
{
    Started,
    AlreadyStarted,
    NoLobby,
    LobbyNotReady,
    NotSeated
}

public enum GameRoundStatus
{
    AwaitingRoll
}

public sealed record GameRoundSnapshot(int RoundNumber, GameRoundStatus Status)
{
    public static GameRoundSnapshot StartNew(int roundNumber) => new(roundNumber, GameRoundStatus.AwaitingRoll);
}

public sealed record GameSessionSnapshot(long GroupChatId, LobbyPlayer Pilot, LobbyPlayer Copilot, GameRoundSnapshot Round);

public readonly record struct GameSessionStartResult(GameSessionStartStatus Status, GameSessionSnapshot? Snapshot);

public sealed class InMemoryGroupGameSessionStore
{
    private readonly object _sync = new();
    private readonly Dictionary<long, GameSession> _sessions = new();

    public GameSessionStartResult Start(long groupChatId, LobbySnapshot? lobbySnapshot, long requestingUserId)
    {
        if (lobbySnapshot is null)
            return new GameSessionStartResult(GameSessionStartStatus.NoLobby, null);

        if (!lobbySnapshot.IsReady)
            return new GameSessionStartResult(GameSessionStartStatus.LobbyNotReady, null);

        if (lobbySnapshot.Pilot!.UserId != requestingUserId && lobbySnapshot.Copilot!.UserId != requestingUserId)
            return new GameSessionStartResult(GameSessionStartStatus.NotSeated, null);

        lock (_sync)
        {
            if (_sessions.TryGetValue(groupChatId, out var existing))
                return new GameSessionStartResult(GameSessionStartStatus.AlreadyStarted, existing.ToSnapshot());

            var created = new GameSession(groupChatId, lobbySnapshot.Pilot!, lobbySnapshot.Copilot!);
            _sessions.Add(groupChatId, created);

            return new GameSessionStartResult(GameSessionStartStatus.Started, created.ToSnapshot());
        }
    }

    public GameSessionSnapshot? GetSnapshot(long groupChatId)
    {
        lock (_sync)
        {
            return _sessions.TryGetValue(groupChatId, out var session)
                ? session.ToSnapshot()
                : null;
        }
    }

    private sealed class GameSession(long groupChatId, LobbyPlayer pilot, LobbyPlayer copilot)
    {
        public long GroupChatId { get; } = groupChatId;
        public LobbyPlayer Pilot { get; } = pilot;
        public LobbyPlayer Copilot { get; } = copilot;
        public GameRoundSnapshot Round { get; } = GameRoundSnapshot.StartNew(roundNumber: 1);

        public GameSessionSnapshot ToSnapshot() => new(GroupChatId, Pilot, Copilot, Round);
    }
}
