using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class AirportTests
{
    [Fact]
    public void Airport_Constructor_ShouldCreateEmptyQueue_WhenNoSegments()
    {
        // Arrange
        var segments = Enumerable.Empty<PathSegment>();

        // Act
        var airport = new Airport(segments);

        // Assert
        airport.PathSegments.Should().BeEmpty();
    }

    [Fact]
    public void Airport_ShouldPreserveOrderOfPathSegments()
    {
        // Arrange
        var segments = new[] { new PathSegment(1), new PathSegment(2), new PathSegment(3) };

        // Act
        var airport = new Airport(segments);

        // Assert
        airport.PathSegments.Select(s => s.NumberOfPlanes).Should().Equal(1, 2, 3);
    }

    [Fact]
    public void MontrealAirport_ImplicitConversion_ShouldProduceExpectedSequence()
    {
        // Arrange
        var montreal = new MontrealAirport();

        // Act
        Airport airport = montreal;

        // Assert
        airport.PathSegments.Select(s => s.NumberOfPlanes)
            .Should()
            .Equal(0, 0, 1, 2, 1, 3, 2);
    }
}