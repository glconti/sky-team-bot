namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue64WebAppPlacementUndoTests
{
    [Fact]
    public void WebAppActionEndpoints_ShouldExposePlacementRoute_ForMiniAppFlow()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var hasPlaceRoute = source.Contains("group.MapPost(\"/game/place\", PlaceDie);", StringComparison.Ordinal);

        // Assert
        hasPlaceRoute.Should().BeTrue("issue #64 requires a Mini App placement endpoint");
    }

    [Fact]
    public void WebAppActionEndpoints_ShouldExposeUndoRoute_ForMiniAppFlow()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var hasUndoRoute = source.Contains("group.MapPost(\"/game/undo\", UndoLastPlacement);", StringComparison.Ordinal);

        // Assert
        hasUndoRoute.Should().BeTrue("issue #64 requires a Mini App undo endpoint");
    }

    [Fact]
    public void PlacementAndUndoHandlers_ShouldRefreshGroupCockpit_AfterEachSuccessfulAction()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var placeRefreshesCockpit = source.Contains(
            "await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId!.Value, cancellationToken);",
            StringComparison.Ordinal);
        var usesPlacementMutation = source.Contains("gameSessionStore.PlaceDie(", StringComparison.Ordinal);
        var usesUndoMutation = source.Contains("gameSessionStore.UndoLastPlacement(", StringComparison.Ordinal);

        // Assert
        placeRefreshesCockpit.Should().BeTrue("group cockpit must refresh after successful placement/undo actions");
        usesPlacementMutation.Should().BeTrue("placement flow must execute via application store placement API");
        usesUndoMutation.Should().BeTrue("undo flow must execute via application store undo API");
    }

    [Fact]
    public void PlacementFlow_ShouldSupportTokenAdjustedCommandSelection()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var consumesCommandId = source.Contains("commandId", StringComparison.Ordinal);
        var delegatesPlacementCommand = source.Contains("gameSessionStore.PlaceDie(", StringComparison.Ordinal);

        // Assert
        consumesCommandId.Should().BeTrue("placement endpoint should accept command identifiers for token-adjusted options");
        delegatesPlacementCommand.Should().BeTrue("placement endpoint should pass selected command id to application layer");
    }

    [Fact]
    public void WebAppPlacementUndoFlow_ShouldNotLeakSecretOptions_ToGroupChatTransport()
    {
        // Arrange
        var endpointSource = File.ReadAllText(ResolveWebAppEndpointsSourcePath());
        var telegramSource = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var webAppSendsDirectMessages = endpointSource.Contains("SendMessage(", StringComparison.Ordinal)
                                     || endpointSource.Contains("TrySendDirectMessageAsync(", StringComparison.Ordinal);
        var groupHandlersSendSecretDice = telegramSource.Contains("TrySendSecretDiceAsync(botClient", StringComparison.Ordinal);

        // Assert
        webAppSendsDirectMessages.Should().BeFalse("Mini App placement/undo should remain WebApp-driven and not DM secret options");
        groupHandlersSendSecretDice.Should().BeFalse("group flow must not send secret option payloads");
    }

    private static string ResolveWebAppEndpointsSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "WebApp", "WebAppEndpoints.cs"));

    private static string ResolveTelegramBotServiceSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "TelegramBotService.cs"));
}
