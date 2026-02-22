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

public sealed record GameSessionPublicState(
    GameSessionSnapshot Session,
    GameStatePublicSnapshot Cockpit,
    string GameStatus,
    PlayerSeat? CurrentPlayer,
    int? PlacementsMade,
    int? PlacementsRemaining);

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

public enum GameUndoStatus
{
    Undone,
    NoActiveSession,
    NotSeated,
    RoundNotRolled,
    UndoNotAllowed,
    DomainError
}

public sealed record GameUndoPublicInfo(
    long GroupChatId,
    LobbyPlayer Player,
    PlayerSeat Seat,
    int UndonePlacementIndex,
    DieValue Value,
    string CommandId,
    string CommandDisplayName,
    PlayerSeat NextPlayer,
    int PlacementsRemaining);

public readonly record struct GameUndoResult(
    GameUndoStatus Status,
    GameUndoPublicInfo? PublicInfo,
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
    private readonly Dictionary<long, int> _cockpitMessageIdByGroupChatId = new();

    public GameSessionStartResult Start(long groupChatId, LobbySnapshot? lobbySnapshot, long requestingUserId)
    {
        if (lobbySnapshot is null)
            return new(GameSessionStartStatus.NoLobby, null);

        if (!lobbySnapshot.IsReady)
            return new(GameSessionStartStatus.LobbyNotReady, null);

        if (lobbySnapshot.Pilot!.UserId != requestingUserId && lobbySnapshot.Copilot!.UserId != requestingUserId)
            return new(GameSessionStartStatus.NotSeated, null);

        lock (_sync)
        {
            if (_sessions.TryGetValue(groupChatId, out var existing))
                return new(GameSessionStartStatus.AlreadyStarted, existing.ToSnapshot());

            var created = new GameSession(groupChatId, lobbySnapshot.Pilot!, lobbySnapshot.Copilot!);
            _sessions.Add(groupChatId, created);

            _groupChatIdByUserId[created.Pilot.UserId] = groupChatId;
            _groupChatIdByUserId[created.Copilot.UserId] = groupChatId;

            return new(GameSessionStartStatus.Started, created.ToSnapshot());
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

    public bool TryGetCockpitMessageId(long groupChatId, out int cockpitMessageId)
    {
        lock (_sync)
            return _cockpitMessageIdByGroupChatId.TryGetValue(groupChatId, out cockpitMessageId);
    }

    public void SetCockpitMessageId(long groupChatId, int cockpitMessageId)
    {
        lock (_sync)
            _cockpitMessageIdByGroupChatId[groupChatId] = cockpitMessageId;
    }

    public GameSessionPublicState? GetPublicState(long groupChatId)
    {
        lock (_sync)
        {
            if (!_sessions.TryGetValue(groupChatId, out var session))
                return null;

            return new(
                Session: session.ToSnapshot(),
                Cockpit: session.CreateStateSnapshot(),
                GameStatus: session.DomainGame.Status.ToString(),
                CurrentPlayer: session.TurnState?.CurrentPlayer,
                PlacementsMade: session.TurnState?.PlacementsMade,
                PlacementsRemaining: session.TurnState?.PlacementsRemaining);
        }
    }

    public GameSessionRollResult RegisterRoll(long groupChatId, SecretDiceRoll roll, PlayerSeat startingPlayer = PlayerSeat.Pilot)
    {
        ArgumentNullException.ThrowIfNull(roll);

        lock (_sync)
        {
            if (!_sessions.TryGetValue(groupChatId, out var session))
                return new(GameSessionRollStatus.NoSession, Snapshot: null, StartingPlayer: null);

            if (session.Round.Status != GameRoundStatus.AwaitingRoll)
                return new(GameSessionRollStatus.RoundNotAwaitingRoll, session.ToSnapshot(), StartingPlayer: null);

            var actualStartingPlayer = session.InitializeRoundFromRoll(roll, startingPlayer);
            return new(GameSessionRollStatus.Rolled, session.ToSnapshot(), actualStartingPlayer);
        }
    }

    public GameHandResult GetHand(long requestingUserId)
    {
        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new(GameHandStatus.NoActiveSession, Seat: null, Hand: null, CurrentPlayer: null, PlacementsRemaining: null, AvailableCommands: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out _))
                return new(GameHandStatus.NotSeated, Seat: null, Hand: null, CurrentPlayer: null, PlacementsRemaining: null, AvailableCommands: null);

            if (session.TurnState is null)
                return new(GameHandStatus.RoundNotRolled, seat, Hand: null, CurrentPlayer: null, PlacementsRemaining: null, AvailableCommands: null);

            var hand = seat == PlayerSeat.Pilot
                ? session.TurnState.PilotHand
                : session.TurnState.CopilotHand;

            IReadOnlyList<AvailableGameCommand> commands = [];

            if (seat == session.TurnState.CurrentPlayer)
            {
                commands = session.DomainGame.GetAvailableCommands()
                    .Select(c => new AvailableGameCommand(c.CommandId, c.DisplayName))
                    .ToArray();
            }

            return new(
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
            return new(GamePlacementStatus.InvalidTarget, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new(GamePlacementStatus.NoActiveSession, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out var player))
                return new(GamePlacementStatus.NotSeated, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (session.TurnState is null)
                return new(GamePlacementStatus.RoundNotRolled, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (session.TurnState.Phase != RoundPhase.InProgress)
                return new(GamePlacementStatus.RoundNotAcceptingPlacements, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (seat != session.TurnState.CurrentPlayer)
                return new(GamePlacementStatus.NotPlayersTurn, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (dieIndex is < 0 or >= SecretDiceHand.DicePerHand)
                return new(GamePlacementStatus.InvalidDieIndex, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            var hand = seat == PlayerSeat.Pilot
                ? session.TurnState.PilotHand
                : session.TurnState.CopilotHand;

            var die = hand.Dice[dieIndex];
            if (die.IsUsed)
                return new(GamePlacementStatus.DieAlreadyUsed, PublicInfo: null, ResolutionInfo: null, ErrorMessage: null);

            if (!TryGetRolledValueFromCommandId(commandId, out var rolledValue))
                return new(GamePlacementStatus.InvalidTarget, PublicInfo: null, ResolutionInfo: null, ErrorMessage: "Invalid command id.");

            if (rolledValue != die.Value.Value)
                return new(GamePlacementStatus.CommandDoesNotMatchDie, PublicInfo: null, ResolutionInfo: null, ErrorMessage: "The command does not match the selected die.");

            var command = session.DomainGame.GetAvailableCommands().SingleOrDefault(c => c.CommandId == commandId);
            if (command is null)
                return new(GamePlacementStatus.CommandNotAvailable, PublicInfo: null, ResolutionInfo: null, ErrorMessage: "That command is not currently available.");

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
                return new(GamePlacementStatus.DomainError, PublicInfo: null, ResolutionInfo: null, ErrorMessage: exception.Message);
            }

            session.LogPlacement(command.CommandId, command.DisplayName);
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

            return new(GamePlacementStatus.Placed, publicInfo, resolutionInfo, ErrorMessage: null);
        }
    }

    public GameUndoResult UndoLastPlacement(long requestingUserId)
    {
        lock (_sync)
        {
            if (!TryGetSessionByUserId(requestingUserId, out var session))
                return new(GameUndoStatus.NoActiveSession, PublicInfo: null, ErrorMessage: null);

            if (!session.TryGetSeat(requestingUserId, out var seat, out var player))
                return new(GameUndoStatus.NotSeated, PublicInfo: null, ErrorMessage: null);

            if (session.TurnState is null)
                return new(GameUndoStatus.RoundNotRolled, PublicInfo: null, ErrorMessage: null);

            if (!session.TurnState.CanUndoLastPlacement(seat))
                return new(GameUndoStatus.UndoNotAllowed, PublicInfo: null, ErrorMessage: null);

            var last = session.TurnState.Placements[^1];

            try
            {
                var logged = session.RemoveLastLoggedPlacement();

                session.TurnState = session.TurnState.UndoLastPlacement(seat);
                session.RebuildDomainGameFromLogs();

                var publicInfo = new GameUndoPublicInfo(
                    session.GroupChatId,
                    player!,
                    seat,
                    last.Index,
                    last.Value,
                    logged.CommandId,
                    logged.CommandDisplayName,
                    NextPlayer: session.TurnState.CurrentPlayer,
                    PlacementsRemaining: session.TurnState.PlacementsRemaining);

                return new(GameUndoStatus.Undone, publicInfo, ErrorMessage: null);
            }
            catch (Exception exception)
            {
                return new(GameUndoStatus.DomainError, PublicInfo: null, ErrorMessage: exception.Message);
            }
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
        private sealed record LoggedPlacement(string CommandId, string CommandDisplayName);

        private sealed class RoundLog
        {
            public RoundLog(int roundNumber, IReadOnlyList<int> pilotDice, IReadOnlyList<int> copilotDice)
            {
                RoundNumber = roundNumber;
                PilotDice = pilotDice.ToArray();
                CopilotDice = copilotDice.ToArray();
            }

            public int RoundNumber { get; }
            public int[] PilotDice { get; }
            public int[] CopilotDice { get; }
            public List<LoggedPlacement> Placements { get; } = [];
            public bool IsCompleted { get; set; }
        }

        private Airport _airport = null!;
        private Altitude _altitude = null!;

        private AxisPositionModule _axis = null!;
        private EnginesModule _engines = null!;
        private BrakesModule _brakes = null!;
        private FlapsModule _flaps = null!;
        private LandingGearModule _landingGear = null!;
        private RadioModule _radio = null!;
        private ConcentrationModule _concentration = null!;

        private readonly List<RoundLog> _roundLogs = [];

        public GameSession(long groupChatId, LobbyPlayer pilot, LobbyPlayer copilot)
        {
            GroupChatId = groupChatId;
            Pilot = pilot;
            Copilot = copilot;

            InitializeNewDomainGame();
        }

        public long GroupChatId { get; }
        public LobbyPlayer Pilot { get; }
        public LobbyPlayer Copilot { get; }

        public Game DomainGame { get; private set; } = null!;

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
            ArgumentNullException.ThrowIfNull(roll);

            DomainGame.SetRoundDice(roll.PilotDice, roll.CopilotDice);
            _roundLogs.Add(new(Round.RoundNumber, roll.PilotDice, roll.CopilotDice));

            var actualStartingPlayer = ToSeat(DomainGame.CurrentPlayer);

            TurnState = RoundTurnState.StartNew(
                Round.RoundNumber,
                actualStartingPlayer,
                SecretDiceHand.Create(roll.PilotDice),
                SecretDiceHand.Create(roll.CopilotDice));

            Round = Round with { Status = GameRoundStatus.AwaitingPlacements };

            return actualStartingPlayer;
        }

        public void LogPlacement(string commandId, string commandDisplayName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
            ArgumentException.ThrowIfNullOrWhiteSpace(commandDisplayName);

            if (_roundLogs.Count == 0)
                throw new InvalidOperationException("Cannot log a placement before the round has been rolled.");

            _roundLogs[^1].Placements.Add(new(commandId, commandDisplayName));
        }

        public (string CommandId, string CommandDisplayName) RemoveLastLoggedPlacement()
        {
            if (_roundLogs.Count == 0)
                throw new InvalidOperationException("Cannot remove placements before the round has been rolled.");

            var placements = _roundLogs[^1].Placements;
            if (placements.Count == 0)
                throw new InvalidOperationException("Cannot remove a placement when no placements have been logged.");

            var lastIndex = placements.Count - 1;
            var last = placements[lastIndex];
            placements.RemoveAt(lastIndex);

            return (last.CommandId, last.CommandDisplayName);
        }

        public void RebuildDomainGameFromLogs()
        {
            InitializeNewDomainGame();

            foreach (var round in _roundLogs)
            {
                DomainGame.SetRoundDice(round.PilotDice, round.CopilotDice);

                foreach (var placement in round.Placements)
                    ExecutePlacementCommand(placement.CommandId);

                if (round.IsCompleted && DomainGame.Status == GameStatus.InProgress)
                    DomainGame.ExecuteCommand("NextRound");
            }
        }

        private void ExecutePlacementCommand(string commandId)
        {
            try
            {
                DomainGame.ExecuteCommand(commandId);
            }
            catch (InvalidOperationException) when (DomainGame.Status == GameStatus.Lost)
            {
                // Replaying a placement that caused a loss should still restore the terminal state.
            }
        }

        public GameRoundResolutionPublicInfo ResolveRoundAndAdvance()
        {
            var resolvedRoundNumber = Round.RoundNumber;
            var resolvedState = CreateStateSnapshot();
            var gameStatus = DomainGame.Status.ToString();

            int? nextRoundNumber = null;
            PlayerSeat? nextStartingPlayer = null;

            if (_roundLogs.Count > 0)
                _roundLogs[^1].IsCompleted = true;

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

            return new(
                GroupChatId,
                resolvedRoundNumber,
                resolvedState,
                gameStatus,
                nextRoundNumber,
                nextStartingPlayer);
        }

        public GameStatePublicSnapshot CreateStateSnapshot()
        {
            var totalPlanes = _airport.PathSegments.Sum(segment => segment.PlaneTokens);

            return new(
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

        private void InitializeNewDomainGame()
        {
            _airport = (Airport)new MontrealAirport();
            _altitude = new();

            _axis = new();
            _engines = new(_airport);
            _brakes = new();
            _flaps = new(_airport);
            _landingGear = new(_airport);
            _radio = new(_airport);
            _concentration = new();

            DomainGame = new(
                _airport,
                _altitude,
                [_axis, _engines, _brakes, _flaps, _landingGear, _radio, _concentration]);
        }

        private static PlayerSeat ToSeat(Player player) => player == Player.Pilot
            ? PlayerSeat.Pilot
            : PlayerSeat.Copilot;
    }
}
