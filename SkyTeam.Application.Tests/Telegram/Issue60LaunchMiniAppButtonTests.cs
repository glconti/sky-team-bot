using System.Reflection;
using FluentAssertions;
using Telegram.Bot.Types.ReplyMarkups;
using Xunit;

namespace SkyTeam.Application.Tests.Telegram;

public sealed class Issue60LaunchMiniAppButtonTests
{
    [Fact]
    public void BuildGroupStateKeyboard_ShouldIncludeOpenAppWebAppButton_WhenBotUsernameIsKnown()
    {
        // Arrange
        var type = ResolveBotServiceTypeOrSkip();
        var buildKeyboard = type.GetMethod(
            "BuildGroupStateKeyboard",
            BindingFlags.NonPublic | BindingFlags.Static);

        buildKeyboard.Should().NotBeNull("Slice #60 expects an Open app button in the group cockpit keyboard");

        var groupChatId = 123L;
        var botUsername = "sky_team_bot";
        var miniAppUrl = "https://example.test/";

        // Act
        var keyboard = (InlineKeyboardMarkup?)buildKeyboard!.Invoke(null, [groupChatId, botUsername, miniAppUrl]);

        // Assert
        keyboard.Should().NotBeNull();

        var buttons = keyboard!.InlineKeyboard.SelectMany(row => row).ToArray();
        buttons.Should().Contain(b => b.WebApp != null && b.WebApp.Url == miniAppUrl);
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

    private static Type ResolveBotServiceTypeOrSkip()
    {
        var assembly = TryLoadAssembly("SkyTeam.TelegramBot");
        var type = assembly?.GetTypes().FirstOrDefault(t => t.Name == "TelegramBotService");

        type.Should().NotBeNull("SkyTeam.TelegramBot.TelegramBotService should exist in this solution");
        return type!;
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
