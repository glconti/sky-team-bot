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
using SkyTeam.Application.Lobby;
using SkyTeam.TelegramBot;
using SkyTeam.TelegramBot.WebApp;

public sealed class Issue59WebAppGameStateEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn401_WhenInitDataHeaderIsMissing()
    {
        // Arrange
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/webapp/game-state?gameId=123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn401_WhenInitDataHashIsInvalid()
    {
        // Arrange
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var now = DateTimeOffset.UtcNow;
        var initData = BuildInitData(TestBotToken, authDate: now, viewerUserId: 111, viewerDisplayName: "Alice", startParam: "123");
        var tampered = TamperHash(initData);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/webapp/game-state?gameId=123");
        request.Headers.Add("X-Telegram-Init-Data", tampered);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn401_WhenAuthDateIsExpired()
    {
        // Arrange
        await using var factory = CreateFactory(initDataMaxAgeSeconds: 60);
        using var client = factory.CreateClient();

        var authDate = DateTimeOffset.UtcNow - TimeSpan.FromMinutes(10);
        var initData = BuildInitData(TestBotToken, authDate, viewerUserId: 111, viewerDisplayName: "Alice", startParam: "123");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/webapp/game-state?gameId=123");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn404_WhenNoLobbyOrGameExists()
    {
        // Arrange
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 111, viewerDisplayName: "Alice", startParam: "123");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/webapp/game-state?gameId=123");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn400_WhenGameIdDoesNotMatchSignedStartParam()
    {
        // Arrange
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId: 111, viewerDisplayName: "Alice", startParam: "123");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/webapp/game-state?gameId=999");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturnLobbyState_WhenLobbyExists()
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

        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state.Should().BeEquivalentTo(new WebAppGameStateResponse(
            GameId: groupChatId,
            Phase: WebAppGamePhase.Lobby,
            Lobby: new WebAppLobbyState(
                Pilot: new WebAppLobbySeat(111, "Alice"),
                Copilot: new WebAppLobbySeat(222, "Bob"),
                IsReady: true),
            Cockpit: null,
            GameStatus: "InProgress",
            Viewer: new WebAppViewer(111, "Pilot")));
    }

    private static WebApplicationFactory<Program> CreateFactory(int? initDataMaxAgeSeconds = null)
        => new TelegramBotWebAppFactory(initDataMaxAgeSeconds);

    private sealed class TelegramBotWebAppFactory(int? initDataMaxAgeSeconds) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TELEGRAM_BOT_TOKEN"] = TestBotToken,
                    ["WebApp:InitDataMaxAgeSeconds"] = initDataMaxAgeSeconds?.ToString() ?? "300"
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
        var hashHex = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();

        fields["hash"] = hashHex;

        return string.Join("&", fields
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static string TamperHash(string initData)
    {
        var fields = initData.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(seg => seg.Split('=', 2))
            .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => p.Length == 2 ? Uri.UnescapeDataString(p[1]) : string.Empty, StringComparer.Ordinal);

        if (!fields.TryGetValue("hash", out var hash) || hash.Length == 0)
            return initData;

        fields["hash"] = hash[..^1] + (hash[^1] == '0' ? '1' : '0');

        return string.Join("&", fields.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }
}
