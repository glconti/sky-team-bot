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
    public void GetAvailableCommands_ShouldReturnCommandsForUnusedDiceValues_WhenSlotIsEmpty()
    {
        // Arrange
        var module = new AxisPositionModule();
        var unusedBlueDice = new[] { BlueDie.FromValue(2), BlueDie.FromValue(5) };
        var unusedOrangeDice = new[] { OrangeDie.FromValue(1), OrangeDie.FromValue(6) };

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, unusedBlueDice, unusedOrangeDice).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, unusedBlueDice, unusedOrangeDice).ToArray();

        // Assert
        pilotCommands.Select(c => c.CommandId).Should().BeEquivalentTo(["Axis.AssignBlue:2", "Axis.AssignBlue:5"]);
        copilotCommands.Select(c => c.CommandId).Should().BeEquivalentTo(["Axis.AssignOrange:1", "Axis.AssignOrange:6"]);
    }
}
