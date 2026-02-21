namespace SkyTeam.Domain.Tests;

using System.Linq;
using FluentAssertions;

public class EnginesModuleTests
{
    [Fact]
    public void CanAcceptDice_ShouldAllowPilotBlueAndCopilotOrangeOnly_WhenSlotsAreEmpty()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

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
    public void AssignDice_ShouldNotAdvanceApproach_WhenSpeedIsBelowBlueThreshold()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

        // Act
        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Assert
        module.LastSpeed.Should().Be(4);
        airport.CurrentPositionIndex.Should().Be(0);
    }

    [Fact]
    public void AssignDice_ShouldAdvanceApproachByOne_WhenSpeedIsBetweenBlueAndOrangeThresholds()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

        // Act
        module.AssignBlueDie(BlueDie.FromValue(4));
        module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Assert
        module.LastSpeed.Should().Be(7);
        airport.CurrentPositionIndex.Should().Be(1);
    }

    [Fact]
    public void AssignDice_ShouldAdvanceApproachByTwo_WhenSpeedIsAboveOrangeThreshold()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

        // Act
        module.AssignBlueDie(BlueDie.FromValue(6));
        module.AssignOrangeDie(OrangeDie.FromValue(6));

        // Assert
        module.LastSpeed.Should().Be(12);
        airport.CurrentPositionIndex.Should().Be(2);
    }

    [Fact]
    public void AssignDice_ShouldThrow_WhenAdvancingFromCurrentPositionWithAirplanes()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

        // Reach a segment with airplanes (allowed).
        module.AssignBlueDie(BlueDie.FromValue(6));
        module.AssignOrangeDie(OrangeDie.FromValue(6));

        airport.CurrentSegment.PlaneTokens.Should().BeGreaterThan(0);

        module.ResetRound();

        // Act
        var resolving = () =>
        {
            module.AssignBlueDie(BlueDie.FromValue(4));
            module.AssignOrangeDie(OrangeDie.FromValue(3));
        };

        // Assert
        resolving.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot advance approach with airplanes at current position.");
    }

    [Fact]
    public void AssignDice_ShouldThrow_WhenApproachWouldOvershoot()
    {
        // Arrange
        var airport = new Airport([new PathSegment(0)]);
        var module = new EnginesModule(airport);

        // Act
        var resolving = () =>
        {
            module.AssignBlueDie(BlueDie.FromValue(4));
            module.AssignOrangeDie(OrangeDie.FromValue(3));
        };

        // Assert
        resolving.Should().Throw<InvalidOperationException>()
            .WithMessage("Approach overshoot.");
    }

    [Fact]
    public void ResetRound_ShouldClearDiceAssignmentsAndLastSpeed_WhenCalled()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

        module.AssignBlueDie(BlueDie.FromValue(4));
        module.AssignOrangeDie(OrangeDie.FromValue(3));

        // Act
        module.ResetRound();

        // Assert
        new
        {
            CanAcceptBlue = module.CanAcceptBlueDie(Player.Pilot),
            CanAcceptOrange = module.CanAcceptOrangeDie(Player.Copilot),
            module.LastSpeed
        }
        .Should().BeEquivalentTo(new { CanAcceptBlue = true, CanAcceptOrange = true, LastSpeed = (int?)null });
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenNoUnusedDiceOfCurrentPlayersColor()
    {
        // Arrange
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

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
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);

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
        var airport = (Airport)new MontrealAirport();
        var module = new EnginesModule(airport);
        var unusedBlueDice = new[] { BlueDie.FromValue(5), BlueDie.FromValue(2), BlueDie.FromValue(5) };
        var unusedOrangeDice = new[] { OrangeDie.FromValue(6), OrangeDie.FromValue(1), OrangeDie.FromValue(6) };

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, unusedBlueDice, unusedOrangeDice, new CoffeeTokenPool()).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, unusedBlueDice, unusedOrangeDice, new CoffeeTokenPool()).ToArray();

        // Assert
        pilotCommands.Select(c => c.CommandId).Should().Equal(["Engines.AssignBlue:2", "Engines.AssignBlue:5"]);
        copilotCommands.Select(c => c.CommandId).Should().Equal(["Engines.AssignOrange:1", "Engines.AssignOrange:6"]);
    }
}
