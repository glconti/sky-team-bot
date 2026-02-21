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

    [Fact]
    public void ExecuteCommand_ShouldMutateAxisPosition_WhenCommandsReturnedByModuleAreExecuted()
    {
        // Arrange
        var axis = new AxisPositionModule();
        var game = CreateGame([axis]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(4)], orangeDice: [OrangeDie.FromValue(5)]);

        var assignBlueCommandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(assignBlueCommandId);

        var assignOrangeCommandId = game.GetAvailableCommands().Single().CommandId;
        game.ExecuteCommand(assignOrangeCommandId);

        // Assert
        new
        {
            axis.AxisPosition,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { AxisPosition = -1, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Pilot });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateEnginesAndAirport_WhenCommandsReturnedByModuleAreExecuted()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var engines = new EnginesModule(airport);

        var game = CreateGame([engines], airport: airport);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(4)], orangeDice: [OrangeDie.FromValue(3)]);

        var assignBlueCommandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(assignBlueCommandId);

        var assignOrangeCommandId = game.GetAvailableCommands().Single().CommandId;
        game.ExecuteCommand(assignOrangeCommandId);

        // Assert
        new
        {
            engines.LastSpeed,
            airport.CurrentPositionIndex,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { LastSpeed = (int?)7, CurrentPositionIndex = 1, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Pilot });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateBrakes_WhenCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var brakes = new BrakesModule();
        var game = CreateGame([brakes]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(2)], orangeDice: []);

        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            brakes.BrakesValue,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { BrakesValue = 2, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Copilot });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateFlapsAndAirport_WhenCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var flaps = new FlapsModule(airport);

        var game = CreateGame([flaps], airport: airport);
        SetUnusedDice(game, blueDice: [], orangeDice: [OrangeDie.FromValue(1)]);

        game.SwitchPlayer();
        var initialThreshold = airport.OrangeAerodynamicsThreshold;

        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            flaps.FlapsValue,
            airport.OrangeAerodynamicsThreshold,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new
        {
            FlapsValue = 1,
            OrangeAerodynamicsThreshold = initialThreshold + 1,
            OrangeRemaining = 0,
            CurrentPlayer = Player.Pilot
        });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateLandingGearAndAirport_WhenCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var landingGear = new LandingGearModule(airport);

        var game = CreateGame([landingGear], airport: airport);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(1)], orangeDice: []);

        var initialThreshold = airport.BlueAerodynamicsThreshold;
        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            landingGear.LandingGearValue,
            airport.BlueAerodynamicsThreshold,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new
        {
            LandingGearValue = 1,
            BlueAerodynamicsThreshold = initialThreshold + 1,
            BlueRemaining = 0,
            OrangeRemaining = 0,
            CurrentPlayer = Player.Copilot
        });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateAirportPlaneTokens_WhenRadioCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var radio = new RadioModule(airport);

        var game = CreateGame([radio], airport: airport);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(3)], orangeDice: []);

        var before = airport.PathSegments[2].PlaneTokens;
        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            Before = before,
            After = airport.PathSegments[2].PlaneTokens,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { Before = 1, After = 0, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Copilot });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateAirportPlaneTokens_WhenRadioOrangeCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var radio = new RadioModule(airport);

        var game = CreateGame([radio], airport: airport);
        SetUnusedDice(game, blueDice: [], orangeDice: [OrangeDie.FromValue(3)]);

        game.SwitchPlayer();

        var before = airport.PathSegments[2].PlaneTokens;
        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            Before = before,
            After = airport.PathSegments[2].PlaneTokens,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { Before = 1, After = 0, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Pilot });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateConcentrationTokenPool_WhenCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var concentration = new ConcentrationModule();
        var game = CreateGame([concentration]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(1)], orangeDice: []);

        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            TokenCount = concentration.TokenPool.Count,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { TokenCount = 1, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Copilot });
    }

    [Fact]
    public void ExecuteCommand_ShouldMutateConcentrationTokenPool_WhenOrangeCommandReturnedByModuleIsExecuted()
    {
        // Arrange
        var concentration = new ConcentrationModule();
        var game = CreateGame([concentration]);
        SetUnusedDice(game, blueDice: [], orangeDice: [OrangeDie.FromValue(1)]);

        game.SwitchPlayer();

        var commandId = game.GetAvailableCommands().Single().CommandId;

        // Act
        game.ExecuteCommand(commandId);

        // Assert
        new
        {
            TokenCount = concentration.TokenPool.Count,
            BlueRemaining = game.UnusedBlueDice.Count,
            OrangeRemaining = game.UnusedOrangeDice.Count,
            game.CurrentPlayer
        }.Should().BeEquivalentTo(new { TokenCount = 1, BlueRemaining = 0, OrangeRemaining = 0, CurrentPlayer = Player.Pilot });
    }

    private static Game CreateGame(GameModule[]? modules = null, Altitude? altitude = null, Airport? airport = null)
    {
        airport ??= (Airport)new MontrealAirport();
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
