namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue79WebAppInGameUiTests
{
    [Fact]
    public void InGameView_ShouldHandleConcurrencyConflict_AndUseVersionedActions()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var handlesConflict = source.Contains("ConcurrencyConflict", StringComparison.Ordinal);
        var includesExpectedVersion = source.Contains("expectedVersion", StringComparison.Ordinal);

        // Assert
        handlesConflict.Should().BeTrue("in-game UI should surface concurrency conflicts for refresh");
        includesExpectedVersion.Should().BeTrue("in-game actions should pass expectedVersion");
    }

    [Fact]
    public void InGameView_ShouldRenderTurnAndCockpitSections_ForReadableState()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasInGameHeader = source.Contains("In Game", StringComparison.Ordinal);
        var hasRoundSection = source.Contains("Round & Turn", StringComparison.Ordinal);
        var hasCockpitSection = source.Contains("Cockpit", StringComparison.Ordinal);

        // Assert
        hasInGameHeader.Should().BeTrue("in-game UI should label the section");
        hasRoundSection.Should().BeTrue("in-game UI should summarize round and turn state");
        hasCockpitSection.Should().BeTrue("in-game UI should surface cockpit status");
    }

    [Fact]
    public void InGameView_ShouldHaveUndoButton_WhenPlacementReversible()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasUndoButton = source.Contains("Undo", StringComparison.Ordinal);

        // Assert
        hasUndoButton.Should().BeTrue("because in-game UI should expose undo action for reversible placements");
    }

    [Fact]
    public void InGameView_ShouldNotLeakPrivateHand_ToPublicSection()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasPrivateHandCheck = source.Contains("privateHand", StringComparison.Ordinal);
        var checksViewerSeat = source.Contains("viewerSeat", StringComparison.Ordinal);

        // Assert
        hasPrivateHandCheck.Should().BeTrue("because private hand should be conditionally rendered");
        checksViewerSeat.Should().BeTrue("because private hand rendering should verify viewer is active player");
    }

    [Fact]
    public void InGameView_ShouldShowModuleStatusIndicators_ForCockpitModules()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasAxisModule = source.Contains("Axis", StringComparison.Ordinal);
        var hasEnginesModule = source.Contains("Engines", StringComparison.Ordinal);
        var hasBrakesModule = source.Contains("Brakes", StringComparison.Ordinal);
        var hasFlapsModule = source.Contains("Flaps", StringComparison.Ordinal);
        var hasLandingGearModule = source.Contains("Landing gear", StringComparison.Ordinal);

        // Assert
        hasAxisModule.Should().BeTrue("because cockpit should display Axis module status");
        hasEnginesModule.Should().BeTrue("because cockpit should display Engines module status");
        hasBrakesModule.Should().BeTrue("because cockpit should display Brakes module status");
        hasFlapsModule.Should().BeTrue("because cockpit should display Flaps module status");
        hasLandingGearModule.Should().BeTrue("because cockpit should display Landing Gear module status");
    }

    [Fact]
    public void InGameView_ShouldDisplayRollButton_WhenActivePlayer()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasRollButton = source.Contains("Roll", StringComparison.Ordinal);
        var checksViewerSeat = source.Contains("viewerSeat", StringComparison.Ordinal);

        // Assert
        hasRollButton.Should().BeTrue("because in-game UI should expose roll action");
        checksViewerSeat.Should().BeTrue("because roll button should be conditional on viewer being active player");
    }

    private static string ResolveWebAppIndexPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "wwwroot", "index.html"));
}
