namespace SkyTeam.Application.Tests.Lobby;

using FluentAssertions;
using SkyTeam.Application.Lobby;

public sealed class InMemoryGroupLobbyStoreTests
{
    private const long GroupChatId = 123;

    [Fact]
    public void CreateNew_ShouldReturnCreatedSnapshot_WhenLobbyDoesNotExist()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();

        // Act
        var result = store.CreateNew(GroupChatId);

        // Assert
        result.Should().BeEquivalentTo(new LobbyCreateResult(
            LobbyCreateStatus.Created,
            new LobbySnapshot(GroupChatId, Pilot: null, Copilot: null)));
    }

    [Fact]
    public void CreateNew_ShouldReturnAlreadyExistsSnapshot_WhenLobbyAlreadyExists()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        store.CreateNew(GroupChatId);

        var pilot = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");
        store.Join(GroupChatId, pilot);

        // Act
        var result = store.CreateNew(GroupChatId);

        // Assert
        result.Should().BeEquivalentTo(new LobbyCreateResult(
            LobbyCreateStatus.AlreadyExists,
            new LobbySnapshot(GroupChatId, Pilot: pilot, Copilot: null)));
    }

    [Fact]
    public void Join_ShouldJoinAsPilot_WhenPilotSeatIsEmpty()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        store.CreateNew(GroupChatId);

        var player = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");

        // Act
        var result = store.Join(GroupChatId, player);

        // Assert
        result.Should().BeEquivalentTo(new LobbyJoinResult(
            LobbyJoinStatus.JoinedAsPilot,
            new LobbySnapshot(GroupChatId, Pilot: player, Copilot: null)));
    }

    [Fact]
    public void Join_ShouldJoinAsCopilot_WhenPilotSeatIsTakenAndCopilotSeatIsEmpty()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        store.CreateNew(GroupChatId);

        var pilot = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");
        store.Join(GroupChatId, pilot);

        var copilot = new LobbyPlayer(UserId: 2, DisplayName: "Copilot");

        // Act
        var result = store.Join(GroupChatId, copilot);

        // Assert
        result.Should().BeEquivalentTo(new LobbyJoinResult(
            LobbyJoinStatus.JoinedAsCopilot,
            new LobbySnapshot(GroupChatId, Pilot: pilot, Copilot: copilot)));
    }

    [Fact]
    public void Join_ShouldReturnFull_WhenBothSeatsAreTaken()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        store.CreateNew(GroupChatId);

        var pilot = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");
        var copilot = new LobbyPlayer(UserId: 2, DisplayName: "Copilot");
        store.Join(GroupChatId, pilot);
        store.Join(GroupChatId, copilot);

        var spectator = new LobbyPlayer(UserId: 3, DisplayName: "Spectator");

        // Act
        var result = store.Join(GroupChatId, spectator);

        // Assert
        result.Should().BeEquivalentTo(new LobbyJoinResult(
            LobbyJoinStatus.Full,
            new LobbySnapshot(GroupChatId, Pilot: pilot, Copilot: copilot)));
    }

    [Fact]
    public void Join_ShouldReturnAlreadySeated_WhenPlayerIsAlreadySeated()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        store.CreateNew(GroupChatId);

        var player = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");
        store.Join(GroupChatId, player);

        // Act
        var result = store.Join(GroupChatId, player);

        // Assert
        result.Should().BeEquivalentTo(new LobbyJoinResult(
            LobbyJoinStatus.AlreadySeated,
            new LobbySnapshot(GroupChatId, Pilot: player, Copilot: null)));
    }

    [Fact]
    public void Join_ShouldReturnNoLobby_WhenLobbyDoesNotExist()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        var player = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");

        // Act
        var result = store.Join(GroupChatId, player);

        // Assert
        result.Should().BeEquivalentTo(new LobbyJoinResult(LobbyJoinStatus.NoLobby, null));
    }

    [Fact]
    public void GetSnapshot_ShouldReturnNull_WhenLobbyDoesNotExist()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();

        // Act
        var snapshot = store.GetSnapshot(GroupChatId);

        // Assert
        snapshot.Should().BeNull();
    }

    [Fact]
    public void GetSnapshot_ShouldReturnSnapshot_WhenLobbyExists()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        store.CreateNew(GroupChatId);

        var pilot = new LobbyPlayer(UserId: 1, DisplayName: "Pilot");
        store.Join(GroupChatId, pilot);

        // Act
        var snapshot = store.GetSnapshot(GroupChatId);

        // Assert
        snapshot.Should().BeEquivalentTo(new LobbySnapshot(GroupChatId, Pilot: pilot, Copilot: null));
    }
}
