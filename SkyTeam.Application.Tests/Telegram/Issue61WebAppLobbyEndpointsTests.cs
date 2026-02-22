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

public sealed class Issue61WebAppLobbyEndpointsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public async Task LobbyNewEndpoint_ShouldCreateLobby_WhenLobbyDoesNotExist()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 111, viewerDisplayName: "Alice", startParam: groupChatId.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webapp/lobby/new?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.Lobby);
        state.Lobby!.IsReady.Should().BeFalse();
    }

    [Fact]
    public async Task LobbyJoinEndpoint_ShouldSeatViewer_WhenLobbyExists()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        lobbyStore.CreateNew(groupChatId);

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 111, viewerDisplayName: "Alice", startParam: groupChatId.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webapp/lobby/join?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.Lobby);
        state.Viewer.Seat.Should().Be("Pilot");
        state.Lobby!.Pilot!.UserId.Should().Be(111);
    }

    [Fact]
    public async Task LobbyStartEndpoint_ShouldStartGame_WhenLobbyIsReadyAndViewerIsSeated()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 111, viewerDisplayName: "Alice", startParam: groupChatId.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webapp/lobby/start?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.InGame);
        state.Cockpit.Should().NotBeNull();
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldIncludePrivateHand_WhenViewerIsSeated()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();
        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
        gameSessionStore.RegisterRoll(groupChatId, new SecretDiceRoll([1, 2, 3, 4], [5, 6, 1, 2]));

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 111, viewerDisplayName: "Alice", startParam: groupChatId.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.InGame);
        state.PrivateHand.Should().NotBeNull();
        state.PrivateHand!.Seat.Should().Be("Pilot");
        state.PrivateHand.Dice.Should().HaveCount(4);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldNotIncludePrivateHand_WhenViewerIsNotSeated()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();
        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
        gameSessionStore.RegisterRoll(groupChatId, new SecretDiceRoll([1, 2, 3, 4], [5, 6, 1, 2]));

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 333, viewerDisplayName: "Eve", startParam: groupChatId.ToString());
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.InGame);
        state.Viewer.Seat.Should().BeNull();
        state.PrivateHand.Should().BeNull();
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
}
