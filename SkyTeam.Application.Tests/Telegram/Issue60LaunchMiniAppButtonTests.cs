using System.Reflection;
using FluentAssertions;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace SkyTeam.Application.Tests.Telegram;

public sealed class Issue60LaunchMiniAppButtonTests
{
    [Fact]
    public void BuildGroupStateKeyboard_ShouldIncludeOpenAppStartAppUrl_WhenBotUsernameIsKnown()
    {
        // Arrange
        var type = ResolveBotServiceTypeOrSkip();
        var buildKeyboard = type.GetMethod(
            "BuildGroupStateKeyboard",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildKeyboard.Should().NotBeNull("Slice #60 expects an Open app button in the group cockpit keyboard");

        var groupChatId = 123L;
        var botUsername = "sky_team_bot";
        var miniAppShortName = "skyteam";

        // Act
        var keyboard = (InlineKeyboardMarkup?)buildKeyboard!.Invoke(null, [groupChatId, botUsername, miniAppShortName]);

        // Assert
        keyboard.Should().NotBeNull();

        var buttons = keyboard!.InlineKeyboard.SelectMany(row => row).ToArray();
        buttons.Should().Contain(b => b.Url == "https://t.me/sky_team_bot/skyteam?startapp=123");
        buttons.Should().NotContain(b => b.WebApp != null);
    }

    [Fact]
    public void BuildGroupStateKeyboard_ShouldTrimAtPrefixAndSupportNegativeChatIds_WhenGeneratingDeepLinks()
    {
        // Arrange
        var type = ResolveBotServiceTypeOrSkip();
        var buildKeyboard = type.GetMethod(
            "BuildGroupStateKeyboard",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildKeyboard.Should().NotBeNull();

        var groupChatId = -123L;
        var botUsername = "@sky_team_bot";

        // Act
        var keyboard = (InlineKeyboardMarkup?)buildKeyboard!.Invoke(null, [groupChatId, botUsername, null]);

        // Assert
        keyboard.Should().NotBeNull();

        var buttons = keyboard!.InlineKeyboard.SelectMany(row => row).ToArray();
        buttons.Should().Contain(b => b.Url == "https://t.me/sky_team_bot?startapp=-123");
    }

    [Fact]
    public void BuildGroupStateKeyboard_ShouldHideOpenAppButton_WhenBotUsernameIsInvalid()
    {
        // Arrange
        var type = ResolveBotServiceTypeOrSkip();
        var buildKeyboard = type.GetMethod(
            "BuildGroupStateKeyboard",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildKeyboard.Should().NotBeNull();

        // Act
        var keyboard = (InlineKeyboardMarkup?)buildKeyboard!.Invoke(null, [123L, "sky team bot", "https://example.test/"]);

        // Assert
        keyboard.Should().NotBeNull();

        var buttons = keyboard!.InlineKeyboard.SelectMany(row => row).ToArray();
        buttons.Should().NotContain(b => b.Text == "Open app");
        buttons.Should().Contain(b => b.Text == "Refresh" && b.CallbackData != null);
    }

    [Fact]
    public void BuildGroupStateKeyboard_ShouldKeepOpenAppButtonPersistent_WhenCockpitIsRefreshedRepeatedly()
    {
        // Arrange
        var type = ResolveBotServiceTypeOrSkip();
        var buildKeyboard = type.GetMethod(
            "BuildGroupStateKeyboard",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildKeyboard.Should().NotBeNull();

        var groupChatId = -1001234567890L;
        var botUsername = "sky_team_bot";

        // Act
        for (var refreshCount = 0; refreshCount < 30; refreshCount++)
        {
            var keyboard = (InlineKeyboardMarkup?)buildKeyboard!.Invoke(null, [groupChatId, botUsername, null]);
            var buttons = keyboard!.InlineKeyboard.SelectMany(row => row).ToArray();

            // Assert
            buttons.Should().Contain(button => button.Text == "Open app" && button.Url == "https://t.me/sky_team_bot?startapp=-1001234567890");
            buttons.Should().Contain(button => button.Text == "Refresh" && button.CallbackData != null);
        }
    }

    [Fact]
    public void BuildGroupStateKeyboard_ShouldFallbackToPrimaryStartAppLink_WhenMiniAppShortNameIsInvalid()
    {
        // Arrange
        var type = ResolveBotServiceTypeOrSkip();
        var buildKeyboard = type.GetMethod(
            "BuildGroupStateKeyboard",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildKeyboard.Should().NotBeNull();

        // Act
        var keyboard = (InlineKeyboardMarkup?)buildKeyboard!.Invoke(null, [123L, "sky_team_bot", "invalid-short-name!"]);

        // Assert
        keyboard.Should().NotBeNull();
        var buttons = keyboard!.InlineKeyboard.SelectMany(row => row).ToArray();
        buttons.Should().Contain(button => button.Text == "Open app" && button.Url == "https://t.me/sky_team_bot?startapp=123");
    }

    [Fact]
    public void GroupCockpitCallbackPayloads_ShouldStayWithinTelegramCallbackDataLimit()
    {
        // Arrange
        var callbackPayloads = new[]
        {
            GetPrivateStaticString("NewCallbackData"),
            GetPrivateStaticString("JoinCallbackData"),
            GetPrivateStaticString("StartCallbackData"),
            GetPrivateStaticString("RollCallbackData"),
            GetPrivateStaticString("PlaceDmCallbackData"),
            GetPrivateStaticString("RefreshCallbackData")
        };

        // Assert
        callbackPayloads.Should().OnlyContain(payload => payload.Length <= 64);
    }

    private static Type ResolveBotServiceTypeOrSkip()
    {
        var assembly = TryLoadAssembly("SkyTeam.TelegramBot");
        var type = assembly?.GetTypes().FirstOrDefault(t => t.Name == "TelegramBotService");

        type.Should().NotBeNull("SkyTeam.TelegramBot.TelegramBotService should exist in this solution");
        return type!;
    }

    private static string GetPrivateStaticString(string fieldName)
    {
        var field = ResolveBotServiceTypeOrSkip().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        field.Should().NotBeNull();
        return (string)field!.GetValue(null)!;
    }

    private static Assembly? TryLoadAssembly(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null;
        }
    }
}
