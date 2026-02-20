namespace SkyTeam.Domain.Tests;

using System.Linq;
using System.Reflection;
using FluentAssertions;

public class GameCommandTests
{
    [Fact]
    public void ctor_ShouldSetCurrentPlayerFromAltitude_WhenAltitudeWasAdvancedBeforeConstruction()
    {
        // Arrange
        var altitude = new Altitude();
        altitude.Advance();

        // Act
        var game = CreateGame(altitude: altitude);

        // Assert
        game.CurrentPlayer.Should().Be(Player.Copilot);
    }

    [Fact]
    public void ctor_ShouldCopyModulesArray_WhenSourceArrayIsModifiedAfterConstruction()
    {
        // Arrange
        var originalModule = new TestModule([new TestCommand("Original")]);
        var modifiedModule = new TestModule([new TestCommand("Modified")]);

        var modules = new GameModule[] { originalModule };
        var game = CreateGame(modules);
        modules[0] = modifiedModule;

        // Act
        var commands = game.GetAvailableCommands().ToArray();

        // Assert
        commands.Should().ContainSingle(c => c.CommandId == "Original");
        commands.Should().NotContain(c => c.CommandId == "Modified");
    }

    [Fact]
    public void NextRound_ShouldThrow_WhenUnusedDiceRemain()
    {
        // Arrange
        var game = CreateGame();

        // Act
        var advancing = () => game.NextRound();

        // Assert
        advancing.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot proceed to next round with unused dice.");
    }

    [Fact]
    public void NextRound_ShouldAdvanceRound_RollDice_AndSwitchCurrentPlayer_WhenNoUnusedDiceRemain()
    {
        // Arrange
        var game = CreateGame();
        var startingPlayer = game.CurrentPlayer;
        ClearUnusedDice(game);

        // Act
        game.NextRound();

        // Assert
        game.CurrentPlayer.Should().NotBe(startingPlayer);
        game.UnusedBlueDice.Should().HaveCount(4).And.OnlyContain(die => die != null);
        game.UnusedOrangeDice.Should().HaveCount(4).And.OnlyContain(die => die != null);
    }

    [Fact]
    public void SwitchPlayer_ShouldToggleBetweenPilotAndCopilot()
    {
        // Arrange
        var game = CreateGame();

        // Act
        game.SwitchPlayer();

        // Assert
        game.CurrentPlayer.Should().Be(Player.Copilot);

        // Act
        game.SwitchPlayer();

        // Assert
        game.CurrentPlayer.Should().Be(Player.Pilot);
    }

    [Fact]
    public void GetAvailableCommands_ShouldNotIncludeNextRound_WhenUnusedDiceRemain()
    {
        // Arrange
        var game = CreateGame([new TestModule([new TestCommand("TestCommand")])]);

        // Act
        var commands = game.GetAvailableCommands().ToArray();

        // Assert
        commands.Should().ContainSingle(c => c.CommandId == "TestCommand");
        commands.Should().NotContain(c => c.CommandId == "NextRound");
    }

    [Fact]
    public void GetAvailableCommands_ShouldReturnNextRoundOnly_WhenNoUnusedDiceRemain()
    {
        // Arrange
        var game = CreateGame([new TestModule([new TestCommand("TestCommand")])]);
        ClearUnusedDice(game);

        // Act
        var commands = game.GetAvailableCommands().ToArray();

        // Assert
        commands.Should().ContainSingle(c => c.CommandId == "NextRound");
        commands.Should().NotContain(c => c.CommandId == "TestCommand");
    }

    private static Game CreateGame(GameModule[]? modules = null, Altitude? altitude = null)
    {
        var airport = (Airport)new MontrealAirport();
        altitude ??= new Altitude();
        modules ??= [];

        return new Game(airport, altitude, modules);
    }

    private static void ClearUnusedDice(Game game) => GetState(game).ClearUnusedDice();

    private static GameState GetState(Game game)
    {
        var field = typeof(Game).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Could not find '_state' field on {nameof(Game)}.");

        return (GameState)field.GetValue(game)!;
    }

    private sealed class TestModule : GameModule
    {
        private readonly GameCommand[] _commands;

        public TestModule(GameCommand[] commands) => _commands = commands.ToArray();

        public override bool CanAcceptBlueDie(Player player) => true;
        public override bool CanAcceptOrangeDie(Player player) => true;
        public override string GetModuleName() => "Test Module";

        public override IEnumerable<GameCommand> GetAvailableCommands(Player currentPlayer) => _commands;
    }

    private sealed record TestCommand(string Id) : GameCommand
    {
        public override string CommandId => Id;
        public override string DisplayName => Id;
    }
}
