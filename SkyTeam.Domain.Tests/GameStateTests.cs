namespace SkyTeam.Domain.Tests;

using FluentAssertions;

public class GameStateTests
{
    [Fact]
    public void CurrentPlayer_ShouldBePilot_WhenInitialized()
    {
        // Arrange & Act
        var state = new GameState();

        // Assert
        state.CurrentPlayer.Should().Be(Player.Pilot);
    }

    [Fact]
    public void AddBlueDie_ShouldAddToUnusedBlueDice()
    {
        // Arrange
        var state = new GameState();
        var die = BlueDie.Roll();

        // Act
        state.AddBlueDie(die);

        // Assert
        state.UnusedBlueDice.Should().HaveCount(1);
        state.UnusedBlueDice[0].Should().Be(die);
    }

    [Fact]
    public void AddOrangeDie_ShouldAddToUnusedOrangeDice()
    {
        // Arrange
        var state = new GameState();
        var die = OrangeDie.Roll();

        // Act
        state.AddOrangeDie(die);

        // Assert
        state.UnusedOrangeDice.Should().HaveCount(1);
        state.UnusedOrangeDice[0].Should().Be(die);
    }


    [Fact]
    public void RemoveBlueDie_ShouldRemoveFromUnusedBlueDice()
    {
        // Arrange
        var state = new GameState();
        var die = BlueDie.Roll();
        state.AddBlueDie(die);

        // Act
        state.RemoveBlueDie(die);

        // Assert
        state.UnusedBlueDice.Should().BeEmpty();
    }

    [Fact]
    public void RemoveOrangeDie_ShouldRemoveFromUnusedOrangeDice()
    {
        // Arrange
        var state = new GameState();
        var die = OrangeDie.Roll();
        state.AddOrangeDie(die);

        // Act
        state.RemoveOrangeDie(die);

        // Assert
        state.UnusedOrangeDice.Should().BeEmpty();
    }

    [Fact]
    public void RemoveBlueDie_ShouldThrow_WhenDieNotFound()
    {
        // Arrange
        var state = new GameState();
        var die = BlueDie.Roll();

        // Act
        var removing = () => state.RemoveBlueDie(die);

        // Assert
        removing.Should().Throw<InvalidOperationException>()
            .WithMessage("Blue die not found in unused dice.");
    }

    [Fact]
    public void RemoveOrangeDie_ShouldThrow_WhenDieNotFound()
    {
        // Arrange
        var state = new GameState();
        var die = OrangeDie.Roll();

        // Act
        var removing = () => state.RemoveOrangeDie(die);

        // Assert
        removing.Should().Throw<InvalidOperationException>()
            .WithMessage("Orange die not found in unused dice.");
    }

    [Fact]
    public void SwitchPlayer_ShouldToggleBetweenPilotAndCopilot()
    {
        // Arrange
        var state = new GameState();

        // Act
        state.SwitchPlayer();

        // Assert
        state.CurrentPlayer.Should().Be(Player.Copilot);

        // Act
        state.SwitchPlayer();

        // Assert
        state.CurrentPlayer.Should().Be(Player.Pilot);
    }

    [Fact]
    public void Reset_ShouldClearAllDiceAndResetToInitialState()
    {
        // Arrange
        var state = new GameState();
        state.AddBlueDie(BlueDie.Roll());
        state.AddOrangeDie(OrangeDie.Roll());
        state.SwitchPlayer();

        // Act
        state.Reset();

        // Assert
        state.UnusedBlueDice.Should().BeEmpty();
        state.UnusedOrangeDice.Should().BeEmpty();
        state.CurrentPlayer.Should().Be(Player.Pilot);
    }

    [Fact]
    public void UnusedBlueDice_ShouldBeReadOnly()
    {
        // Arrange
        var state = new GameState();

        // Act & Assert
        state.UnusedBlueDice.Should().BeAssignableTo<IReadOnlyList<BlueDie>>();
    }

    [Fact]
    public void UnusedOrangeDice_ShouldBeReadOnly()
    {
        // Arrange
        var state = new GameState();

        // Act & Assert
        state.UnusedOrangeDice.Should().BeAssignableTo<IReadOnlyList<OrangeDie>>();
    }
}
