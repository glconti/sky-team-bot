namespace SkyTeam.Application.Tests.Telegram;

using System.Reflection;
using FluentAssertions;
using global::Telegram.Bot.Types.ReplyMarkups;

public sealed class Issue52LobbyButtonFlowTests
{
    [Fact]
    public void BuildGroupStateKeyboard_ShouldExposeRefreshCallback_WhenLobbyCockpitIsRendered()
    {
        // Arrange
        var refreshCallbackData = GetPrivateConst("RefreshCallbackData");
        var buildKeyboard = GetPrivateMethod("BuildGroupStateKeyboard");

        // Act
        var keyboard = (InlineKeyboardMarkup)buildKeyboard.Invoke(null, System.Array.Empty<object>())!;
        var buttons = keyboard.InlineKeyboard.SelectMany(row => row).ToList();

        // Assert
        buttons.Should().ContainSingle(button =>
            button.Text == "Refresh" && button.CallbackData == refreshCallbackData);
    }

    [Fact(Skip =
        "Issue #52 is partially implemented: callback routing currently handles only refresh; add New/Join/Start callback dispatch once dedicated callback payloads and routing are introduced.")]
    public void LobbyCallbacks_ShouldRouteNewJoinStartCallbackPaths_ThroughExistingHandlers()
    {
    }

    [Fact(Skip =
        "Issue #52 is partially implemented: invalid callback press shows toast, but explicit no-op side-effect assertions require callback handler extraction/test doubles.")]
    public void InvalidPress_ShouldBeNoOpAndShowToast_WhenCallbackPayloadIsUnknownOrExpired()
    {
    }

    [Fact(Skip =
        "Issue #52 is partially implemented: successful callbacks must integrate with HandleSkyNew/Join/Start and edit cockpit after those callback paths are implemented.")]
    public void SuccessfulLobbyCallbacks_ShouldIntegrateWithExistingHandlers_AndEditCockpit()
    {
    }

    [Fact]
    public void SkyCommandFallback_ShouldRemainValid_WhenCallbackCannotProceed()
    {
        // Arrange
        var expiredMenuToast = GetPrivateConst("ExpiredMenuToast");
        var programSourcePath = ResolveTelegramBotServiceSourcePath();
        var source = File.ReadAllText(programSourcePath);

        // Act
        var includesSkyStateFallback = expiredMenuToast.Contains("/sky state", StringComparison.Ordinal);
        var includesSkyStateCommand = source.Contains("case \"state\":", StringComparison.Ordinal) &&
                                      source.Contains("HandleSkyStateAsync", StringComparison.Ordinal);

        // Assert
        includesSkyStateFallback.Should().BeTrue("expired callback toast should point to /sky state");
        includesSkyStateCommand.Should().BeTrue("group command fallback must continue to support /sky state");
    }

    private static string GetPrivateConst(string name)
    {
        var field = TelegramBotServiceType.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();
        return (string)field!.GetRawConstantValue()!;
    }

    private static MethodInfo GetPrivateMethod(string name)
    {
        var method = TelegramBotServiceType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        return method!;
    }

    private static string ResolveTelegramBotServiceSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot",
            "TelegramBotService.cs"));

    private static Type TelegramBotServiceType
        => Assembly.Load("SkyTeam.TelegramBot").GetType("SkyTeam.TelegramBot.TelegramBotService", throwOnError: true)!;
}
