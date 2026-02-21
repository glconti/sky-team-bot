namespace SkyTeam.Domain.Tests;

using System.Linq;
using FluentAssertions;

public class AxisPositionModuleTests
{
    [Fact]
    public void AssignBlueDie_ShouldThrow_WhenBlueDieAlreadyAssigned()
    {
        // Arrange
        var module = new AxisPositionModule();
        module.AssignBlueDie(BlueDie.FromValue(3));

        // Act
        var assigningAgain = () => module.AssignBlueDie(BlueDie.FromValue(4));

        // Assert
        assigningAgain.Should().Throw<InvalidOperationException>()
            .WithMessage("Blue die already assigned.");
    }

    [Fact]
    public void AssignOrangeDie_ShouldThrow_WhenOrangeDieAlreadyAssigned()
    {
        // Arrange
        var module = new AxisPositionModule();
        module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Act
        var assigningAgain = () => module.AssignOrangeDie(OrangeDie.FromValue(4));

        // Assert
        assigningAgain.Should().Throw<InvalidOperationException>()
            .WithMessage("Orange die already assigned.");
    }

    [Fact]
    public void AssignDice_ShouldRotateAxisTowardHigherDieByDifference_WhenBothDiceAreAssigned()
    {
        // Arrange
        var module = new AxisPositionModule();

        // Act
        module.AssignBlueDie(BlueDie.FromValue(4));
        module.AssignOrangeDie(OrangeDie.FromValue(5));

        // Assert
        module.AxisPosition.Should().Be(-1);
    }

    [Fact]
    public void ResetRound_ShouldClearDiceAssignments_ButKeepAxisPosition()
    {
        // Arrange
        var module = new AxisPositionModule();
        module.AssignBlueDie(BlueDie.FromValue(4));
        module.AssignOrangeDie(OrangeDie.FromValue(5));

        // Act
        module.ResetRound();

        // Assert
        module.CanAcceptBlueDie(Player.Pilot).Should().BeTrue();
        module.CanAcceptOrangeDie(Player.Copilot).Should().BeTrue();
        module.AxisPosition.Should().Be(-1);
    }

    [Fact]
    public void CanAcceptDice_ShouldAllowPilotBlueAndCopilotOrangeOnly_WhenSlotsAreEmpty()
    {
        // Arrange
        var module = new AxisPositionModule();

        // Act
        var canAccept = new
        {
            PilotBlue = module.CanAcceptBlueDie(Player.Pilot),
            CopilotBlue = module.CanAcceptBlueDie(Player.Copilot),
            PilotOrange = module.CanAcceptOrangeDie(Player.Pilot),
            CopilotOrange = module.CanAcceptOrangeDie(Player.Copilot)
        };

        // Assert
        canAccept.Should().BeEquivalentTo(new
        {
            PilotBlue = true,
            CopilotBlue = false,
            PilotOrange = false,
            CopilotOrange = true
        });
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenNoUnusedDiceOfCurrentPlayersColor()
    {
        // Arrange
        var module = new AxisPositionModule();

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, [], [OrangeDie.FromValue(1)], new CoffeeTokenPool()).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, [BlueDie.FromValue(1)], [], new CoffeeTokenPool()).ToArray();

        // Assert
        pilotCommands.Should().BeEmpty();
        copilotCommands.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenSlotIsAlreadyFilledForThatPlayer()
    {
        // Arrange
        var module = new AxisPositionModule();
        module.AssignBlueDie(BlueDie.FromValue(2));
        module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, [BlueDie.FromValue(1)], [OrangeDie.FromValue(1)], new CoffeeTokenPool()).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, [BlueDie.FromValue(1)], [OrangeDie.FromValue(1)], new CoffeeTokenPool()).ToArray();

        // Assert
        pilotCommands.Should().BeEmpty();
        copilotCommands.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableCommands_ShouldReturnDistinctSortedCommandsForUnusedDiceValues_WhenSlotIsEmpty()
    {
        // Arrange
        var module = new AxisPositionModule();
        var unusedBlueDice = new[] { BlueDie.FromValue(5), BlueDie.FromValue(2), BlueDie.FromValue(5) };
        var unusedOrangeDice = new[] { OrangeDie.FromValue(6), OrangeDie.FromValue(1), OrangeDie.FromValue(6) };

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, unusedBlueDice, unusedOrangeDice, new CoffeeTokenPool()).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, unusedBlueDice, unusedOrangeDice, new CoffeeTokenPool()).ToArray();

        // Assert
        pilotCommands.Select(c => c.CommandId).Should().Equal(["Axis.AssignBlue:2", "Axis.AssignBlue:5"]);
        copilotCommands.Select(c => c.CommandId).Should().Equal(["Axis.AssignOrange:1", "Axis.AssignOrange:6"]);
    }

    [Fact]
    public void AssignDice_ShouldThrow_WhenAxisWouldGoOutOfBoundsAboveTwo()
    {
        // Arrange
        var module = new AxisPositionModule();
        module.AssignBlueDie(BlueDie.FromValue(6));

        // Act
        var assigning = () => module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Assert
        assigning.Should().Throw<InvalidOperationException>()
            .WithMessage("Axis position out of bounds.");
    }

    [Fact]
    public void AssignDice_ShouldThrow_WhenAxisWouldGoOutOfBoundsBelowMinusTwo()
    {
        // Arrange
        var module = new AxisPositionModule();
        module.AssignOrangeDie(OrangeDie.FromValue(6));

        // Act
        var assigning = () => module.AssignBlueDie(BlueDie.FromValue(3));

        // Assert
        assigning.Should().Throw<InvalidOperationException>()
            .WithMessage("Axis position out of bounds.");
    }

    [Theory]
    [InlineData(6, 4, 2)]
    [InlineData(4, 6, -2)]
    public void AssignDice_ShouldAllowAxisAtBounds_WhenResultIsMinusTwoOrPlusTwo(int blueValue, int orangeValue, int expectedAxisPosition)
    {
        // Arrange
        var module = new AxisPositionModule();

        // Act
        module.AssignBlueDie(BlueDie.FromValue(blueValue));
        module.AssignOrangeDie(OrangeDie.FromValue(orangeValue));

        // Assert
        module.AxisPosition.Should().Be(expectedAxisPosition);
    }
}
