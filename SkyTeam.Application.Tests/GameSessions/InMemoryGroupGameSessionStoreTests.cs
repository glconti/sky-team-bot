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
        return (pilot, copilot, new LobbySnapshot(GroupChatId, pilot, copilot));
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
        var lobby = new LobbySnapshot(GroupChatId, Pilot: new LobbyPlayer(1, "Pilot"), Copilot: null);

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
            Pilot: new LobbyPlayer(1, "Pilot"),
            Copilot: new LobbyPlayer(2, "Copilot"));

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
            new GameRoundSnapshot(RoundNumber: 1, GameRoundStatus.AwaitingRoll)));
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
        var roll = store.RegisterRoll(GroupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));
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
    public void PlaceDie_ShouldResolveAndAdvanceToNextRound_WhenEighthPlacementIsMade()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        var roll = store.RegisterRoll(GroupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));
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
}
