namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue53InGameCockpitButtonFlowTests
{
    [Fact]
    public void RollCallbackPath_ShouldRouteAndUseCockpitEditFlow()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var routesRollCallback = programSource.Contains(
            "RollCallbackData => await HandleInGameRollFromCallbackAsync(",
            StringComparison.Ordinal);
        var usesEditFirstCockpitLifecycle = programSource.Contains(
                                             "if (await TryEditCockpitAsync(botClient, groupChatId, cockpitMessageId, text, replyMarkup, cancellationToken))",
                                             StringComparison.Ordinal) &&
                                         programSource.Contains("await botClient.EditMessageText(", StringComparison.Ordinal);

        // Assert
        routesRollCallback.Should().BeTrue("Roll callback path should be wired in callback dispatcher");
        usesEditFirstCockpitLifecycle.Should().BeTrue("cockpit refresh should try EditMessageText before creating a new message");
    }

    [Fact]
    public void PlaceDmCallback_ShouldRedirectToMiniApp_WhenSecretFlowsAreMiniAppOnly()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var routesPlaceDmCallback = programSource.Contains(
            "PlaceDmCallbackData => await HandleInGamePlaceFromCallbackAsync(",
            StringComparison.Ordinal);
        var hasMiniAppOnlyHint = programSource.Contains(
            "Secret hand/place/undo actions are Mini App-only. Press Open app.",
            StringComparison.Ordinal);

        // Assert
        routesPlaceDmCallback.Should().BeTrue("Place(DM) callback should be wired in callback dispatcher");
        hasMiniAppOnlyHint.Should().BeTrue("Place(DM) callback should enforce Mini App-only secret flows");
    }

    [Fact]
    public void GroupRollFlow_ShouldRedirectToMiniAppWithoutLeakingSecretDiceToGroup()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var hasMiniAppHint = programSource.Contains("Open /sky app to view your private hand", StringComparison.Ordinal);
        var hasLegacyDmWarning = programSource.Contains("Dice rolled, but I couldn't DM:", StringComparison.Ordinal);

        // Assert
        hasMiniAppHint.Should().BeTrue("group users should be directed to the Mini App for secret hand interactions");
        hasLegacyDmWarning.Should().BeFalse("roll fallback should avoid DM-secret delivery warnings");
    }

    [Fact]
    public void GroupCockpitRenderer_ShouldNotContainPrivateHandOrPlacementCommands()
    {
        // Arrange
        var rendererSourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..",
            "SkyTeam.Application", "Presentation", "GroupChatCockpitRenderer.cs"));
        var rendererSource = File.ReadAllText(rendererSourcePath);

        // Act
        var leaksPrivateHand = rendererSource.Contains("/sky hand", StringComparison.Ordinal) ||
                               rendererSource.Contains("secret dice", StringComparison.Ordinal) ||
                               rendererSource.Contains("<dieIndex>", StringComparison.Ordinal);

        // Assert
        leaksPrivateHand.Should().BeFalse("group cockpit text must remain non-secret and DM-only actions stay private");
    }

    [Fact]
    public void KeyboardAndCommandRouter_ShouldKeepRefreshAndSkyRollHandFallbacksCompatible()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var hasInGameRefreshButton = programSource.Contains(
            "InlineKeyboardButton.WithCallbackData(\"Refresh\", RefreshCallbackData)",
            StringComparison.Ordinal);
        var hasGroupRollFallback = programSource.Contains("case \"roll\":", StringComparison.Ordinal) &&
                                   programSource.Contains("HandleSkyRollAsync", StringComparison.Ordinal);
        var hasPrivateHandFallback = programSource.Contains("case \"hand\":", StringComparison.Ordinal) &&
                                     programSource.Contains("HandleSkyHandAsync", StringComparison.Ordinal);

        // Assert
        hasInGameRefreshButton.Should().BeTrue("refresh callback compatibility from previous slices must stay intact");
        hasGroupRollFallback.Should().BeTrue("group fallback /sky roll must remain supported");
        hasPrivateHandFallback.Should().BeTrue("private fallback /sky hand must remain supported");
    }

    [Fact]
    public void SecretFlowFallbackMessaging_ShouldKeepUsersOnGroupLaunchpad_WhenOpenAppLinkIsUnavailable()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var keepsGroupSkyAppFallback = programSource.Contains(
            "Secret hand/place/undo actions are Mini App-only. Use /sky app in your group chat.",
            StringComparison.Ordinal);
        var keepsGroupSkyStateFallback = programSource.Contains(
            "Secret hand/place/undo actions are Mini App-only. Open app link is temporarily unavailable; run /sky state in your group chat and retry.",
            StringComparison.Ordinal);

        // Assert
        keepsGroupSkyAppFallback.Should().BeTrue("fallback guidance should keep players in the group launchpad");
        keepsGroupSkyStateFallback.Should().BeTrue("fallback guidance should keep retries on group cockpit refresh");
    }

    private static string ResolveProgramSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot",
            "TelegramBotService.cs"));
}
