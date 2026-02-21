namespace SkyTeam.Domain.Tests;

using System.Linq;
using FluentAssertions;

public class BrakesModuleTests
{
    [Fact]
    public void CanAcceptBlueDie_ShouldBeTrueForPilot_WhenNotAllSwitchesAreActivated()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        var canAccept = module.CanAcceptBlueDie(Player.Pilot);

        // Assert
        canAccept.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CanAcceptOrangeDie_ShouldAlwaysBeFalse(int playerValue)
    {
        // Arrange
        var module = new BrakesModule();
        var player = (Player)playerValue;

        // Act
        var canAccept = module.CanAcceptOrangeDie(player);

        // Assert
        canAccept.Should().BeFalse();
    }

    [Fact]
    public void CanAcceptBlueDie_ShouldBeFalse_ForCopilot_AndWhenAllSwitchesAreActivated()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        var canCopilotAccept = module.CanAcceptBlueDie(Player.Copilot);

        module.AssignBlueDie(BlueDie.FromValue(2));
        module.AssignBlueDie(BlueDie.FromValue(4));
        module.AssignBlueDie(BlueDie.FromValue(6));

        var canPilotAcceptAfterExhausted = module.CanAcceptBlueDie(Player.Pilot);

        // Assert
        canCopilotAccept.Should().BeFalse();
        canPilotAcceptAfterExhausted.Should().BeFalse();
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenCurrentPlayerIsNotPilot()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        var commands = module.GetAvailableCommands(Player.Copilot, [BlueDie.FromValue(2)], []).ToArray();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenNoUnusedBlueDiceMatchRequiredValue()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        var commands = module.GetAvailableCommands(Player.Pilot, [BlueDie.FromValue(1)], []).ToArray();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenNoUnusedBlueDice()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        var commands = module.GetAvailableCommands(Player.Pilot, [], []).ToArray();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void AssignBlueDie_ShouldAdvanceInOrder_AndIncreaseBrakesValue_WhenCorrectValuesArePlaced()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        module.AssignBlueDie(BlueDie.FromValue(2));
        module.AssignBlueDie(BlueDie.FromValue(4));
        module.AssignBlueDie(BlueDie.FromValue(6));

        // Assert
        module.BrakesValue.Should().Be(6);

        var commands = module.GetAvailableCommands(Player.Pilot, [BlueDie.FromValue(2)], []).ToArray();
        commands.Should().BeEmpty();
    }

    [Fact]
    public void AssignBlueDie_ShouldThrow_WhenDieValueDoesNotMatchNextRequiredValue()
    {
        // Arrange
        var module = new BrakesModule();

        // Act
        var assigning = () => module.AssignBlueDie(BlueDie.FromValue(4));

        // Assert
        assigning.Should().Throw<InvalidOperationException>()
            .WithMessage("Brakes requires die value 2 next.");
    }

    [Fact]
    public void GetAvailableCommands_ShouldReturnNextRequiredValue_WhenPlayerHasMatchingDie()
    {
        // Arrange
        var module = new BrakesModule();
        var unusedBlueDice = new[] { BlueDie.FromValue(1), BlueDie.FromValue(2) };

        // Act
        var commands = module.GetAvailableCommands(Player.Pilot, unusedBlueDice, []).ToArray();

        // Assert
        commands.Should().ContainSingle();
        commands[0].CommandId.Should().Be("Brakes.AssignBlue:2");
    }
}
