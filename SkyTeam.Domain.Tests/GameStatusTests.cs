namespace SkyTeam.Domain.Tests;

using System.Linq;
using System.Reflection;
using FluentAssertions;

public class GameStatusTests
{
    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenGameHasEnded()
    {
        // Arrange
        var game = CreateLostGameByCrash();

        // Act
        var commands = game.GetAvailableCommands().ToArray();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void NextRound_ShouldSetLost_WhenAltitudeBecomesLandedBeforeAirportFinalPosition()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();

        var altitude = new Altitude();
        for (var i = 0; i < 5; i++)
            altitude.Advance();

        var game = new Game(airport, altitude, []);
        ClearUnusedDice(game);

        // Act
        game.ExecuteCommand("NextRound");

        // Assert
        game.Status.Should().Be(GameStatus.Lost);
    }

    [Fact]
    public void NextRound_ShouldSetWon_WhenAllLandingCriteriaMet()
    {
        // Arrange
        var airport = CreateAirportWithNoPlaneTokens(segmentCount: 7);
        airport.AdvanceApproach(6);

        var altitude = new Altitude();
        for (var i = 0; i < 6; i++)
            altitude.Advance();

        var axisModule = new AxisPositionModule();
        var enginesModule = new EnginesModule(airport);
        var brakesModule = new BrakesModule();
        var flapsModule = new FlapsModule(airport);
        var landingGearModule = new LandingGearModule(airport);

        var game = new Game(airport, altitude, [axisModule, enginesModule, brakesModule, flapsModule, landingGearModule]);

        brakesModule.AssignBlueDie(BlueDie.FromValue(2));
        brakesModule.AssignBlueDie(BlueDie.FromValue(4));
        brakesModule.AssignBlueDie(BlueDie.FromValue(6));

        flapsModule.AssignOrangeDie(OrangeDie.FromValue(1));
        flapsModule.AssignOrangeDie(OrangeDie.FromValue(2));
        flapsModule.AssignOrangeDie(OrangeDie.FromValue(4));
        flapsModule.AssignOrangeDie(OrangeDie.FromValue(5));

        landingGearModule.AssignBlueDie(BlueDie.FromValue(1));
        landingGearModule.AssignBlueDie(BlueDie.FromValue(3));
        landingGearModule.AssignBlueDie(BlueDie.FromValue(5));

        enginesModule.AssignBlueDie(BlueDie.FromValue(6));
        enginesModule.AssignOrangeDie(OrangeDie.FromValue(3));

        ClearUnusedDice(game);

        // Act
        game.ExecuteCommand("NextRound");

        // Assert
        game.Status.Should().Be(GameStatus.Won);
        game.UnusedBlueDice.Should().BeEmpty();
        game.UnusedOrangeDice.Should().BeEmpty();
    }

    [Fact]
    public void ExecuteCommand_ShouldSetLostAndRethrow_WhenAvailableCommandFailsDuringModuleAssignment()
    {
        // Arrange
        var airport = CreateAirportWithNoPlaneTokens(segmentCount: 7);
        airport.AdvanceApproach(6);

        var enginesModule = new EnginesModule(airport);
        enginesModule.AssignOrangeDie(OrangeDie.FromValue(6));

        var game = new Game(airport, new Altitude(), [enginesModule]);
        SetUnusedDice(game, blueDice: [BlueDie.FromValue(6)], orangeDice: []);

        // Act
        var invoking = () => game.ExecuteCommand("Engines.AssignBlue:6");

        // Assert
        invoking.Should().Throw<InvalidOperationException>();
        game.Status.Should().Be(GameStatus.Lost);
    }

    private static Game CreateLostGameByCrash()
    {
        var airport = (Airport)new MontrealAirport();

        var altitude = new Altitude();
        for (var i = 0; i < 5; i++)
            altitude.Advance();

        var game = new Game(airport, altitude, []);
        ClearUnusedDice(game);

        game.ExecuteCommand("NextRound");

        game.Status.Should().Be(GameStatus.Lost);
        return game;
    }

    private static Airport CreateAirportWithNoPlaneTokens(int segmentCount) =>
        new(Enumerable.Range(0, segmentCount).Select(_ => new PathSegment(0)));

    private static void ClearUnusedDice(Game game) => GetState(game).ClearUnusedDice();

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
