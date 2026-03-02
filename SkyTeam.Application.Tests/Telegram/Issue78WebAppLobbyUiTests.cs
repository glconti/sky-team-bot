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
        var hasGameNameInput = source.Contains("Game name", StringComparison.Ordinal);
        var hasPlayerCountInput = source.Contains("Player count", StringComparison.Ordinal);
        var hasLobbySettingsInput = source.Contains("Lobby settings", StringComparison.Ordinal);
        var hasGameCodeInput = source.Contains("Game code", StringComparison.Ordinal);

        // Assert
        hasPilotPlaceholder.Should().BeTrue("lobby should show pilot placeholder copy");
        hasCopilotPlaceholder.Should().BeTrue("lobby should show copilot placeholder copy");
        hasNewLobby.Should().BeTrue("lobby actions include New Lobby");
        hasJoinLobby.Should().BeTrue("lobby actions include Join Lobby");
        hasStartGame.Should().BeTrue("lobby actions include Start Game");
        hasGameNameInput.Should().BeTrue("create flow should collect game name");
        hasPlayerCountInput.Should().BeTrue("create flow should collect player count");
        hasLobbySettingsInput.Should().BeTrue("create flow should collect optional settings");
        hasGameCodeInput.Should().BeTrue("join flow should collect a game code");
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

    [Fact]
    public void LobbyView_ShouldExposeValidationMessages_ForInvalidCreateAndJoinInput()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasRequiredNameValidation = source.Contains("Game name is required.", StringComparison.Ordinal);
        var hasPlayerCountValidation = source.Contains("Player count must be ${requiredLobbyPlayerCount} (Pilot + Copilot).", StringComparison.Ordinal)
            || source.Contains("Player count must be", StringComparison.Ordinal) && source.Contains("Pilot + Copilot", StringComparison.Ordinal);
        var hasNumericJoinCodeValidation = source.Contains("Enter a numeric game code.", StringComparison.Ordinal);

        // Assert
        hasRequiredNameValidation.Should().BeTrue("create flow should explain required name validation");
        hasPlayerCountValidation.Should().BeTrue("create flow should explain valid player count");
        hasNumericJoinCodeValidation.Should().BeTrue("join flow should explain code validation");
    }

    private static string ResolveWebAppIndexPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "wwwroot", "index.html"));
}
