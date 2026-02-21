namespace SkyTeam.Application.Tests.GameSessions;

using FluentAssertions;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;

public sealed class InMemoryGroupGameSessionStoreTests
{
    private const long GroupChatId = 123;

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

        var pilot = new LobbyPlayer(1, "Pilot");
        var copilot = new LobbyPlayer(2, "Copilot");
        var lobby = new LobbySnapshot(GroupChatId, pilot, copilot);

        store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);

        // Act
        var result = store.Start(GroupChatId, lobby, requestingUserId: pilot.UserId);

        // Assert
        result.Status.Should().Be(GameSessionStartStatus.AlreadyStarted);
        result.Snapshot.Should().NotBeNull();
        result.Snapshot!.Round.RoundNumber.Should().Be(1);
        result.Snapshot.Round.Status.Should().Be(GameRoundStatus.AwaitingRoll);
    }
}
