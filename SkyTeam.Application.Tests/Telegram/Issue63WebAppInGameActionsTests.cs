namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue63WebAppInGameActionsTests
{
    [Fact]
    public void RollCallbackPath_ShouldRouteThroughGroupRollAndRefreshPipeline()
    {
        // Arrange
        var source = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var routesRollCallback = source.Contains("RollCallbackData => await HandleInGameRollFromCallbackAsync(", StringComparison.Ordinal);
        var callbackCallsGroupRoll = source.Contains("await HandleGroupRollAsync(botClient, groupChatId, cancellationToken);", StringComparison.Ordinal);
        var rollRefreshesCockpit = source.Contains("await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);", StringComparison.Ordinal);

        // Assert
        routesRollCallback.Should().BeTrue("issue #63 needs Roll callback dispatch from cockpit actions");
        callbackCallsGroupRoll.Should().BeTrue("Roll callback should reuse group roll flow");
        rollRefreshesCockpit.Should().BeTrue("rolling must propagate cockpit refresh");
    }

    [Fact]
    public void WebAppActionEndpoints_ShouldUseCockpitRefreshBridge()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var usesWebAppRefreshBridge = source.Contains(
                                        "await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId.Value, cancellationToken);",
                                        StringComparison.Ordinal);

        // Assert
        usesWebAppRefreshBridge.Should().BeTrue("WebApp actions should propagate cockpit updates via TelegramBotService refresh bridge");
    }

    [Fact]
    public void GameStateEndpoint_ShouldKeepPrivateHandBoundariesForSeatedUsersOnly()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var readsHandForAuthenticatedViewer = source.Contains(
            "gameSessionStore.GetHand(result.GroupChatId.Value, result.Context.Viewer.UserId)",
            StringComparison.Ordinal);
        var mapsNullWhenHandIsNotAccessible = source.Contains(
            "if (handResult.Status != GameHandStatus.Ok",
            StringComparison.Ordinal);

        // Assert
        readsHandForAuthenticatedViewer.Should().BeTrue("private hand should be resolved by authenticated viewer identity");
        mapsNullWhenHandIsNotAccessible.Should().BeTrue("non-seated or not-rolled contexts must not leak private hand payload");
    }

    [Fact]
    public void WebAppInGameActions_ShouldExposeRollAction_AndReturnUpdatedState()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var mapsRollEndpoint = source.Contains("group.MapPost(\"/game/roll\", RollGame);", StringComparison.Ordinal);
        var registersRoundRoll = source.Contains("gameSessionStore.RegisterRoll(", StringComparison.Ordinal);
        var returnsUpdatedState = source.Contains("return Results.Ok(MapStateResponse(", StringComparison.Ordinal);

        // Assert
        mapsRollEndpoint.Should().BeTrue("issue #63 requires a dedicated Mini App roll action endpoint");
        registersRoundRoll.Should().BeTrue("roll action must mutate session state by registering round dice");
        returnsUpdatedState.Should().BeTrue("roll action should return updated game-state payload");
    }

    [Fact]
    public void SkyRollFallback_ShouldRedirectToMiniApp_WithoutDirectMessageSecretPayloads()
    {
        // Arrange
        var source = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var hasMiniAppRollGuidance = source.Contains("Dice rolled. Open /sky app to view your private hand and continue.", StringComparison.Ordinal);
        var hasAlreadyRolledMiniAppGuidance = source.Contains("This round has already been rolled. Open /sky app to view your private hand and place dice.", StringComparison.Ordinal);
        var usesRollDmDelivery = source.Contains("TrySendSecretDiceAsync(botClient", StringComparison.Ordinal);

        // Assert
        hasMiniAppRollGuidance.Should().BeTrue("group /sky roll should direct seated players to Mini App private hand flow");
        hasAlreadyRolledMiniAppGuidance.Should().BeTrue("repeat /sky roll should keep users on Mini App flow");
        usesRollDmDelivery.Should().BeFalse("group roll fallback must not DM secret dice payloads");
    }

    private static string ResolveTelegramBotServiceSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "TelegramBotService.cs"));

    private static string ResolveWebAppEndpointsSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "WebApp", "WebAppEndpoints.cs"));
}
