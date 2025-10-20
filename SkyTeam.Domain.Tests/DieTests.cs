using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

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
        rolls.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6 });
    }
}