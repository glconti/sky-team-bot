namespace SkyTeam.Application.Tests.Telegram;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;
using SkyTeam.TelegramBot;
using SkyTeam.TelegramBot.WebApp;

public sealed class Issue62WebAppInGameViewTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Theory]
    [InlineData(111, "Pilot")]
    [InlineData(222, "Copilot")]
    [InlineData(333, null)]
    public async Task GameStateEndpoint_ShouldDetectViewerRole_WhenViewerIsPilotCopilotOrSpectator(long viewerUserId, string? expectedSeat)
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedInGameSession(factory, groupChatId);

        using var request = CreateGameStateRequest(groupChatId, viewerUserId, "Viewer");

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.InGame);
        state.Viewer.Seat.Should().Be(expectedSeat);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldNotExposePrivateHandData_WhenViewerIsSpectator()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedInGameSession(factory, groupChatId);

        using var request = CreateGameStateRequest(groupChatId, viewerUserId: 333, "Spectator");

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.PrivateHand.Should().BeNull("spectators must not receive private hand");
    }

    [Fact]
    public void WebAppEndpointSource_ShouldReadPrivateHandWithoutDirectMessages_WhenServingInGameView()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var readsPrivateHandFromStore = source.Contains("gameSessionStore.GetHand(", StringComparison.Ordinal);
        var usesDirectMessages = source.Contains("SendMessage(", StringComparison.Ordinal)
                                || source.Contains("TrySendDirectMessageAsync(", StringComparison.Ordinal);

        // Assert
        readsPrivateHandFromStore.Should().BeTrue("issue #62 requires private hand in the in-game WebApp response");
        usesDirectMessages.Should().BeFalse("issue #62 WebApp endpoint should not send DMs");
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturnPrivateHandOnlyForSeatedViewer()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedInGameSession(factory, groupChatId);

        using var seatedRequest = CreateGameStateRequest(groupChatId, viewerUserId: 111, "Pilot");
        using var spectatorRequest = CreateGameStateRequest(groupChatId, viewerUserId: 333, "Spectator");

        // Act
        var seatedResponse = await client.SendAsync(seatedRequest);
        var seatedState = await seatedResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);
        var spectatorResponse = await client.SendAsync(spectatorRequest);
        var spectatorState = await spectatorResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        seatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        seatedState!.PrivateHand.Should().NotBeNull();
        spectatorResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        spectatorState!.PrivateHand.Should().BeNull();
    }

    private static void SeedInGameSession(WebApplicationFactory<Program> factory, long groupChatId)
    {
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();

        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
        gameSessionStore.RegisterRoll(groupChatId, new SecretDiceRoll([1, 2, 3, 4], [6, 5, 4, 3]));
    }

    private static HttpRequestMessage CreateGameStateRequest(long groupChatId, long viewerUserId, string viewerDisplayName)
    {
        var initData = BuildInitData(
            TestBotToken,
            DateTimeOffset.UtcNow,
            viewerUserId,
            viewerDisplayName,
            groupChatId.ToString());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);
        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory()
        => new TelegramBotWebAppFactory();

    private sealed class TelegramBotWebAppFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");

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
                var hostedServiceDescriptors = services
                    .Where(d => d.ServiceType == typeof(IHostedService))
                    .ToList();

                foreach (var descriptor in hostedServiceDescriptors)
                    services.Remove(descriptor);
            });
        }
    }

    private static string BuildInitData(
        string botToken,
        DateTimeOffset authDate,
        long viewerUserId,
        string viewerDisplayName,
        string startParam)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "AAH-test-query-id",
            ["start_param"] = startParam,
            ["user"] = $"{{\"id\":{viewerUserId},\"first_name\":\"{viewerDisplayName}\"}}"
        };

        var dataCheckString = string.Join("\n", fields
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}"));

        var secretKey = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("WebAppData"),
            System.Text.Encoding.UTF8.GetBytes(botToken));

        var expectedHashBytes = System.Security.Cryptography.HMACSHA256.HashData(secretKey, System.Text.Encoding.UTF8.GetBytes(dataCheckString));
        fields["hash"] = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();

        return string.Join("&", fields
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static string ResolveWebAppEndpointsSourcePath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "WebApp", "WebAppEndpoints.cs"));
}
