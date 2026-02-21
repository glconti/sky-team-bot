namespace SkyTeam.Application.Lobby;

public enum LobbyCreateStatus
{
    Created,
    AlreadyExists
}

public enum LobbyJoinStatus
{
    JoinedAsPilot,
    JoinedAsCopilot,
    AlreadySeated,
    Full,
    NoLobby
}

public sealed record LobbyPlayer(long UserId, string DisplayName);

public sealed record LobbySnapshot(long GroupChatId, LobbyPlayer? Pilot, LobbyPlayer? Copilot)
{
    public bool IsReady => Pilot is not null && Copilot is not null;
}

public readonly record struct LobbyCreateResult(LobbyCreateStatus Status, LobbySnapshot Snapshot);

public readonly record struct LobbyJoinResult(LobbyJoinStatus Status, LobbySnapshot? Snapshot);

public sealed class InMemoryGroupLobbyStore
{
    private readonly object _sync = new();
    private readonly Dictionary<long, LobbySession> _sessions = new();

    public LobbyCreateResult CreateNew(long groupChatId)
    {
        lock (_sync)
        {
            if (_sessions.TryGetValue(groupChatId, out var existing))
                return new LobbyCreateResult(LobbyCreateStatus.AlreadyExists, existing.ToSnapshot());

            var created = new LobbySession(groupChatId);
            _sessions.Add(groupChatId, created);

            return new LobbyCreateResult(LobbyCreateStatus.Created, created.ToSnapshot());
        }
    }

    public LobbyJoinResult Join(long groupChatId, LobbyPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        lock (_sync)
        {
            if (!_sessions.TryGetValue(groupChatId, out var session))
                return new LobbyJoinResult(LobbyJoinStatus.NoLobby, null);

            if (session.Pilot?.UserId == player.UserId || session.Copilot?.UserId == player.UserId)
                return new LobbyJoinResult(LobbyJoinStatus.AlreadySeated, session.ToSnapshot());

            if (session.Pilot is null)
            {
                session.Pilot = player;
                return new LobbyJoinResult(LobbyJoinStatus.JoinedAsPilot, session.ToSnapshot());
            }

            if (session.Copilot is null)
            {
                session.Copilot = player;
                return new LobbyJoinResult(LobbyJoinStatus.JoinedAsCopilot, session.ToSnapshot());
            }

            return new LobbyJoinResult(LobbyJoinStatus.Full, session.ToSnapshot());
        }
    }

    public LobbySnapshot? GetSnapshot(long groupChatId)
    {
        lock (_sync)
        {
            return _sessions.TryGetValue(groupChatId, out var session)
                ? session.ToSnapshot()
                : null;
        }
    }

    private sealed class LobbySession(long groupChatId)
    {
        public long GroupChatId { get; } = groupChatId;
        public LobbyPlayer? Pilot { get; set; }
        public LobbyPlayer? Copilot { get; set; }

        public LobbySnapshot ToSnapshot() => new(GroupChatId, Pilot, Copilot);
    }
}
