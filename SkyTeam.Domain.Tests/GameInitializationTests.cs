using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class GameInitializationTests
{
    [Fact]
    public void ctor_ShouldInitializeWithoutExceptions_AndRollDice_WhenValidArguments()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var altitude = new Altitude();

        // Act
        var game = new Game(airport, altitude, []);

        // Assert
        game.CurrentPlayer.Should().Be(altitude.CurrentPlayer);
        game.UnusedBlueDice.Should().HaveCount(4);
        game.UnusedOrangeDice.Should().HaveCount(4);
    }

    [Fact]
    public void ctor_ShouldThrowArgumentNullException_WhenAirportIsNull()
    {
        // Arrange
        var altitude = new Altitude();

        // Act
        var creating = () => new Game(null!, altitude, []);

        // Assert
        creating.Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "airport");
    }

    [Fact]
    public void ctor_ShouldThrowArgumentNullException_WhenAltitudeIsNull()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();

        // Act
        var creating = () => new Game(airport, null!, []);

        // Assert
        creating.Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "altitude");
    }

    [Fact]
    public void ctor_ShouldThrowArgumentNullException_WhenModulesIsNull()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var altitude = new Altitude();

        // Act
        var creating = () => new Game(airport, altitude, null!);

        // Assert
        creating.Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "modules");
    }

    [Fact]
    public void UnusedDiceCollections_ShouldNotBeMutable()
    {
        // Arrange
        var game = new Game((Airport)new MontrealAirport(), new Altitude(), []);

        // Act
        var addingBlue = () => ((ICollection<BlueDie>)game.UnusedBlueDice).Add(BlueDie.Roll());
        var addingOrange = () => ((ICollection<OrangeDie>)game.UnusedOrangeDice).Add(OrangeDie.Roll());

        // Assert
        addingBlue.Should().Throw<NotSupportedException>();
        addingOrange.Should().Throw<NotSupportedException>();
    }
}
