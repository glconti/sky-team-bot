using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class AltitudeTests
{
    [Fact]
    public void ctor_ShouldInitializeAltitudeCorrectly()
    {
        // Arrange & Act
        var altitude = new Altitude();

        // Assert
        altitude.CurrentAltitude.HasReroll.Should().BeTrue();
        altitude.CurrentPlayer.Should().Be(Player.Pilot);
    }

    [Fact]
    public void Advance_UpdatesAltitudeAndPlayerTurn()
    {
        // Arrange
        var altitude = new Altitude();

        // Act
        altitude.Advance();

        // Assert
        altitude.CurrentAltitude.HasReroll.Should().BeFalse();
        altitude.CurrentPlayer.Should().Be(Player.Copilot);
    }
}