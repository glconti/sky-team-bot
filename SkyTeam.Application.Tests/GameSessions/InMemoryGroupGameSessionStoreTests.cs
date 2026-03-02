namespace SkyTeam.Application.Tests.GameSessions;

using FluentAssertions;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;

public sealed class InMemoryGroupGameSessionStoreTests
{
    private const long GroupChatId = 123;

    private static (LobbyPlayer Pilot, LobbyPlayer Copilot, LobbySnapshot Lobby) CreateReadyLobby()
    {
        var pilot = new LobbyPlayer(1, "Pilot");
        var copilot = new LobbyPlayer(2, "Copilot");
        return (pilot, copilot, new(GroupChatId, pilot, copilot));
    }

    private static string GetCommandIdForDie(InMemoryGroupGameSessionStore store, long userId, int dieIndex)
    {
        var hand = store.GetHand(userId);
        hand.Status.Should().Be(GameHandStatus.Ok);
        hand.Hand.Should().NotBeNull();
        hand.AvailableCommands.Should().NotBeNull();

        var rolledValue = hand.Hand!.Dice.Single(d => d.Index == dieIndex).Value.Value;
        var command = hand.AvailableCommands!.First(c => c.CommandId.Contains($":{rolledValue}"));
        return command.CommandId;
    }

    [Fact]
    public void Start_ShouldReturnNoLobby_WhenLobbySnapshotIsNull()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        // Act
        var result = store.Start(GroupChatId, lobbySnapshot: null, requestingUserId: 1);

        // Assert
        result.Should().Be(new GameSessionStartResult(GameSessionStartStatus.NoLobby, Snapshot: null));
    }

    [Fact]
    public void Start_ShouldReturnLobbyNotReady_WhenLobbyIsNotReady()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var lobby = new LobbySnapshot(GroupChatId, Pilot: new(1, "Pilot"), Copilot: null);

        // Act
        var result = store.Start(GroupChatId, lobby, requestingUserId: 1);

        // Assert
        result.Should().Be(new GameSessionStartResult(GameSessionStartStatus.LobbyNotReady, Snapshot: null));
    }

    [Fact]
    public void Start_ShouldReturnNotSeated_WhenRequestingUserIsNotPilotOrCopilot()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        var lobby = new LobbySnapshot(
            GroupChatId,
            Pilot: new(1, "Pilot"),
            Copilot: new(2, "Copilot"));

        // Act
        var result = store.Start(GroupChatId, lobby, requestingUserId: 99);

        // Assert
        result.Should().Be(new GameSessionStartResult(GameSessionStartStatus.NotSeated, Snapshot: null));
    }

    [Fact]
    public void Start_ShouldReturnStartedAndInitializeRound_WhenLobbyIsReadyAndRequestingUserIsSeated()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        var pilot = new LobbyPlayer(1, "Pilot");
        var copilot = new LobbyPlayer(2, "Copilot");
        var lobby = new LobbySnapshot(GroupChatId, pilot, copilot);

        // Act
        var result = store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);

        // Assert
        result.Status.Should().Be(GameSessionStartStatus.Started);
        result.Snapshot.Should().Be(new GameSessionSnapshot(
            GroupChatId,
            pilot,
            copilot,
            new(RoundNumber: 1, GameRoundStatus.AwaitingRoll)));
    }

    [Fact]
    public void Start_ShouldReturnAlreadyStarted_WhenGameIsAlreadyStarted()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        var (pilot, _, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);

        // Act
        var result = store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);

        // Assert
        result.Status.Should().Be(GameSessionStartStatus.AlreadyStarted);
        result.Snapshot.Should().NotBeNull();
        result.Snapshot!.Round.RoundNumber.Should().Be(1);
        result.Snapshot.Round.Status.Should().Be(GameRoundStatus.AwaitingRoll);
    }

    [Fact]
    public void CockpitMessageId_ShouldBePersistedPerGroupSession()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        const long firstGroupChatId = 123;
        const long secondGroupChatId = 456;

        // Act
        store.SetCockpitMessageId(firstGroupChatId, cockpitMessageId: 11);
        store.SetCockpitMessageId(secondGroupChatId, cockpitMessageId: 22);

        var hasFirst = store.TryGetCockpitMessageId(firstGroupChatId, out var firstCockpitMessageId);
        var hasSecond = store.TryGetCockpitMessageId(secondGroupChatId, out var secondCockpitMessageId);

        // Assert
        hasFirst.Should().BeTrue();
        firstCockpitMessageId.Should().Be(11);
        hasSecond.Should().BeTrue();
        secondCockpitMessageId.Should().Be(22);
    }

    [Fact]
    public void CockpitMessageId_ShouldKeepSingleLatestValue_WhenRecreated()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        // Act
        store.SetCockpitMessageId(GroupChatId, cockpitMessageId: 100);
        store.SetCockpitMessageId(GroupChatId, cockpitMessageId: 200);

        var found = store.TryGetCockpitMessageId(GroupChatId, out var cockpitMessageId);

        // Assert
        found.Should().BeTrue();
        cockpitMessageId.Should().Be(200);
    }

    [Fact]
    public void PlaceDie_ShouldNotAdvanceRound_WhenFewerThanEightPlacementsAreMade()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        static string GetCommandIdForDie(InMemoryGroupGameSessionStore store, long userId, int dieIndex)
        {
            var hand = store.GetHand(userId);
            hand.Status.Should().Be(GameHandStatus.Ok);
            hand.Hand.Should().NotBeNull();
            hand.AvailableCommands.Should().NotBeNull();

            var rolledValue = hand.Hand!.Dice.Single(d => d.Index == dieIndex).Value.Value;
            var command = hand.AvailableCommands!.First(c => c.CommandId.Contains($":{rolledValue}"));
            return command.CommandId;
        }

        var firstUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var secondUser = roll.StartingPlayer == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;
        var expectedNextPlayer = roll.StartingPlayer == PlayerSeat.Pilot ? PlayerSeat.Copilot : PlayerSeat.Pilot;

        for (var dieIndex = 0; dieIndex < 3; dieIndex++)
        {
            store.PlaceDie(firstUser, dieIndex, GetCommandIdForDie(store, firstUser, dieIndex));
            store.PlaceDie(secondUser, dieIndex, GetCommandIdForDie(store, secondUser, dieIndex));
        }

        store.PlaceDie(firstUser, dieIndex: 3, GetCommandIdForDie(store, firstUser, dieIndex: 3));

        // Act
        var snapshot = store.GetSnapshot(GroupChatId);
        var hand = store.GetHand(secondUser);

        // Assert
        snapshot.Should().NotBeNull();
        snapshot!.Round.Should().Be(new GameRoundSnapshot(RoundNumber: 1, GameRoundStatus.AwaitingPlacements));

        hand.Status.Should().Be(GameHandStatus.Ok);
        hand.CurrentPlayer.Should().Be(expectedNextPlayer);
        hand.PlacementsRemaining.Should().Be(1);
    }

    [Fact]
    public void GetHand_ShouldReturnNoCommands_WhenRequestingPlayerIsNotCurrentPlayer()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        var nonCurrentUser = roll.StartingPlayer == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;

        // Act
        var hand = store.GetHand(nonCurrentUser);

        // Assert
        hand.Status.Should().Be(GameHandStatus.Ok);
        hand.AvailableCommands.Should().BeEmpty();
    }

    [Fact]
    public void PlaceDie_ShouldReturnNotPlayersTurn_WhenRequestingPlayerIsNotCurrentPlayer()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        var nonCurrentUser = roll.StartingPlayer == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;

        // Act
        var result = store.PlaceDie(nonCurrentUser, dieIndex: 0, commandId: "Axis.AssignBlue:1");

        // Assert
        result.Status.Should().Be(GamePlacementStatus.NotPlayersTurn);
    }

    [Fact]
    public void PlaceDie_ShouldBindToRequestedGroupChat_WhenUserHasMultipleActiveSessions()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        const long firstGroupChatId = 123;
        const long secondGroupChatId = 456;
        var sharedPilot = new LobbyPlayer(1, "Pilot");
        var firstCopilot = new LobbyPlayer(2, "Copilot A");
        var secondCopilot = new LobbyPlayer(3, "Copilot B");

        store.Start(firstGroupChatId, new LobbySnapshot(firstGroupChatId, sharedPilot, firstCopilot), requestingUserId: sharedPilot.UserId);
        store.RegisterRoll(firstGroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        var commandId = GetCommandIdForDie(store, sharedPilot.UserId, dieIndex: 0);

        store.Start(secondGroupChatId, new LobbySnapshot(secondGroupChatId, sharedPilot, secondCopilot), requestingUserId: sharedPilot.UserId);
        store.RegisterRoll(secondGroupChatId, new([6, 6, 6, 6], [6, 6, 6, 6]));

        // Act
        var result = store.PlaceDie(firstGroupChatId, sharedPilot.UserId, dieIndex: 0, commandId);
        var firstState = store.GetPublicState(firstGroupChatId);
        var secondState = store.GetPublicState(secondGroupChatId);

        // Assert
        result.Status.Should().Be(GamePlacementStatus.Placed);
        firstState!.PlacementsMade.Should().Be(1);
        secondState!.PlacementsMade.Should().Be(0);
    }

    [Fact]
    public void PlaceDie_ShouldRejectCrossChatMutation_WhenUserIsNotSeatedInRequestedSession()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        const long firstGroupChatId = 123;
        const long secondGroupChatId = 456;
        var firstPilot = new LobbyPlayer(1, "Pilot");
        var firstCopilot = new LobbyPlayer(2, "Copilot A");
        var secondPilot = new LobbyPlayer(3, "Pilot B");
        var secondCopilot = new LobbyPlayer(4, "Copilot B");

        store.Start(firstGroupChatId, new LobbySnapshot(firstGroupChatId, firstPilot, firstCopilot), requestingUserId: firstPilot.UserId);
        store.RegisterRoll(firstGroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        store.Start(secondGroupChatId, new LobbySnapshot(secondGroupChatId, secondPilot, secondCopilot), requestingUserId: secondPilot.UserId);
        store.RegisterRoll(secondGroupChatId, new([6, 6, 6, 6], [6, 6, 6, 6]));
        var secondCommand = GetCommandIdForDie(store, secondPilot.UserId, dieIndex: 0);

        // Act
        var result = store.PlaceDie(secondGroupChatId, firstPilot.UserId, dieIndex: 0, secondCommand);

        // Assert
        result.Status.Should().Be(GamePlacementStatus.NotSeated);
    }

    [Fact]
    public void PlaceDie_ShouldMarkOnlyRequestingPlayersDieUsed_WhenPlacementIsAccepted()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        static string GetCommandIdForDie(InMemoryGroupGameSessionStore store, long userId, int dieIndex)
        {
            var hand = store.GetHand(userId);
            hand.Status.Should().Be(GameHandStatus.Ok);
            hand.Hand.Should().NotBeNull();
            hand.AvailableCommands.Should().NotBeNull();

            var rolledValue = hand.Hand!.Dice.Single(d => d.Index == dieIndex).Value.Value;
            var command = hand.AvailableCommands!.First(c => c.CommandId.Contains($":{rolledValue}"));
            return command.CommandId;
        }

        var currentUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var otherUser = roll.StartingPlayer == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;

        // Act
        store.PlaceDie(currentUser, dieIndex: 0, GetCommandIdForDie(store, currentUser, dieIndex: 0));

        var currentHand = store.GetHand(currentUser);
        var otherHand = store.GetHand(otherUser);

        // Assert
        currentHand.Hand!.Dice[0].IsUsed.Should().BeTrue();
        otherHand.Hand!.Dice[0].IsUsed.Should().BeFalse();
    }

    [Fact]
    public void PlaceDie_ShouldNotConsumeDieOrAdvanceTurn_WhenCommandDoesNotMatchDie()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        var startingPlayerSeat = roll.StartingPlayer!.Value;
        var startingPlayerUserId = startingPlayerSeat == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;

        var handBefore = store.GetHand(startingPlayerUserId);
        handBefore.Status.Should().Be(GameHandStatus.Ok);

        const int dieIndex = 0;

        // Act
        var result = store.PlaceDie(startingPlayerUserId, dieIndex, commandId: "Axis.AssignBlue:999");

        var handAfter = store.GetHand(startingPlayerUserId);

        // Assert
        new
        {
            result.Status,
            DieUsed = handAfter.Hand!.Dice[dieIndex].IsUsed,
            CurrentPlayer = handAfter.CurrentPlayer!.Value
        }
        .Should()
        .BeEquivalentTo(new
        {
            Status = GamePlacementStatus.CommandDoesNotMatchDie,
            DieUsed = false,
            CurrentPlayer = startingPlayerSeat
        });
    }

    [Fact]
    public void PlaceDie_ShouldResolveAndAdvanceToNextRound_WhenEighthPlacementIsMade()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        static string GetCommandIdForDie(InMemoryGroupGameSessionStore store, long userId, int dieIndex)
        {
            var hand = store.GetHand(userId);
            hand.Status.Should().Be(GameHandStatus.Ok);
            hand.Hand.Should().NotBeNull();
            hand.AvailableCommands.Should().NotBeNull();

            var rolledValue = hand.Hand!.Dice.Single(d => d.Index == dieIndex).Value.Value;
            var command = hand.AvailableCommands!.First(c => c.CommandId.Contains($":{rolledValue}"));
            return command.CommandId;
        }

        var firstUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var secondUser = roll.StartingPlayer == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;

        // Act
        for (var dieIndex = 0; dieIndex < 4; dieIndex++)
        {
            store.PlaceDie(firstUser, dieIndex, GetCommandIdForDie(store, firstUser, dieIndex));
            store.PlaceDie(secondUser, dieIndex, GetCommandIdForDie(store, secondUser, dieIndex));
        }

        var snapshot = store.GetSnapshot(GroupChatId);
        var pilotHand = store.GetHand(pilot.UserId);
        var placeAfterResolve = store.PlaceDie(pilot.UserId, dieIndex: 0, commandId: "Axis.AssignBlue:1");

        // Assert
        snapshot.Should().NotBeNull();
        snapshot!.Round.Should().Be(new GameRoundSnapshot(RoundNumber: 2, GameRoundStatus.AwaitingRoll));

        pilotHand.Status.Should().Be(GameHandStatus.RoundNotRolled);
        placeAfterResolve.Status.Should().Be(GamePlacementStatus.RoundNotRolled);
    }

    [Fact]
    public void UndoLastPlacement_ShouldUndoAndRestoreTurn_WhenOpponentHasNotPlayedYet()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        var lastPlacerSeat = roll.StartingPlayer!.Value;
        var lastPlacerUserId = lastPlacerSeat == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var opponentUserId = lastPlacerSeat == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;

        const int dieIndex = 0;

        store.PlaceDie(lastPlacerUserId, dieIndex, GetCommandIdForDie(store, lastPlacerUserId, dieIndex));
        var placementsRemainingAfterPlacement = store.GetHand(opponentUserId).PlacementsRemaining!.Value;

        // Act
        var result = store.UndoLastPlacement(lastPlacerUserId);

        var stateAfterUndo = store.GetPublicState(GroupChatId)!;
        var lastPlacerHandAfterUndo = store.GetHand(lastPlacerUserId);

        // Assert
        new
        {
            result.Status,
            CurrentPlayer = stateAfterUndo.CurrentPlayer!.Value,
            PlacementsRemainingAfterUndo = stateAfterUndo.PlacementsRemaining!.Value,
            DieUsedAfterUndo = lastPlacerHandAfterUndo.Hand!.Dice[dieIndex].IsUsed
        }
        .Should()
        .BeEquivalentTo(new
        {
            Status = GameUndoStatus.Undone,
            CurrentPlayer = lastPlacerSeat,
            PlacementsRemainingAfterUndo = placementsRemainingAfterPlacement + 1,
            DieUsedAfterUndo = false
        });
    }

    [Fact]
    public void UndoLastPlacement_ShouldReturnUndoNotAllowed_WhenOpponentHasAlreadyPlayed()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        roll.StartingPlayer.Should().NotBeNull();

        var firstPlayerSeat = roll.StartingPlayer!.Value;
        var firstPlayerUserId = firstPlayerSeat == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var secondPlayerUserId = firstPlayerSeat == PlayerSeat.Pilot ? copilot.UserId : pilot.UserId;

        store.PlaceDie(firstPlayerUserId, dieIndex: 0, GetCommandIdForDie(store, firstPlayerUserId, dieIndex: 0));
        store.PlaceDie(secondPlayerUserId, dieIndex: 0, GetCommandIdForDie(store, secondPlayerUserId, dieIndex: 0));

        var expectedState = store.GetPublicState(GroupChatId)!;

        // Act
        var result = store.UndoLastPlacement(firstPlayerUserId);

        var actualState = store.GetPublicState(GroupChatId)!;

        // Assert
        new
        {
            result.Status,
            CurrentPlayer = actualState.CurrentPlayer!.Value,
            PlacementsRemaining = actualState.PlacementsRemaining!.Value
        }
        .Should()
        .BeEquivalentTo(new
        {
            Status = GameUndoStatus.UndoNotAllowed,
            CurrentPlayer = expectedState.CurrentPlayer!.Value,
            PlacementsRemaining = expectedState.PlacementsRemaining!.Value
        });
    }

    [Fact]
    public void UndoLastPlacement_ShouldReturnRoundNotRolled_WhenRoundHasNotBeenRolled()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, _, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);

        // Act
        var result = store.UndoLastPlacement(pilot.UserId);

        // Assert
        result.Should().Be(new GameUndoResult(GameUndoStatus.RoundNotRolled, PublicInfo: null, ErrorMessage: null));
    }

    [Fact]
    public void OpenLobbyStartRollPlaceUndo_ShouldKeepRoundInProgress_WhenUndoingFirstPlacement()
    {
        // Arrange
        var lobbyStore = new InMemoryGroupLobbyStore();
        var gameStore = new InMemoryGroupGameSessionStore();

        var createLobby = lobbyStore.CreateNew(GroupChatId);
        var pilot = new LobbyPlayer(1, "Pilot");
        var copilot = new LobbyPlayer(2, "Copilot");
        var joinPilot = lobbyStore.Join(GroupChatId, pilot);
        var joinCopilot = lobbyStore.Join(GroupChatId, copilot);

        var start = gameStore.Start(GroupChatId, lobbyStore.GetSnapshot(GroupChatId), requestingUserId: pilot.UserId);
        var roll = gameStore.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        var firstUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var commandId = GetCommandIdForDie(gameStore, firstUser, dieIndex: 0);

        // Act
        var place = gameStore.PlaceDie(firstUser, dieIndex: 0, commandId);
        var undo = gameStore.UndoLastPlacement(firstUser);

        var state = gameStore.GetPublicState(GroupChatId);
        var hand = gameStore.GetHand(firstUser);

        // Assert
        createLobby.Status.Should().Be(LobbyCreateStatus.Created);
        joinPilot.Status.Should().Be(LobbyJoinStatus.JoinedAsPilot);
        joinCopilot.Status.Should().Be(LobbyJoinStatus.JoinedAsCopilot);
        start.Status.Should().Be(GameSessionStartStatus.Started);
        roll.Status.Should().Be(GameSessionRollStatus.Rolled);
        place.Status.Should().Be(GamePlacementStatus.Placed);
        undo.Status.Should().Be(GameUndoStatus.Undone);
        state!.Session.Round.Status.Should().Be(GameRoundStatus.AwaitingPlacements);
        state.PlacementsMade.Should().Be(0);
        hand.Hand!.Dice[0].IsUsed.Should().BeFalse();
    }

    [Fact]
    public void PlaceDie_ShouldReturnNotPlayersTurn_WhenPlacementIsReplayedBySamePlayer()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();
        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        var currentUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var commandId = GetCommandIdForDie(store, currentUser, dieIndex: 0);

        store.PlaceDie(currentUser, dieIndex: 0, commandId);

        // Act
        var replay = store.PlaceDie(currentUser, dieIndex: 0, commandId);
        var state = store.GetPublicState(GroupChatId);

        // Assert
        replay.Status.Should().Be(GamePlacementStatus.NotPlayersTurn);
        state!.PlacementsMade.Should().Be(1);
    }

    [Fact]
    public void PlaceDie_ShouldBeIdempotent_WhenIdempotencyKeyIsReused()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();
        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        var currentUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var commandId = GetCommandIdForDie(store, currentUser, dieIndex: 0);
        const string idempotencyKey = "place-1";

        // Act
        var first = store.PlaceDie(currentUser, dieIndex: 0, commandId, idempotencyKey);
        var replay = store.PlaceDie(currentUser, dieIndex: 0, commandId, idempotencyKey);
        var state = store.GetPublicState(GroupChatId);

        // Assert
        first.Status.Should().Be(GamePlacementStatus.Placed);
        replay.Should().Be(first);
        state!.PlacementsMade.Should().Be(1);
    }

    [Fact]
    public void RegisterRoll_ShouldReturnRoundNotAwaitingRoll_WhenRollIsReplayed()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, _, lobby) = CreateReadyLobby();
        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        // Act
        var replayedRoll = store.RegisterRoll(GroupChatId, new([6, 6, 6, 6], [6, 6, 6, 6]));

        // Assert
        replayedRoll.Status.Should().Be(GameSessionRollStatus.RoundNotAwaitingRoll);
        replayedRoll.Snapshot!.Round.Status.Should().Be(GameRoundStatus.AwaitingPlacements);
    }

    [Fact]
    public void GetHand_ShouldReturnNoActiveSession_WhenUserIsNotSeatedInAnyGame()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, _, lobby) = CreateReadyLobby();
        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        // Act
        var spectatorHand = store.GetHand(requestingUserId: 999);

        // Assert
        spectatorHand.Status.Should().Be(GameHandStatus.NoActiveSession);
    }

    [Fact]
    public async Task PlaceDie_ShouldAcceptSinglePlacement_WhenConcurrentSubmissionsTargetSameDie()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();
        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        var currentUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var commandId = GetCommandIdForDie(store, currentUser, dieIndex: 0);

        using var gate = new ManualResetEventSlim(false);
        var firstRequest = Task.Run(() =>
        {
            gate.Wait();
            return store.PlaceDie(currentUser, dieIndex: 0, commandId);
        });

        var secondRequest = Task.Run(() =>
        {
            gate.Wait();
            return store.PlaceDie(currentUser, dieIndex: 0, commandId);
        });

        // Act
        gate.Set();
        var results = await Task.WhenAll(firstRequest, secondRequest);
        var state = store.GetPublicState(GroupChatId);

        // Assert
        new
        {
            Statuses = results.Select(result => result.Status).OrderBy(status => status.ToString()).ToArray(),
            PlacementsMade = state!.PlacementsMade
        }
        .Should()
        .BeEquivalentTo(new
        {
            Statuses = (new[] { GamePlacementStatus.NotPlayersTurn, GamePlacementStatus.Placed }).OrderBy(status => status.ToString()).ToArray(),
            PlacementsMade = 1
        });
    }

    [Fact]
    public void PersistenceRoundTrip_ShouldRestoreSessionState_WhenStoreIsRehydrated()
    {
        // Arrange
        var persistence = new InMemoryGameSessionPersistence();
        var store = new InMemoryGroupGameSessionStore(persistence);
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        store.SetCockpitMessageId(GroupChatId, cockpitMessageId: 77);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        var currentUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var commandId = GetCommandIdForDie(store, currentUser, dieIndex: 0);
        store.PlaceDie(currentUser, dieIndex: 0, commandId).Status.Should().Be(GamePlacementStatus.Placed);

        // Act
        var restored = new InMemoryGroupGameSessionStore(persistence);
        var restoredState = restored.GetPublicState(GroupChatId);
        var restoredHand = restored.GetHand(currentUser);
        var hasCockpit = restored.TryGetCockpitMessageId(GroupChatId, out var cockpitMessageId);

        // Assert
        restoredState.Should().NotBeNull();
        restoredState!.Session.Round.Status.Should().Be(GameRoundStatus.AwaitingPlacements);
        restoredState.PlacementsMade.Should().Be(1);

        restoredHand.Status.Should().Be(GameHandStatus.Ok);
        restoredHand.Hand!.Dice[0].IsUsed.Should().BeTrue();

        hasCockpit.Should().BeTrue();
        cockpitMessageId.Should().Be(77);

        persistence.State.Sessions.Single().Version.Should().Be(3);
    }

    [Fact]
    public void Update_ShouldReturnVersionConflict_WhenExpectedVersionIsOutdated()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();
        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new([1, 2, 3, 4], [1, 2, 3, 4]));

        var staleVersion = store.GetSnapshot(GroupChatId)!.Version;
        var currentUser = roll.StartingPlayer == PlayerSeat.Pilot ? pilot.UserId : copilot.UserId;
        var nextUser = currentUser == pilot.UserId ? copilot.UserId : pilot.UserId;

        var currentCommandId = GetCommandIdForDie(store, currentUser, dieIndex: 0);
        store.PlaceDie(GroupChatId, currentUser, dieIndex: 0, currentCommandId, staleVersion).Status.Should().Be(GamePlacementStatus.Placed);

        var nextCommandId = GetCommandIdForDie(store, nextUser, dieIndex: 0);

        // Act
        var staleUpdate = store.PlaceDie(GroupChatId, nextUser, dieIndex: 0, nextCommandId, staleVersion);
        var state = store.GetPublicState(GroupChatId);

        // Assert
        staleUpdate.Status.Should().Be(GamePlacementStatus.VersionConflict);
        staleUpdate.CurrentVersion.Should().Be(state!.Session.Version);
        state.PlacementsMade.Should().Be(1);
    }

    [Fact(Skip = "Auth expiry UX is implemented in Telegram/WebApp transport layer, not in application store.")]
    public void AuthExpiryUx_ShouldPromptReopenFlow_WhenSessionTokenIsExpired()
    {
    }

    private sealed class InMemoryGameSessionPersistence : IGameSessionPersistence
    {
        public PersistedGameSessionStoreState State { get; private set; } = PersistedGameSessionStoreState.Empty;

        public PersistedGameSessionStoreState Load() => State;

        public void Save(PersistedGameSessionStoreState state) => State = state;
    }
}
