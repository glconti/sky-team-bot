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
        store.RegisterRoll(GroupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));

        store.PlaceDie(pilot.UserId, dieIndex: 0, target: "Axis");
        store.PlaceDie(copilot.UserId, dieIndex: 0, target: "Axis");
        store.PlaceDie(pilot.UserId, dieIndex: 1, target: "Axis");
        store.PlaceDie(copilot.UserId, dieIndex: 1, target: "Axis");
        store.PlaceDie(pilot.UserId, dieIndex: 2, target: "Axis");
        store.PlaceDie(copilot.UserId, dieIndex: 2, target: "Axis");
        store.PlaceDie(pilot.UserId, dieIndex: 3, target: "Axis");

        // Act
        var snapshot = store.GetSnapshot(GroupChatId);
        var hand = store.GetHand(copilot.UserId);

        // Assert
        snapshot.Should().NotBeNull();
        snapshot!.Round.Should().Be(new GameRoundSnapshot(RoundNumber: 1, GameRoundStatus.AwaitingPlacements));

        hand.Status.Should().Be(GameHandStatus.Ok);
        hand.CurrentPlayer.Should().Be(PlayerSeat.Copilot);
        hand.PlacementsRemaining.Should().Be(1);
    }

    [Fact]
    public void PlaceDie_ShouldResolveAndAdvanceToNextRound_WhenEighthPlacementIsMade()
    {
        // Arrange
        var store = new InMemoryGroupGameSessionStore();
        var (pilot, copilot, lobby) = CreateReadyLobby();

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);
        store.RegisterRoll(GroupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));

        // Act
        for (var dieIndex = 0; dieIndex < 4; dieIndex++)
        {
            store.PlaceDie(pilot.UserId, dieIndex, target: "Axis");
            store.PlaceDie(copilot.UserId, dieIndex, target: "Axis");
        }

        var snapshot = store.GetSnapshot(GroupChatId);
        var pilotHand = store.GetHand(pilot.UserId);
        var placeAfterResolve = store.PlaceDie(pilot.UserId, dieIndex: 0, target: "Axis");

        // Assert
        snapshot.Should().NotBeNull();
        snapshot!.Round.Should().Be(new GameRoundSnapshot(RoundNumber: 2, GameRoundStatus.AwaitingRoll));

        pilotHand.Status.Should().Be(GameHandStatus.RoundNotRolled);
        placeAfterResolve.Status.Should().Be(GamePlacementStatus.RoundNotRolled);
    }
}
