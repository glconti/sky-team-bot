namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue83AsyncTurnNotificationTests
{
    [Fact]
    public void NotificationFlow_ShouldBridgeRollPlaceUndoTransitions_WhenGroupAndWebAppActionsSucceed()
    {
        // Arrange
        var serviceSource = File.ReadAllText(ResolveTelegramBotServiceSourcePath());
        var endpointSource = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var groupRollNotifiesCurrentTurn = serviceSource.Contains(
            "await NotifyCurrentTurnAsync(",
            StringComparison.Ordinal);
        var webAppRollNotifiesCurrentTurn = endpointSource.Contains(
            "transitionKey: $\"roll:{rollResult.Snapshot?.Round.RoundNumber ?? 0}\"",
            StringComparison.Ordinal);
        var webAppPlacementNotifiesCurrentTurn = endpointSource.Contains(
            "transitionKey: $\"place:{state.Session.Round.RoundNumber}:{placement.PublicInfo!.PlacementIndex}\"",
            StringComparison.Ordinal);
        var webAppUndoNotifiesCurrentTurn = endpointSource.Contains(
            "transitionKey: $\"undo:{state.Session.Round.RoundNumber}:{undo.PublicInfo!.UndonePlacementIndex}\"",
            StringComparison.Ordinal);

        // Assert
        groupRollNotifiesCurrentTurn.Should().BeTrue("group roll transitions should notify the active player");
        webAppRollNotifiesCurrentTurn.Should().BeTrue("webapp roll transitions should notify the active player");
        webAppPlacementNotifiesCurrentTurn.Should().BeTrue("webapp placement transitions should notify the active player");
        webAppUndoNotifiesCurrentTurn.Should().BeTrue("webapp undo transitions should notify the active player");
    }

    [Fact]
    public void NotificationDedup_ShouldResetForGroup_WhenANewGameStarts()
    {
        // Arrange
        var source = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var callbackStartResetsGroupDedup = source.Contains(
            "ForgetTurnNotificationKeysForGroup(groupChatId);",
            StringComparison.Ordinal);
        var commandStartResetsGroupDedup = source.Contains(
            "ForgetTurnNotificationKeysForGroup(message.Chat.Id);",
            StringComparison.Ordinal);

        // Assert
        callbackStartResetsGroupDedup.Should().BeTrue("starting a new game from callbacks should clear stale dedup keys");
        commandStartResetsGroupDedup.Should().BeTrue("starting a new game from commands should clear stale dedup keys");
    }

    [Fact]
    public void NotificationFallback_ShouldRemainPublicAndBestEffort_WhenDirectMessageFails()
    {
        // Arrange
        var source = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var usesSafeFallbackSender = source.Contains(
            "await TrySendGroupTurnFallbackAsync(botClient, groupChatId, recipient, seat, cancellationToken);",
            StringComparison.Ordinal);
        var fallbackTextIsActionOnly = source.Contains(
            "$\"🔔 {recipientDisplayName} ({seat}), your turn. Open /sky app and place one die.\"",
            StringComparison.Ordinal);
        var fallbackFailureIsLogged = source.Contains(
            "logger.LogWarning(",
            StringComparison.Ordinal);

        // Assert
        usesSafeFallbackSender.Should().BeTrue("fallback sends should be routed through a safe helper");
        fallbackTextIsActionOnly.Should().BeTrue("fallback content should stay public and non-secret");
        fallbackFailureIsLogged.Should().BeTrue("fallback delivery failures should not break gameplay paths");
    }

    private static string ResolveTelegramBotServiceSourcePath()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SkyTeam.TelegramBot",
            "TelegramBotService.cs"));

    private static string ResolveWebAppEndpointsSourcePath()
        => Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "SkyTeam.TelegramBot",
            "WebApp",
            "WebAppEndpoints.cs"));
}
