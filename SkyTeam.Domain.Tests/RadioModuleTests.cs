namespace SkyTeam.Domain.Tests;

using System;
using System.Linq;
using FluentAssertions;

public class RadioModuleTests
{
    [Fact]
    public void CanAcceptBlueDie_ShouldBeTrueOnlyForPilotAndUntilAssigned_WhenRoundNotReset()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new RadioModule(airport);

        // Act
        var before = new
        {
            Pilot = module.CanAcceptBlueDie(Player.Pilot),
            Copilot = module.CanAcceptBlueDie(Player.Copilot)
        };

        module.AssignBlueDie(BlueDie.FromValue(1));

        var afterAssignment = new
        {
            Pilot = module.CanAcceptBlueDie(Player.Pilot),
            Copilot = module.CanAcceptBlueDie(Player.Copilot)
        };

        module.ResetRound();

        var afterReset = new
        {
            Pilot = module.CanAcceptBlueDie(Player.Pilot),
            Copilot = module.CanAcceptBlueDie(Player.Copilot)
        };

        // Assert
        new { Before = before, AfterAssignment = afterAssignment, AfterReset = afterReset }
            .Should().BeEquivalentTo(new
            {
                Before = new { Pilot = true, Copilot = false },
                AfterAssignment = new { Pilot = false, Copilot = false },
                AfterReset = new { Pilot = true, Copilot = false }
            });
    }

    [Fact]
    public void CanAcceptOrangeDie_ShouldBeTrueOnlyForCopilotAndUpToTwoAssignments_WhenRoundNotReset()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new RadioModule(airport);

        // Act
        var before = new
        {
            Pilot = module.CanAcceptOrangeDie(Player.Pilot),
            Copilot = module.CanAcceptOrangeDie(Player.Copilot)
        };

        module.AssignOrangeDie(OrangeDie.FromValue(1));

        var afterFirstAssignment = new
        {
            Pilot = module.CanAcceptOrangeDie(Player.Pilot),
            Copilot = module.CanAcceptOrangeDie(Player.Copilot)
        };

        module.AssignOrangeDie(OrangeDie.FromValue(2));

        var afterSecondAssignment = new
        {
            Pilot = module.CanAcceptOrangeDie(Player.Pilot),
            Copilot = module.CanAcceptOrangeDie(Player.Copilot)
        };

        module.ResetRound();

        var afterReset = new
        {
            Pilot = module.CanAcceptOrangeDie(Player.Pilot),
            Copilot = module.CanAcceptOrangeDie(Player.Copilot)
        };

        // Assert
        new
        {
            Before = before,
            AfterFirstAssignment = afterFirstAssignment,
            AfterSecondAssignment = afterSecondAssignment,
            AfterReset = afterReset
        }
        .Should().BeEquivalentTo(new
        {
            Before = new { Pilot = false, Copilot = true },
            AfterFirstAssignment = new { Pilot = false, Copilot = true },
            AfterSecondAssignment = new { Pilot = false, Copilot = false },
            AfterReset = new { Pilot = false, Copilot = true }
        });
    }

    [Fact]
    public void AssignBlueDie_ShouldRemovePlaneTokenAtOffset_WhenTokenExists()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new RadioModule(airport);

        // Act
        var before = airport.PathSegments[2].PlaneTokens;
        module.AssignBlueDie(BlueDie.FromValue(3));
        var after = airport.PathSegments[2].PlaneTokens;

        // Assert
        new { Before = before, After = after }
            .Should().BeEquivalentTo(new { Before = 1, After = 0 });
    }

    [Fact]
    public void AssignBlueDie_ShouldBeNoOp_WhenTargetSegmentHasNoTokens()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new RadioModule(airport);

        // Act
        var before = airport.PathSegments[0].PlaneTokens;
        module.AssignBlueDie(BlueDie.FromValue(1));
        var after = airport.PathSegments[0].PlaneTokens;

        // Assert
        new { Before = before, After = after }
            .Should().BeEquivalentTo(new { Before = 0, After = 0 });
    }

    [Fact]
    public void AssignBlueDie_ShouldBeNoOpAndNotThrow_WhenOffsetIsBeyondSegments()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        airport.AdvanceApproach(airport.SegmentCount - 1);

        var module = new RadioModule(airport);
        var lastIndex = airport.SegmentCount - 1;
        var before = airport.PathSegments[lastIndex].PlaneTokens;

        // Act
        var exception = Record.Exception(() => module.AssignBlueDie(BlueDie.FromValue(6)));
        var after = airport.PathSegments[lastIndex].PlaneTokens;

        // Assert
        new { Exception = exception, Before = before, After = after }
            .Should().BeEquivalentTo(new { Exception = (Exception?)null, Before = 2, After = 2 });
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldDistinctUnusedBlueValues_WithExpectedCommandIds_WhenPilotHasAvailableSlot()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new RadioModule(airport);

        var unusedBlueDice = new[]
        {
            BlueDie.FromValue(2),
            BlueDie.FromValue(5),
            BlueDie.FromValue(2),
            BlueDie.FromValue(1),
            BlueDie.FromValue(1)
        };

        // Act
        var commandIds = module.GetAvailableCommands(Player.Pilot, unusedBlueDice, [])
            .Select(command => command.CommandId)
            .ToArray();

        // Assert
        commandIds.Should().Equal(["Radio.AssignBlue:1", "Radio.AssignBlue:2", "Radio.AssignBlue:5"]);
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldDistinctUnusedOrangeValues_WithExpectedCommandIds_WhenCopilotHasAvailableSlots()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new RadioModule(airport);

        var unusedOrangeDice = new[]
        {
            OrangeDie.FromValue(6),
            OrangeDie.FromValue(3),
            OrangeDie.FromValue(3),
            OrangeDie.FromValue(1),
            OrangeDie.FromValue(6)
        };

        // Act
        var commandIds = module.GetAvailableCommands(Player.Copilot, [], unusedOrangeDice)
            .Select(command => command.CommandId)
            .ToArray();

        // Assert
        commandIds.Should().Equal(["Radio.AssignOrange:1", "Radio.AssignOrange:3", "Radio.AssignOrange:6"]);
    }
}
