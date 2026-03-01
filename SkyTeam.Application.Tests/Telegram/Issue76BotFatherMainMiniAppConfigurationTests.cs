using FluentAssertions;
using SkyTeam.TelegramBot.WebApp;
using Xunit;

namespace SkyTeam.Application.Tests.Telegram;

public sealed class Issue76BotFatherMainMiniAppConfigurationTests
{
    [Fact]
    public void WebAppOptionsValidator_ShouldAllowMissingMiniAppUrl_WhenMiniAppIsNotConfiguredYet()
    {
        // Arrange
        var validator = new WebAppOptionsValidator();
        var options = new WebAppOptions();

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://skyteam.example/")]
    [InlineData("https://skyteam.example/index.html")]
    public void WebAppOptionsValidator_ShouldAcceptAbsoluteHttpsMiniAppUrl(string miniAppUrl)
    {
        // Arrange
        var validator = new WebAppOptionsValidator();
        var options = new WebAppOptions { MiniAppUrl = miniAppUrl };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("http://skyteam.example/")]
    [InlineData("/index.html")]
    [InlineData("https://skyteam.example/?launch=1")]
    [InlineData("https://skyteam.example/#mini-app")]
    public void WebAppOptionsValidator_ShouldRejectNonBotFatherCompatibleMiniAppUrl(string miniAppUrl)
    {
        // Arrange
        var validator = new WebAppOptionsValidator();
        var options = new WebAppOptions { MiniAppUrl = miniAppUrl };

        // Act
        var result = validator.Validate(name: null, options);

        // Assert
        result.Failed.Should().BeTrue();
        result.Failures.Should().Contain(failure => failure.Contains("absolute HTTPS URL", StringComparison.Ordinal));
    }
}
