namespace SkyTeam.Application.Tests.Telegram;

using FluentAssertions;

public sealed class Issue78WebAppLobbyUiTests
{
    [Fact]
    public void LobbyView_ShouldExposeSeatPlaceholdersAndActions_ForMiniAppLobbyUi()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasPilotPlaceholder = source.Contains("Waiting for Pilot", StringComparison.Ordinal);
        var hasCopilotPlaceholder = source.Contains("Waiting for Copilot", StringComparison.Ordinal);
        var hasNewLobby = source.Contains("New Lobby", StringComparison.Ordinal);
        var hasJoinLobby = source.Contains("Join Lobby", StringComparison.Ordinal);
        var hasStartGame = source.Contains("Start Game", StringComparison.Ordinal);

        // Assert
        hasPilotPlaceholder.Should().BeTrue("lobby should show pilot placeholder copy");
        hasCopilotPlaceholder.Should().BeTrue("lobby should show copilot placeholder copy");
        hasNewLobby.Should().BeTrue("lobby actions include New Lobby");
        hasJoinLobby.Should().BeTrue("lobby actions include Join Lobby");
        hasStartGame.Should().BeTrue("lobby actions include Start Game");
    }

    [Fact]
    public void LobbyView_ShouldTruncateDisplayNames_ToTelegramLimit()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var declaresMaxLength = source.Contains("maxDisplayNameLength = 32", StringComparison.Ordinal);
        var usesTruncation = source.Contains("truncateDisplayName", StringComparison.Ordinal);

        // Assert
        declaresMaxLength.Should().BeTrue("display names should be truncated to Telegram's 32 char limit");
        usesTruncation.Should().BeTrue("lobby UI should truncate long names");
    }

    private static string ResolveWebAppIndexPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "wwwroot", "index.html"));
}
