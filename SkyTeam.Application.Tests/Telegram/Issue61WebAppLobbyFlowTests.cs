namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue61WebAppLobbyFlowTests
{
    [Fact]
    public void SkyCommandFallback_ShouldKeepNewJoinStartRoutes_ForMiniAppLobbyParity()
    {
        // Arrange
        var source = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var routesNew = source.Contains("case \"new\":", StringComparison.Ordinal)
                        && source.Contains("await HandleSkyNewAsync(botClient, message, cancellationToken);", StringComparison.Ordinal);
        var routesJoin = source.Contains("case \"join\":", StringComparison.Ordinal)
                         && source.Contains("await HandleSkyJoinAsync(botClient, message, cancellationToken);", StringComparison.Ordinal);
        var routesStart = source.Contains("case \"start\":", StringComparison.Ordinal)
                          && source.Contains("await HandleSkyStartAsync(botClient, message, cancellationToken);", StringComparison.Ordinal);

        // Assert
        routesNew.Should().BeTrue("issue #61 keeps /sky new fallback");
        routesJoin.Should().BeTrue("issue #61 keeps /sky join fallback");
        routesStart.Should().BeTrue("issue #61 keeps /sky start fallback");
    }

    [Fact]
    public void LobbyMutations_ShouldRefreshAndEditCockpit_WhenActionsSucceed()
    {
        // Arrange
        var source = File.ReadAllText(ResolveTelegramBotServiceSourcePath());

        // Act
        var callbackMutationsRefresh = source.Contains("private async Task<string?> HandleLobbyNewFromCallbackAsync(", StringComparison.Ordinal)
                                       && source.Contains("private async Task<string?> HandleLobbyJoinFromCallbackAsync(", StringComparison.Ordinal)
                                       && source.Contains("private async Task<string?> HandleLobbyStartFromCallbackAsync(", StringComparison.Ordinal)
                                       && source.Contains("await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);", StringComparison.Ordinal);
        var refreshUsesEditFirst = source.Contains(
                                       "if (await TryEditCockpitAsync(botClient, groupChatId, cockpitMessageId, text, replyMarkup, cancellationToken))",
                                       StringComparison.Ordinal)
                                  && source.Contains("await botClient.EditMessageText(", StringComparison.Ordinal);

        // Assert
        callbackMutationsRefresh.Should().BeTrue("lobby actions should trigger cockpit refresh after successful mutation");
        refreshUsesEditFirst.Should().BeTrue("cockpit refresh should prefer edit-in-place updates");
    }

    [Fact(Skip = "Issue #61 pending implementation: POST /api/webapp/lobby/new endpoint contract")]
    public void WebAppLobbyNew_ShouldCreateLobby_ViaBackendEndpoint()
    {
    }

    [Fact(Skip = "Issue #61 pending implementation: POST /api/webapp/lobby/join endpoint contract")]
    public void WebAppLobbyJoin_ShouldSeatViewer_ViaBackendEndpoint()
    {
    }

    [Fact(Skip = "Issue #61 pending implementation: POST /api/webapp/lobby/start endpoint contract")]
    public void WebAppLobbyStart_ShouldStartSession_ViaBackendEndpoint()
    {
    }

    [Fact(Skip = "Issue #61 pending implementation: webapp action success should refresh/edit group cockpit")]
    public void WebAppLobbyActions_ShouldRefreshGroupCockpit_AfterSuccessfulMutations()
    {
    }

    private static string ResolveTelegramBotServiceSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot",
            "TelegramBotService.cs"));
}
