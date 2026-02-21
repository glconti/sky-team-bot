namespace SkyTeam.Application.Tests.Round;

using FluentAssertions;
using SkyTeam.Application.Round;

public sealed class RoundTurnStateTests
{
    [Fact]
    public void StartNew_ShouldSetCurrentPlayerToStartingPlayer_WhenRoundStarts()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([1, 2, 3, 4]);

        // Act
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand);

        // Assert
        state.CurrentPlayer.Should().Be(PlayerSeat.Pilot);
        state.Phase.Should().Be(RoundPhase.InProgress);
        state.Placements.Should().BeEmpty();
        state.CanUndoLastPlacement(PlayerSeat.Pilot).Should().BeFalse();
    }

    [Fact]
    public void RegisterPlacement_ShouldToggleCurrentPlayerAndConsumeDie_WhenCurrentPlayerPlaces()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand);

        // Act
        var newState = state.RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0);

        // Assert
        state.PilotHand.Dice[0].IsUsed.Should().BeFalse();
        newState.PilotHand.Dice[0].IsUsed.Should().BeTrue();
        newState.CopilotHand.Dice.Should().OnlyContain(d => d.IsUsed == false);

        newState.CurrentPlayer.Should().Be(PlayerSeat.Copilot);
        newState.PlacementsMade.Should().Be(1);
        newState.Placements.Should().ContainSingle().Which.Should().Be(new RoundPlacement(
            Index: 0,
            Player: PlayerSeat.Pilot,
            DieIndex: 0,
            Value: new DieValue(1)));
    }

    [Fact]
    public void CanPlace_ShouldBeFalseForNonCurrentPlayer_WhenInProgress()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0);

        // Act
        var pilotCanPlace = state.CanPlace(PlayerSeat.Pilot);
        var copilotCanPlace = state.CanPlace(PlayerSeat.Copilot);

        // Assert
        pilotCanPlace.Should().BeFalse();
        copilotCanPlace.Should().BeTrue();
    }

    [Fact]
    public void CanUndoLastPlacement_ShouldBeTrueOnlyForPlayerWhoJustPlaced_WhenOpponentHasNotPlayedYet()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0);

        // Act
        var pilotCanUndo = state.CanUndoLastPlacement(PlayerSeat.Pilot);
        var copilotCanUndo = state.CanUndoLastPlacement(PlayerSeat.Copilot);

        // Assert
        pilotCanUndo.Should().BeTrue();
        copilotCanUndo.Should().BeFalse();
    }

    [Fact]
    public void UndoLastPlacement_ShouldRestoreDieAndTurn_WhenUndoIsAllowed()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0);

        // Act
        var undone = state.UndoLastPlacement(PlayerSeat.Pilot);

        // Assert
        undone.CurrentPlayer.Should().Be(PlayerSeat.Pilot);
        undone.Placements.Should().BeEmpty();
        undone.PilotHand.Dice[0].IsUsed.Should().BeFalse();
    }

    [Fact]
    public void CanUndoLastPlacement_ShouldBeFalseForPlayerBeforeOpponentPlays_WhenOpponentHasAlreadyPlayed()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0)
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0);

        // Act
        var pilotCanUndo = state.CanUndoLastPlacement(PlayerSeat.Pilot);

        // Assert
        pilotCanUndo.Should().BeFalse();
    }

    [Fact]
    public void CanUndoLastPlacement_ShouldBeFalse_WhenRoundIsReadyToResolve()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);

        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0)
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 1)
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 1)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 2)
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 2)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 3)
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 3);

        // Act
        var canUndo = state.CanUndoLastPlacement(PlayerSeat.Copilot);

        // Assert
        state.IsReadyToResolve.Should().BeTrue();
        canUndo.Should().BeFalse();
    }
}
