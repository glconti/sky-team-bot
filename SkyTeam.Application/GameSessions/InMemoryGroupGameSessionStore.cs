namespace SkyTeam.Application.GameSessions;

using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;

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
    AwaitingRoll,
    AwaitingPlacements,
    ReadyToResolve
}

public sealed record GameRoundSnapshot(int RoundNumber, GameRoundStatus Status)
{
    public static GameRoundSnapshot StartNew(int roundNumber) => new(roundNumber, GameRoundStatus.AwaitingRoll);
}

public sealed record GameSessionSnapshot(long GroupChatId, LobbyPlayer Pilot, LobbyPlayer Copilot, GameRoundSnapshot Round);

public readonly record struct GameSessionStartResult(GameSessionStartStatus Status, GameSessionSnapshot? Snapshot);

public enum GameSessionRollStatus
{
    Rolled,
    NoSession,
    RoundNotAwaitingRoll
}

public readonly record struct GameSessionRollResult(GameSessionRollStatus Status, GameSessionSnapshot? Snapshot);

public enum GamePlacementStatus
{
    Placed,
    NoActiveSession,
    NotSeated,
    RoundNotRolled,
    RoundNotAcceptingPlacements,
    NotPlayersTurn,
    InvalidDieIndex,
    DieAlreadyUsed,
    InvalidTarget
}

public sealed record GamePlacementPublicInfo(
    long GroupChatId,
    LobbyPlayer Player,
    PlayerSeat Seat,
    int PlacementIndex,
    DieValue Value,
    string Target,
    PlayerSeat NextPlayer,
    int PlacementsRemaining);

public readonly record struct GamePlacementResult(GamePlacementStatus Status, GamePlacementPublicInfo? PublicInfo);

public enum GameHandStatus
{
    Ok,
    NoActiveSession,
    NotSeated,
    RoundNotRolled
}

public readonly record struct GameHandResult(
    GameHandStatus Status,
    PlayerSeat? Seat,
    SecretDiceHand? Hand,
    PlayerSeat? CurrentPlayer,
    int? PlacementsRemaining);

public sealed class InMemoryGroupGameSessionStore
{
    private readonly object _sync = new();
    private readonly Dictionary<long, GameSession> _sessions = new();
    private readonly Dictionary<long, long> _groupChatIdByUserId = new();

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

            _groupChatIdByUserId[created.Pilot.UserId] = groupChatId;
            _groupChatIdByUserId[created.Copilot.UserId] = groupChatId;

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

    public GameSessionRollResult RegisterRoll(long groupChatId, SecretDiceRoll roll, PlayerSeat startingPlayer = PlayerSeat.Pilot)
    {
        ArgumentNullException.ThrowIfNull(roll);

        lock (_sync)
        {
            if (!_sessions.TryGetValue(groupChatId, out var session))
                return new GameSessionRollResult(GameSessionRollStatus.NoSession, Snapshot: null);

            if (session.Round.Status != GameRoundStatus.AwaitingRoll)
                return new GameSessionRollResult(GameSessionRollStatus.RoundNotAwaitingRoll, session.ToSnapshot());

            session.InitializeRoundFromRoll(roll, startingPlayer);
            return new GameSessionRollResult(GameSessionRollStatus.Rolled, session.ToSnapshot());
        }
    }

    public GameHandResult GetHand(long requestingUserId)
    {
        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new GameHandResult(GameHandStatus.NoActiveSession, Seat: null, Hand: null, CurrentPlayer: null, PlacementsRemaining: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out _))
                return new GameHandResult(GameHandStatus.NotSeated, Seat: null, Hand: null, CurrentPlayer: null, PlacementsRemaining: null);

            if (session.TurnState is null)
                return new GameHandResult(GameHandStatus.RoundNotRolled, seat, Hand: null, CurrentPlayer: null, PlacementsRemaining: null);

            var hand = seat == PlayerSeat.Pilot
                ? session.TurnState.PilotHand
                : session.TurnState.CopilotHand;

            return new GameHandResult(GameHandStatus.Ok, seat, hand, session.TurnState.CurrentPlayer, session.TurnState.PlacementsRemaining);
        }
    }

    public GamePlacementResult PlaceDie(long requestingUserId, int dieIndex, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return new GamePlacementResult(GamePlacementStatus.InvalidTarget, PublicInfo: null);

        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new GamePlacementResult(GamePlacementStatus.NoActiveSession, PublicInfo: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out var player))
                return new GamePlacementResult(GamePlacementStatus.NotSeated, PublicInfo: null);

            if (session.TurnState is null)
                return new GamePlacementResult(GamePlacementStatus.RoundNotRolled, PublicInfo: null);

            if (session.TurnState.Phase != RoundPhase.InProgress)
                return new GamePlacementResult(GamePlacementStatus.RoundNotAcceptingPlacements, PublicInfo: null);

            if (seat != session.TurnState.CurrentPlayer)
                return new GamePlacementResult(GamePlacementStatus.NotPlayersTurn, PublicInfo: null);

            if (dieIndex is < 0 or >= SecretDiceHand.DicePerHand)
                return new GamePlacementResult(GamePlacementStatus.InvalidDieIndex, PublicInfo: null);

            var hand = seat == PlayerSeat.Pilot
                ? session.TurnState.PilotHand
                : session.TurnState.CopilotHand;

            if (hand.Dice[dieIndex].IsUsed)
                return new GamePlacementResult(GamePlacementStatus.DieAlreadyUsed, PublicInfo: null);

            session.TurnState = session.TurnState.RegisterPlacement(seat, dieIndex, target);

            if (session.TurnState.IsReadyToResolve)
                session.Round = session.Round with { Status = GameRoundStatus.ReadyToResolve };

            var placed = session.TurnState.Placements[^1];

            var publicInfo = new GamePlacementPublicInfo(
                GroupChatId: session.GroupChatId,
                Player: player!,
                Seat: seat,
                PlacementIndex: placed.Index,
                Value: placed.Value,
                Target: placed.Target,
                NextPlayer: session.TurnState.CurrentPlayer,
                PlacementsRemaining: session.TurnState.PlacementsRemaining);

            return new GamePlacementResult(GamePlacementStatus.Placed, publicInfo);
        }
    }

    private bool TryGetSessionByUserId(long userId, out GameSession session)
    {
        if (!_groupChatIdByUserId.TryGetValue(userId, out var groupChatId))
        {
            session = null!;
            return false;
        }

        return _sessions.TryGetValue(groupChatId, out session!);
    }

    private sealed class GameSession(long groupChatId, LobbyPlayer pilot, LobbyPlayer copilot)
    {
        public long GroupChatId { get; } = groupChatId;
        public LobbyPlayer Pilot { get; } = pilot;
        public LobbyPlayer Copilot { get; } = copilot;
        public GameRoundSnapshot Round { get; set; } = GameRoundSnapshot.StartNew(roundNumber: 1);

        public RoundTurnState? TurnState { get; set; }

        public GameSessionSnapshot ToSnapshot() => new(GroupChatId, Pilot, Copilot, Round);

        public bool TryGetSeat(long userId, out PlayerSeat seat, out LobbyPlayer? player)
        {
            if (Pilot.UserId == userId)
            {
                seat = PlayerSeat.Pilot;
                player = Pilot;
                return true;
            }

            if (Copilot.UserId == userId)
            {
                seat = PlayerSeat.Copilot;
                player = Copilot;
                return true;
            }

            seat = default;
            player = null;
            return false;
        }

        public void InitializeRoundFromRoll(SecretDiceRoll roll, PlayerSeat startingPlayer)
        {
            TurnState = RoundTurnState.StartNew(
                Round.RoundNumber,
                startingPlayer,
                SecretDiceHand.Create(roll.PilotDice),
                SecretDiceHand.Create(roll.CopilotDice));

            Round = Round with { Status = GameRoundStatus.AwaitingPlacements };
        }
    }
}
