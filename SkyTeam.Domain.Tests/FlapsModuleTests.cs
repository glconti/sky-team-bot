namespace SkyTeam.Domain.Tests;

using System;
using System.Linq;
using FluentAssertions;

public class FlapsModuleTests
{
    [Fact]
    public void CanAcceptOrangeDie_ShouldBeTrue_ForCopilot_WhenSwitchesRemain()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);

        // Act
        var canAccept = module.CanAcceptOrangeDie(Player.Copilot);

        // Assert
        canAccept.Should().BeTrue();
    }

    [Fact]
    public void CanAcceptOrangeDie_ShouldBeFalse_ForPilot_WhenSwitchesRemain()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);

        // Act
        var canAccept = module.CanAcceptOrangeDie(Player.Pilot);

        // Assert
        canAccept.Should().BeFalse();
    }

    [Fact]
    public void AssignOrangeDie_ShouldActivateNextSwitchAndMoveThreshold_WhenAllowedValue()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);
        var initialOrangeThreshold = airport.OrangeAerodynamicsThreshold;

        // Act
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        // Assert
        new { module.FlapsValue, airport.OrangeAerodynamicsThreshold }
            .Should().BeEquivalentTo(new { FlapsValue = 1, OrangeAerodynamicsThreshold = initialOrangeThreshold + 1 });
    }

    [Fact]
    public void AssignOrangeDie_ShouldThrow_WhenValueNotAllowedForNextSwitch()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);

        // Act
        var assigning = () => module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Assert
        assigning.Should().Throw<InvalidOperationException>()
            .WithMessage("Flaps requires die value 1 or 2 next.");
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldOnlyAllowedDistinctValues_ForNextSwitch_WhenCopilotTurn()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);
        var unusedOrangeDice = new[]
        {
            OrangeDie.FromValue(2),
            OrangeDie.FromValue(1),
            OrangeDie.FromValue(2),
            OrangeDie.FromValue(3),
            OrangeDie.FromValue(6)
        };

        // Act
        var commands = module.GetAvailableCommands(Player.Copilot, [], unusedOrangeDice).ToArray();

        // Assert
        commands.Select(command => command.CommandId)
            .Should().Equal(["Flaps.AssignOrange:1", "Flaps.AssignOrange:2"]);
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenCurrentPlayerIsNotCopilot()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);
        var unusedOrangeDice = new[] { OrangeDie.FromValue(1), OrangeDie.FromValue(2) };

        // Act
        var commands = module.GetAvailableCommands(Player.Pilot, [], unusedOrangeDice).ToArray();

        // Assert
        commands.Should().BeEmpty();
    }

    [Fact]
    public void AssignOrangeDie_ShouldThrow_WhenAllSwitchesActivated()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new FlapsModule(airport);

        module.AssignOrangeDie(OrangeDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(2));
        module.AssignOrangeDie(OrangeDie.FromValue(4));
        module.AssignOrangeDie(OrangeDie.FromValue(5));

        // Act
        var assigning = () => module.AssignOrangeDie(OrangeDie.FromValue(6));

        // Assert
        assigning.Should().Throw<InvalidOperationException>()
            .WithMessage("All flaps switches are already activated.");
    }
}
