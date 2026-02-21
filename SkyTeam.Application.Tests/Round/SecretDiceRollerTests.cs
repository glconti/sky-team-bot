namespace SkyTeam.Application.Tests.Round;

using FluentAssertions;
using SkyTeam.Application.Round;

public sealed class SecretDiceRollerTests
{
    [Fact]
    public void Roll_ShouldReturnDiceForEachSeat_WhenRollerProducesValidValues()
    {
        // Arrange
        var values = new Queue<int>([1, 2, 3, 4, 5, 6, 1, 2]);

        // Act
        var result = SecretDiceRoller.Roll(() => values.Dequeue(), dicePerSeat: 4);

        // Assert
        result.PilotDice.Should().Equal(1, 2, 3, 4);
        result.CopilotDice.Should().Equal(5, 6, 1, 2);
    }

    [Fact]
    public void Roll_ShouldThrowArgumentOutOfRangeException_WhenDicePerSeatIsLessThanOne()
    {
        // Arrange
        var rollDie = () => 1;

        // Act
        var invoking = () => SecretDiceRoller.Roll(rollDie, dicePerSeat: 0);

        // Assert
        invoking.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Roll_ShouldThrowInvalidOperationException_WhenRollerProducesValueOutsideRange()
    {
        // Arrange
        var rollDie = () => 7;

        // Act
        var invoking = () => SecretDiceRoller.Roll(rollDie);

        // Assert
        invoking.Should().Throw<InvalidOperationException>();
    }
}
