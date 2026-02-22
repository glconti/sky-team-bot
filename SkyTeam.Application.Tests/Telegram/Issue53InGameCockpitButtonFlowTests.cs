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
    public void PlaceDmCallback_ShouldShowOnboardingHint_WhenPrivateChatIsUnavailable()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var routesPlaceDmCallback = programSource.Contains(
            "PlaceDmCallbackData => await HandleInGamePlaceFromCallbackAsync(",
            StringComparison.Ordinal);
        var triesSendingDm = programSource.Contains(
            "TrySendDirectMessageAsync(botClient, user.Id, text, cancellationToken)",
            StringComparison.Ordinal);
        var hasOnboardingHint = programSource.Contains(
            "Open a private chat with me and send /start, then press Place (DM) again.",
            StringComparison.Ordinal);

        // Assert
        routesPlaceDmCallback.Should().BeTrue("Place(DM) callback should be wired in callback dispatcher");
        triesSendingDm.Should().BeTrue("Place(DM) path should send private hand details via DM");
        hasOnboardingHint.Should().BeTrue("failed DM delivery should explain onboarding step");
    }

    [Fact]
    public void GroupRollFlow_ShouldUseDmOnboardingHintWithoutLeakingSecretDiceToGroup()
    {
        // Arrange
        var programSource = File.ReadAllText(ResolveProgramSourcePath());

        // Act
        var hasOnboardingHint = programSource.Contains("must /start me in a private chat first.", StringComparison.Ordinal);
        var hasNonLeakingGroupWarning = programSource.Contains(
            "Dice rolled, but I couldn't DM: {string.Join(\", \", failedRecipients)}. Each seated player must /start me in a private chat first.",
            StringComparison.Ordinal);

        // Assert
        hasOnboardingHint.Should().BeTrue("group users should be told how to onboard DM when roll DM delivery fails");
        hasNonLeakingGroupWarning.Should().BeTrue("group warning must not reveal secret dice payload");
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

    private static string ResolveProgramSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot",
            "TelegramBotService.cs"));
}
