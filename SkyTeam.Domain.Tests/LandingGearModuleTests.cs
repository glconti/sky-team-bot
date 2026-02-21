namespace SkyTeam.Domain.Tests;

using System;
using System.Linq;
using FluentAssertions;

public class LandingGearModuleTests
{
    [Fact]
    public void CanAcceptDice_ShouldAllowOnlyPilotBlue_WhenSwitchesRemain()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new LandingGearModule(airport);

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
            CopilotOrange = false
        });
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(4, 3)]
    [InlineData(6, 5)]
    public void AssignBlueDie_ShouldActivateSwitchAndMoveBlueThreshold_WhenValueIsAllowed(int value, int otherValueInSameSwitch)
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new LandingGearModule(airport);
        var initialBlueThreshold = airport.BlueAerodynamicsThreshold;

        // Act
        module.AssignBlueDie(BlueDie.FromValue(value));

        var remainingCommands = module.GetAvailableCommands(
            Player.Pilot,
            [BlueDie.FromValue(otherValueInSameSwitch)],
            []).ToArray();

        // Assert
        new { module.LandingGearValue, airport.BlueAerodynamicsThreshold, RemainingCommands = remainingCommands.Length }
            .Should().BeEquivalentTo(new
            {
                LandingGearValue = 1,
                BlueAerodynamicsThreshold = initialBlueThreshold + 1,
                RemainingCommands = 0
            });
    }

    [Fact]
    public void AssignBlueDie_ShouldActivateSwitchesInAnyOrder_AndMoveBlueThresholdOncePerSwitch()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new LandingGearModule(airport);
        var initialBlueThreshold = airport.BlueAerodynamicsThreshold;

        // Act
        module.AssignBlueDie(BlueDie.FromValue(6));
        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignBlueDie(BlueDie.FromValue(4));

        // Assert
        new { module.LandingGearValue, airport.BlueAerodynamicsThreshold }
            .Should().BeEquivalentTo(new { LandingGearValue = 3, BlueAerodynamicsThreshold = initialBlueThreshold + 3 });
    }

    [Fact]
    public void AssignBlueDie_ShouldBeNoOp_WhenSwitchAlreadyActivated()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new LandingGearModule(airport);

        module.AssignBlueDie(BlueDie.FromValue(1));
        var blueThresholdAfterFirstActivation = airport.BlueAerodynamicsThreshold;

        // Act
        module.AssignBlueDie(BlueDie.FromValue(2));

        // Assert
        new { module.LandingGearValue, airport.BlueAerodynamicsThreshold }
            .Should().BeEquivalentTo(new { LandingGearValue = 1, BlueAerodynamicsThreshold = blueThresholdAfterFirstActivation });
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldOnlyPilotCommands_ForDistinctValuesThatActivateRedSwitches()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new LandingGearModule(airport);
        module.AssignBlueDie(BlueDie.FromValue(1));

        var unusedBlueDice = new[]
        {
            BlueDie.FromValue(2),
            BlueDie.FromValue(4),
            BlueDie.FromValue(4),
            BlueDie.FromValue(6),
            BlueDie.FromValue(1)
        };

        // Act
        var pilotCommandIds = module.GetAvailableCommands(Player.Pilot, unusedBlueDice, [])
            .Select(command => command.CommandId)
            .ToArray();

        var copilotCommandIds = module.GetAvailableCommands(Player.Copilot, unusedBlueDice, [])
            .Select(command => command.CommandId)
            .ToArray();

        // Assert
        new { Pilot = pilotCommandIds, Copilot = copilotCommandIds }
            .Should().BeEquivalentTo(new
            {
                Pilot = new[] { "LandingGear.AssignBlue:4", "LandingGear.AssignBlue:6" },
                Copilot = Array.Empty<string>()
            }, options => options.WithStrictOrdering());
    }
}
