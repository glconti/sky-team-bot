using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class DieTests
{
    [Theory]
    [InlineData("Die")]
    [InlineData("BlueDie")]
    [InlineData("OrangeDie")]
    public void Die_Roll_ShouldReturnDifferentValuesOverMultipleRolls(string dieType)
    {
        // Arrange
        var rolls = new HashSet<int>();
        const int numberOfRolls = 1000;
        Func<int> rollFunc = dieType switch
        {
            "Die" => () => Die.Roll(),
            "BlueDie" => () => BlueDie.Roll(),
            "OrangeDie" => () => OrangeDie.Roll(),
            _ => throw new ArgumentException($"Unknown die type: {dieType}")
        };

        // Act
        for (var i = 0; i < numberOfRolls; i++)
        {
            rolls.Add(rollFunc());

            if (rolls.Count < 6) continue;

            break; // All possible values have been rolled
        }

        // Assert
        rolls.Should().BeEquivalentTo([1, 2, 3, 4, 5, 6]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void FromValue_ShouldThrow_WhenValueIsOutsideOneToSix(int value)
    {
        // Arrange
        // Act
        var creating = () => Die.FromValue(value);

        // Assert
        creating.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void BlueDieFromValue_ShouldThrow_WhenValueIsOutsideOneToSix(int value)
    {
        // Arrange
        // Act
        var creating = () => BlueDie.FromValue(value);

        // Assert
        creating.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void OrangeDieFromValue_ShouldThrow_WhenValueIsOutsideOneToSix(int value)
    {
        // Arrange
        // Act
        var creating = () => OrangeDie.FromValue(value);

        // Assert
        creating.Should().Throw<ArgumentOutOfRangeException>();
    }
}