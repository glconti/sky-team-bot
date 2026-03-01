namespace SkyTeam.Application.Tests.Telegram;

using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyTeam.TelegramBot;
using Xunit;

public sealed class Issue59WebAppStaticHostingTests
{
    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public async Task RootPath_ShouldServeIndexHtml_WhenStaticFilesAreEnabled()
    {
        // Arrange
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        new
        {
            response.StatusCode,
            HasTitle = body.Contains("<title>Sky Team</title>", StringComparison.Ordinal)
        }.Should().BeEquivalentTo(new { StatusCode = HttpStatusCode.OK, HasTitle = true });
    }

    private static WebApplicationFactory<Program> CreateFactory()
        => new TelegramBotWebAppFactory();

    private sealed class TelegramBotWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");

            builder.UseContentRoot(Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "SkyTeam.TelegramBot")));

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TELEGRAM_BOT_TOKEN"] = TestBotToken,
                    ["WebApp:InitDataMaxAgeSeconds"] = "300"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Prevent Telegram polling during integration tests.
                var hostedServiceDescriptors = services
                    .Where(d => d.ServiceType == typeof(IHostedService))
                    .ToList();

                foreach (var descriptor in hostedServiceDescriptors)
                    services.Remove(descriptor);
            });
        }
    }
}
