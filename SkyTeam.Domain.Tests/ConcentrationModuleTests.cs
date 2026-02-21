using System.Linq;
using FluentAssertions;

namespace SkyTeam.Domain.Tests;

public class ConcentrationModuleTests
{
    [Fact]
    public void CanAcceptDie_ShouldBeFalseForPilotAndCopilot_WhenTwoPlacementsDoneInRound()
    {
        // Arrange
        var module = new ConcentrationModule();

        // Act
        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        var canAccept = new
        {
            Blue = module.CanAcceptBlueDie(Player.Pilot),
            Orange = module.CanAcceptOrangeDie(Player.Copilot)
        };

        // Assert
        canAccept.Should().BeEquivalentTo(new { Blue = false, Orange = false });
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
    public void GetAvailableCommands_ShouldYieldNone_WhenSlotsAreFull()
    {
        // Arrange
        var module = new ConcentrationModule();

        module.AssignBlueDie(BlueDie.FromValue(1));
        module.AssignOrangeDie(OrangeDie.FromValue(1));

        // Act
        var commands = module.GetAvailableCommands(Player.Pilot, [BlueDie.FromValue(1)], [])
            .ToArray();

        // Assert
        commands.Should().BeEmpty();
    }
}
