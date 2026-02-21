namespace SkyTeam.Application.Tests.Round;

using FluentAssertions;
using SkyTeam.Application.Round;

public sealed class SecretDiceHandTests
{
    [Fact]
    public void UseDie_ShouldReturnNewHandAndMarkDieUsed_WhenDieIsAvailable()
    {
        // Arrange
        var hand = SecretDiceHand.Create([1, 2, 3, 4]);

        // Act
        var newHand = hand.UseDie(dieIndex: 0, out var value);

        // Assert
        value.Should().Be(new DieValue(1));

        hand.Dice[0].IsUsed.Should().BeFalse();
        newHand.Dice[0].IsUsed.Should().BeTrue();
    }

    [Fact]
    public void UnuseDie_ShouldReturnNewHandAndMarkDieUnused_WhenDieIsUsed()
    {
        // Arrange
        var hand = SecretDiceHand.Create([1, 2, 3, 4]);
        var usedHand = hand.UseDie(dieIndex: 0, out _);

        // Act
        var unusedHand = usedHand.UnuseDie(dieIndex: 0);

        // Assert
        usedHand.Dice[0].IsUsed.Should().BeTrue();
        unusedHand.Dice[0].IsUsed.Should().BeFalse();
    }
}
