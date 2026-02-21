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
        var newState = state.RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis");

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
            Value: new DieValue(1),
            Target: "Axis"));
    }

    [Fact]
    public void CanPlace_ShouldBeFalseForNonCurrentPlayer_WhenInProgress()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis");

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
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis");

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
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis");

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
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0, target: "Axis");

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
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0, target: "Axis")
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 1, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 1, target: "Axis")
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 2, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 2, target: "Axis")
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 3, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 3, target: "Axis");

        // Act
        var canUndo = state.CanUndoLastPlacement(PlayerSeat.Copilot);

        // Assert
        state.IsReadyToResolve.Should().BeTrue();
        canUndo.Should().BeFalse();
    }

    [Fact]
    public void RegisterPlacement_ShouldRejectWrongPlayer_WhenItIsNotTheirTurn()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand);

        // Act
        var act = () => state.RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0, target: "Axis");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not Copilot*turn*");
    }

    [Fact]
    public void RegisterPlacement_ShouldAcceptCorrectPlayerAndUpdatePublicPlacements_WhenPlayersAlternate()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);
        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 1, target: "Axis");

        // Act
        var act = () => state.RegisterPlacement(PlayerSeat.Pilot, dieIndex: 2, target: "Axis");
        var afterCopilot = state.RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0, target: "Axis");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not Pilot*turn*");

        afterCopilot.PlacementsMade.Should().Be(2);
        afterCopilot.PlacementsRemaining.Should().Be(RoundTurnState.MaxPlacementsPerRound - 2);

        afterCopilot.Placements.Should().Equal(
            new RoundPlacement(0, PlayerSeat.Pilot, DieIndex: 1, Value: new DieValue(2), Target: "Axis"),
            new RoundPlacement(1, PlayerSeat.Copilot, DieIndex: 0, Value: new DieValue(5), Target: "Axis"));
    }

    [Fact]
    public void RegisterPlacement_ShouldTransitionToReadyToResolveAndStopAccepting_WhenEightPlacementsAreMade()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);

        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0, target: "Axis")
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 1, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 1, target: "Axis")
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 2, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 2, target: "Axis")
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 3, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 3, target: "Axis");

        // Act
        var act = () => state.RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis");

        // Assert
        state.IsReadyToResolve.Should().BeTrue();
        state.PlacementsRemaining.Should().Be(0);
        state.CanPlace(PlayerSeat.Pilot).Should().BeFalse();
        state.Placements[^1].Index.Should().Be(RoundTurnState.MaxPlacementsPerRound - 1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not accepting placements*");
    }

    [Fact]
    public void UndoLastPlacement_ShouldThrow_WhenRequestingPlayerIsNotLastPlacer()
    {
        // Arrange
        var pilotHand = SecretDiceHand.Create([1, 2, 3, 4]);
        var copilotHand = SecretDiceHand.Create([5, 6, 1, 2]);

        var state = RoundTurnState.StartNew(roundNumber: 1, PlayerSeat.Pilot, pilotHand, copilotHand)
            .RegisterPlacement(PlayerSeat.Pilot, dieIndex: 0, target: "Axis")
            .RegisterPlacement(PlayerSeat.Copilot, dieIndex: 0, target: "Axis");

        // Act
        var act = () => state.UndoLastPlacement(PlayerSeat.Pilot);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot undo*");
    }
}
