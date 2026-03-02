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

    private static string ResolveWebAppIndexPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "wwwroot", "index.html"));
}
