namespace SkyTeam.Application.GameSessions;

using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;
using SkyTeam.Domain;

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
    ReadyToResolve,
    GameOver
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

public readonly record struct GameSessionRollResult(GameSessionRollStatus Status, GameSessionSnapshot? Snapshot, PlayerSeat? StartingPlayer);

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
    InvalidTarget,
    CommandNotAvailable,
    CommandDoesNotMatchDie,
    DomainError
}

public sealed record GamePlacementPublicInfo(
    long GroupChatId,
    LobbyPlayer Player,
    PlayerSeat Seat,
    int PlacementIndex,
    DieValue Value,
    string CommandId,
    string CommandDisplayName,
    PlayerSeat NextPlayer,
    int PlacementsRemaining);

public sealed record GameStatePublicSnapshot(
    int AxisPosition,
    int ApproachPositionIndex,
    int ApproachSegmentCount,
    int TotalPlanesRemaining,
    int CoffeeTokens,
    int? EnginesSpeed,
    int BrakesActivatedSwitchCount,
    int BrakesCapability,
    int FlapsValue,
    int LandingGearValue,
    double BlueAerodynamicsThreshold,
    double OrangeAerodynamicsThreshold);

public sealed record GameRoundResolutionPublicInfo(
    long GroupChatId,
    int ResolvedRoundNumber,
    GameStatePublicSnapshot ResolvedState,
    string GameStatus,
    int? NextRoundNumber,
    PlayerSeat? NextStartingPlayer);

public readonly record struct GamePlacementResult(
    GamePlacementStatus Status,
    GamePlacementPublicInfo? PublicInfo,
    GameRoundResolutionPublicInfo? ResolutionInfo,
    string? ErrorMessage);

public enum GameHandStatus
{
    Ok,
    NoActiveSession,
    NotSeated,
    RoundNotRolled
}

public sealed record AvailableGameCommand(string CommandId, string DisplayName);

public readonly record struct GameHandResult(
    GameHandStatus Status,
    PlayerSeat? Seat,
    SecretDiceHand? Hand,
    PlayerSeat? CurrentPlayer,
    int? PlacementsRemaining,
    IReadOnlyList<AvailableGameCommand>? AvailableCommands);

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

    public bool TryGetGroupChatIdForUserId(long userId, out long groupChatId)
    {
        lock (_sync)
            return _groupChatIdByUserId.TryGetValue(userId, out groupChatId);
    }

    public GameSessionRollResult RegisterRoll(long groupChatId, SecretDiceRoll roll, PlayerSeat startingPlayer = PlayerSeat.Pilot)
    {
        ArgumentNullException.ThrowIfNull(roll);

        lock (_sync)
        {
            if (!_sessions.TryGetValue(groupChatId, out var session))
                return new GameSessionRollResult(GameSessionRollStatus.NoSession, Snapshot: null, StartingPlayer: null);

            if (session.Round.Status != GameRoundStatus.AwaitingRoll)
                return new GameSessionRollResult(GameSessionRollStatus.RoundNotAwaitingRoll, session.ToSnapshot(), StartingPlayer: null);

            var actualStartingPlayer = session.InitializeRoundFromRoll(roll, startingPlayer);
            return new GameSessionRollResult(GameSessionRollStatus.Rolled, session.ToSnapshot(), actualStartingPlayer);
        }
    }

    public GameHandResult GetHand(long requestingUserId)
    {
        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new GameHandResult(GameHandStatus.NoActiveSession, Seat: null, Hand: null, CurrentPlayer: null, PlacementsRemaining: null, AvailableCommands: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out _))
                return new GameHandResult(GameHandStatus.NotSeated, Seat: null, Hand: null, CurrentPlayer: null, PlacementsRemaining: null, AvailableCommands: null);

            if (session.TurnState is null)
                return new GameHandResult(GameHandStatus.RoundNotRolled, seat, Hand: null, CurrentPlayer: null, PlacementsRemaining: null, AvailableCommands: null);

            var hand = seat == PlayerSeat.Pilot
                ? session.TurnState.PilotHand
                : session.TurnState.CopilotHand;

            IReadOnlyList<AvailableGameCommand> commands = Array.Empty<AvailableGameCommand>();

            if (seat == session.TurnState.CurrentPlayer)
            {
                commands = session.DomainGame.GetAvailableCommands()
                    .Select(c => new AvailableGameCommand(c.CommandId, c.DisplayName))
                    .ToArray();
            }

            return new GameHandResult(
                GameHandStatus.Ok,
                seat,
                hand,
                session.TurnState.CurrentPlayer,
                session.TurnState.PlacementsRemaining,
                commands);
        }
    }

    public GamePlacementResult PlaceDie(long requestingUserId, int dieIndex, string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return new GamePlacementResult(GamePlacementStatus.InvalidTarget, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new GamePlacementResult(GamePlacementStatus.NoActiveSession, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out var player))
                return new GamePlacementResult(GamePlacementStatus.NotSeated, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (session.TurnState is null)
                return new GamePlacementResult(GamePlacementStatus.RoundNotRolled, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (session.TurnState.Phase != RoundPhase.InProgress)
                return new GamePlacementResult(GamePlacementStatus.RoundNotAcceptingPlacements, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (seat != session.TurnState.CurrentPlayer)
                return new GamePlacementResult(GamePlacementStatus.NotPlayersTurn, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (dieIndex is < 0 or >= SecretDiceHand.DicePerHand)
                return new GamePlacementResult(GamePlacementStatus.InvalidDieIndex, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            var hand = seat == PlayerSeat.Pilot
                ? session.TurnState.PilotHand
                : session.TurnState.CopilotHand;

            var die = hand.Dice[dieIndex];
            if (die.IsUsed)
                return new GamePlacementResult(GamePlacementStatus.DieAlreadyUsed, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (!TryGetRolledValueFromCommandId(commandId, out var rolledValue))
                return new GamePlacementResult(GamePlacementStatus.InvalidTarget, PublicInfo: null, ResolutionInfo: null, ErrorMessage: "Invalid command id.");

            if (rolledValue != die.Value.Value)
                return new GamePlacementResult(GamePlacementStatus.CommandDoesNotMatchDie, PublicInfo: null, ResolutionInfo: null, ErrorMessage: "The command does not match the selected die.");

            var command = session.DomainGame.GetAvailableCommands().SingleOrDefault(c => c.CommandId == commandId);
            if (command is null)
                return new GamePlacementResult(GamePlacementStatus.CommandNotAvailable, PublicInfo: null, ResolutionInfo: null, ErrorMessage: "That command is not currently available.");

            try
            {
                session.DomainGame.ExecuteCommand(commandId);
            }
            catch (InvalidOperationException) when (session.DomainGame.Status == GameStatus.Lost)
            {
                // Placement was valid but caused a loss.
            }
            catch (Exception exception)
            {
                return new GamePlacementResult(GamePlacementStatus.DomainError, PublicInfo: null, ResolutionInfo: null, ErrorMessage: exception.Message);
            }

            session.TurnState = session.TurnState.RegisterPlacement(seat, dieIndex, commandId);

            var placed = session.TurnState.Placements[^1];
            var nextPlayer = session.TurnState.CurrentPlayer;
            var placementsRemaining = session.TurnState.PlacementsRemaining;

            GameRoundResolutionPublicInfo? resolutionInfo = null;

            if (session.TurnState.IsReadyToResolve)
                resolutionInfo = session.ResolveRoundAndAdvance();

            var publicInfo = new GamePlacementPublicInfo(
                GroupChatId: session.GroupChatId,
                Player: player!,
                Seat: seat,
                PlacementIndex: placed.Index,
                Value: placed.Value,
                CommandId: command.CommandId,
                CommandDisplayName: command.DisplayName,
                NextPlayer: nextPlayer,
                PlacementsRemaining: placementsRemaining);

            return new GamePlacementResult(GamePlacementStatus.Placed, publicInfo, resolutionInfo, ErrorMessage: null);
        }
    }

    private static bool TryGetRolledValueFromCommandId(string commandId, out int rolledValue)
    {
        rolledValue = default;

        var colonIndex = commandId.LastIndexOf(':');
        if (colonIndex < 0 || colonIndex == commandId.Length - 1) return false;

        var endIndex = commandId.IndexOf('>', colonIndex + 1);
        if (endIndex < 0) endIndex = commandId.Length;

        return int.TryParse(commandId.AsSpan(colonIndex + 1, endIndex - colonIndex - 1), out rolledValue);
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

    private sealed class GameSession
    {
        private readonly Airport _airport;
        private readonly AxisPositionModule _axis;
        private readonly EnginesModule _engines;
        private readonly BrakesModule _brakes;
        private readonly FlapsModule _flaps;
        private readonly LandingGearModule _landingGear;
        private readonly RadioModule _radio;
        private readonly ConcentrationModule _concentration;

        public GameSession(long groupChatId, LobbyPlayer pilot, LobbyPlayer copilot)
        {
            GroupChatId = groupChatId;
            Pilot = pilot;
            Copilot = copilot;

            _airport = (Airport)new MontrealAirport();
            var altitude = new Altitude();

            _axis = new AxisPositionModule();
            _engines = new EnginesModule(_airport);
            _brakes = new BrakesModule();
            _flaps = new FlapsModule(_airport);
            _landingGear = new LandingGearModule(_airport);
            _radio = new RadioModule(_airport);
            _concentration = new ConcentrationModule();

            DomainGame = new Game(
                _airport,
                altitude,
                [_axis, _engines, _brakes, _flaps, _landingGear, _radio, _concentration]);
        }

        public long GroupChatId { get; }
        public LobbyPlayer Pilot { get; }
        public LobbyPlayer Copilot { get; }

        public Game DomainGame { get; }

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

        public PlayerSeat InitializeRoundFromRoll(SecretDiceRoll roll, PlayerSeat startingPlayer)
        {
            DomainGame.SetRoundDice(roll.PilotDice, roll.CopilotDice);

            var actualStartingPlayer = ToSeat(DomainGame.CurrentPlayer);

            TurnState = RoundTurnState.StartNew(
                Round.RoundNumber,
                actualStartingPlayer,
                SecretDiceHand.Create(roll.PilotDice),
                SecretDiceHand.Create(roll.CopilotDice));

            Round = Round with { Status = GameRoundStatus.AwaitingPlacements };

            return actualStartingPlayer;
        }

        public GameRoundResolutionPublicInfo ResolveRoundAndAdvance()
        {
            var resolvedRoundNumber = Round.RoundNumber;
            var resolvedState = CreateStateSnapshot();
            var gameStatus = DomainGame.Status.ToString();

            int? nextRoundNumber = null;
            PlayerSeat? nextStartingPlayer = null;

            if (DomainGame.Status == GameStatus.InProgress)
            {
                DomainGame.ExecuteCommand("NextRound");

                nextRoundNumber = Round.RoundNumber + 1;
                nextStartingPlayer = ToSeat(DomainGame.CurrentPlayer);

                Round = GameRoundSnapshot.StartNew(roundNumber: nextRoundNumber.Value);
            }
            else
            {
                Round = Round with { Status = GameRoundStatus.GameOver };
            }

            TurnState = null;

            return new GameRoundResolutionPublicInfo(
                GroupChatId,
                resolvedRoundNumber,
                resolvedState,
                gameStatus,
                nextRoundNumber,
                nextStartingPlayer);
        }

        private GameStatePublicSnapshot CreateStateSnapshot()
        {
            var totalPlanes = _airport.PathSegments.Sum(segment => segment.PlaneTokens);

            return new GameStatePublicSnapshot(
                AxisPosition: _axis.AxisPosition,
                ApproachPositionIndex: _airport.CurrentPositionIndex,
                ApproachSegmentCount: _airport.SegmentCount,
                TotalPlanesRemaining: totalPlanes,
                CoffeeTokens: DomainGame.TokenPool.Count,
                EnginesSpeed: _engines.LastSpeed,
                BrakesActivatedSwitchCount: _brakes.ActivatedSwitchCount,
                BrakesCapability: _brakes.BrakingCapability,
                FlapsValue: _flaps.FlapsValue,
                LandingGearValue: _landingGear.LandingGearValue,
                BlueAerodynamicsThreshold: _airport.BlueAerodynamicsThreshold,
                OrangeAerodynamicsThreshold: _airport.OrangeAerodynamicsThreshold);
        }

        private static PlayerSeat ToSeat(Player player) => player == Player.Pilot
            ? PlayerSeat.Pilot
            : PlayerSeat.Copilot;
    }
}
