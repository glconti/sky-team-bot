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
    public void CockpitMessageId_ShouldBePersistedPerGroupSession()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        // Act
        store.SetCockpitMessageId(GroupChatId, cockpitMessageId: 777);
        var found = store.TryGetCockpitMessageId(GroupChatId, out var cockpitMessageId);

        // Assert
        found.Should().BeTrue();
        cockpitMessageId.Should().Be(777);
    }

    [Fact]
    public void CockpitMessageId_ShouldKeepSingleLatestValue_WhenRecreated()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();

        // Act
        store.SetCockpitMessageId(GroupChatId, cockpitMessageId: 111);
        store.SetCockpitMessageId(GroupChatId, cockpitMessageId: 222);
        var found = store.TryGetCockpitMessageId(GroupChatId, out var cockpitMessageId);

        // Assert
        found.Should().BeTrue();
        cockpitMessageId.Should().Be(222);
    }
}
