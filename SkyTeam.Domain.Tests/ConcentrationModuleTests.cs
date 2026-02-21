using System.Linq;
using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class ConcentrationModuleTests
{
    [Fact]
    public void CanAcceptDice_ShouldAllowPilotBlueAndCopilotOrangeOnly_WhenSlotsRemain()
    {
        // Arrange
        var module = new ConcentrationModule();

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
    public void CanAcceptDice_ShouldBeFalseForBothPlayers_WhenTwoPlacementsDoneInRound()
    {
        // Arrange
        var module = new ConcentrationModule();

        // Act
        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

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
            PilotBlue = false,
            CopilotBlue = false,
            PilotOrange = false,
            CopilotOrange = false
        });
    }

    [Fact]
    public void ResetRound_ShouldClearCapacityButPreserveTokenPool_WhenCalled()
    {
        // Arrange
        var module = new ConcentrationModule();

        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        // Act
        module.ResetRound();

        var afterReset = new
        {
            TokenCount = module.TokenPool.Count,
            CanAcceptBlue = module.CanAcceptBlueDie(Player.Pilot),
            CanAcceptOrange = module.CanAcceptOrangeDie(Player.Copilot)
        };

        // Assert
        afterReset.Should().BeEquivalentTo(new { TokenCount = 2, CanAcceptBlue = true, CanAcceptOrange = true });
    }

    [Fact]
    public void AssignDie_ShouldEarnOneTokenPerPlacementCappedAtThree_WhenPlacedAcrossMultipleRounds()
    {
        // Arrange
        var module = new ConcentrationModule();

        // Act
        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        module.ResetRound();

        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        // Assert
        module.TokenPool.Count.Should().Be(3);
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldOnePerDistinctUnusedBlueValue_WithExpectedCommandIds_WhenPilotAndSlotsRemain()
    {
        // Arrange
        var module = new ConcentrationModule();

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
        commandIds.Should().Equal(["Concentration.AssignBlue:1", "Concentration.AssignBlue:2", "Concentration.AssignBlue:5"]);
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldOnePerDistinctUnusedOrangeValue_WithExpectedCommandIds_WhenCopilotAndSlotsRemain()
    {
        // Arrange
        var module = new ConcentrationModule();

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
        commandIds.Should().Equal(["Concentration.AssignOrange:1", "Concentration.AssignOrange:3", "Concentration.AssignOrange:6"]);
    }

    [Fact]
    public void GetAvailableCommands_ShouldBeEmpty_WhenNoUnusedDiceOfCurrentPlayersColor()
    {
        // Arrange
        var module = new ConcentrationModule();

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, [], [OrangeDie.FromValue(1)]).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, [BlueDie.FromValue(1)], []).ToArray();

        // Assert
        pilotCommands.Should().BeEmpty();
        copilotCommands.Should().BeEmpty();
    }

    [Fact]
    public void GetAvailableCommands_ShouldYieldNone_WhenSlotsAreFull()
    {
        // Arrange
        var module = new ConcentrationModule();

        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        // Act
        var pilotCommands = module.GetAvailableCommands(Player.Pilot, [BlueDie.FromValue(1)], []).ToArray();
        var copilotCommands = module.GetAvailableCommands(Player.Copilot, [], [OrangeDie.FromValue(1)]).ToArray();

        // Assert
        pilotCommands.Should().BeEmpty();
        copilotCommands.Should().BeEmpty();
    }
}
