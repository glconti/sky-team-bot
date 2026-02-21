namespace SkyTeam.Domain.Tests;

using System.Reflection;
using FluentAssertions;

public class ExecuteCommandTests
{
    [Fact]
    public void ExecuteCommand_ShouldConsumeBlueDieAndSwitchPlayer_WhenAxisBlueCommandExecuted()
    {
        // Arrange
        var game = CreateGame([new AxisPositionModule()]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(1)], orangeDice: []);

        // Act
        game.ExecuteCommand("Axis.AssignBlue:1");

        // Assert
        (game.UnusedBlueDice.Count, game.UnusedOrangeDice.Count, game.CurrentPlayer)
            .Should().Be((0, 0, Player.Copilot));
    }

    [Fact]
    public void ExecuteCommand_ShouldSetLostAndRethrow_WhenAxisGoesOutOfBounds()
    {
        // Arrange
        var game = CreateGame([new AxisPositionModule()]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(6)], orangeDice: [OrangeDie.FromValue(3)]);

        game.ExecuteCommand("Axis.AssignBlue:6");

        // Act
        var invoking = () => game.ExecuteCommand("Axis.AssignOrange:3");

        // Assert
        invoking.Should().Throw<InvalidOperationException>()
            .WithMessage("Axis position out of bounds.");

        game.Status.Should().Be(GameStatus.Lost);
    }

    [Fact]
    public void ExecuteCommand_ShouldThrow_WhenCommandNotAvailable()
    {
        // Arrange
        var game = CreateGame([new AxisPositionModule()]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(1)], orangeDice: []);

        // Act
        var invoking = () => game.ExecuteCommand("Axis.AssignBlue:2");

        // Assert
        invoking.Should().Throw<InvalidOperationException>()
            .WithMessage("Command 'Axis.AssignBlue:2' is not currently available.");
    }

    [Fact]
    public void ExecuteCommand_ShouldExecuteNextRound_WhenNoUnusedDiceRemain()
    {
        // Arrange
        var game = CreateGame();
        SetUnusedDice(game, blueDice: [], orangeDice: []);

        // Act
        game.ExecuteCommand("NextRound");

        // Assert
        (game.CurrentPlayer, game.UnusedBlueDice.Count, game.UnusedOrangeDice.Count)
            .Should().Be((Player.Copilot, 4, 4));
    }

    [Fact]
    public void ExecuteCommand_ShouldThrow_WhenNextRoundExecutedWithUnusedDiceRemaining()
    {
        // Arrange
        var game = CreateGame();

        // Act
        var invoking = () => game.ExecuteCommand("NextRound");

        // Assert
        invoking.Should().Throw<InvalidOperationException>()
            .WithMessage("Command 'NextRound' is not currently available.");
    }

    [Fact]
    public void GetAvailableCommands_ShouldIncludeTokenAdjustedCommands_WhenCoffeeTokensAreAvailable()
    {
        // Arrange
        var axis = new AxisPositionModule();
        var concentration = new ConcentrationModule();

        concentration.AssignBlueDie(BlueDie.FromValue(1));
        concentration.AssignOrangeDie(OrangeDie.FromValue(1));
        concentration.ResetRound();

        concentration.TokenPool.Count.Should().Be(2);

        var game = CreateGame([axis, concentration]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(1)], orangeDice: []);

        // Act
        var commandIds = game.GetAvailableCommands().Select(c => c.CommandId).ToArray();

        // Assert
        commandIds.Should().Contain(["Axis.AssignBlue:1", "Axis.AssignBlue:1>2", "Axis.AssignBlue:1>3"]);
    }

    [Fact]
    public void ExecuteCommand_ShouldSpendTokensAndUseEffectiveValue_WhenTokenAdjustedCommandExecuted()
    {
        // Arrange
        var axis = new AxisPositionModule();
        var concentration = new ConcentrationModule();

        concentration.AssignBlueDie(BlueDie.FromValue(1));
        concentration.AssignOrangeDie(OrangeDie.FromValue(1));
        concentration.ResetRound();

        var game = CreateGame([axis, concentration]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(1)], orangeDice: [OrangeDie.FromValue(1)]);

        // Act
        game.ExecuteCommand("Axis.AssignBlue:1>3");

        // Assert
        concentration.TokenPool.Count.Should().Be(0);
        game.CurrentPlayer.Should().Be(Player.Copilot);

        // Act
        game.ExecuteCommand("Axis.AssignOrange:1");

        // Assert
        axis.AxisPosition.Should().Be(2);
    }

    private static Game CreateGame(GameModule[]? modules = null, Altitude? altitude = null)
    {
        var airport = (Airport)new MontrealAirport();
        altitude ??= new Altitude();
        modules ??= [];

        return new Game(airport, altitude, modules);
    }

    private static void SetUnusedDice(Game game, BlueDie[] blueDice, OrangeDie[] orangeDice)
    {
        var state = GetState(game);
        state.ClearUnusedDice();

        foreach (var blueDie in blueDice)
            state.AddBlueDie(blueDie);

        foreach (var orangeDie in orangeDice)
            state.AddOrangeDie(orangeDie);
    }

    private static GameState GetState(Game game)
    {
        var field = typeof(Game).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Could not find '_state' field on {nameof(Game)}.");

        return (GameState)field.GetValue(game)!;
    }
}
