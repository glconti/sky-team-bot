namespace SkyTeam.Application.Tests.Telegram;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.TelegramBot;

public sealed class Issue84AbuseProtectionResidualTests
{
    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn429WithRetryAfter_WhenUserExceedsPerSecondLimit()
    {
        // Arrange
        const long groupChatId = 123;
        const long userId = 111;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedLobby(factory, groupChatId);
        var cancellationToken = TestContext.Current.CancellationToken;

        for (var i = 0; i < 10; i++)
        {
            using var allowedRequest = CreateRequest(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}", groupChatId, userId, "Alice");
            using var allowedResponse = await client.SendAsync(allowedRequest, cancellationToken);
            allowedResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        using var throttledRequest = CreateRequest(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}", groupChatId, userId, "Alice");

        // Act
        using var throttledResponse = await client.SendAsync(throttledRequest, cancellationToken);
        var payload = await throttledResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        // Assert
        throttledResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.TryGetValues("Retry-After", out var retryAfterValues).Should().BeTrue();
        int.Parse(retryAfterValues!.Single()).Should().BeGreaterThan(0);
        payload.GetProperty("error").GetString().Should().Be("Too many requests. Please retry later.");
        payload.GetProperty("retryAfterSeconds").GetInt32().Should().BeGreaterThan(0);
        payload.GetProperty("retryHint").GetString().Should().Contain("Retry after");
    }

    [Fact]
    public async Task GameStateEndpoint_ShouldReturn429_WhenIpExceedsPerMinuteLimit()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedLobby(factory, groupChatId);
        var cancellationToken = TestContext.Current.CancellationToken;

        for (var i = 0; i < 100; i++)
        {
            var userId = 1_000 + i;
            using var allowedRequest = CreateRequest(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}", groupChatId, userId, $"Viewer{userId}");
            using var allowedResponse = await client.SendAsync(allowedRequest, cancellationToken);
            allowedResponse.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        using var throttledRequest = CreateRequest(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}", groupChatId, 9_999, "Viewer9999");

        // Act
        using var throttledResponse = await client.SendAsync(throttledRequest, cancellationToken);

        // Assert
        throttledResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttledResponse.Headers.TryGetValues("Retry-After", out _).Should().BeTrue();
    }

    [Fact]
    public async Task RollEndpoint_ShouldReturn400_WhenIdempotencyKeyIsMissing()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedStartedGame(factory, groupChatId);
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = CreateRequest(HttpMethod.Post, $"/api/webapp/game/roll?gameId={groupChatId}", groupChatId, 111, "Alice");

        // Act
        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.GetProperty("error").GetString().Should().Be("Missing idempotency key.");
        payload.GetProperty("retryHint").GetString().Should().Contain("X-Idempotency-Key");
    }

    [Fact]
    public async Task RollEndpoint_ShouldReturn400_WhenIdempotencyKeyIsInvalid()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedStartedGame(factory, groupChatId);
        var cancellationToken = TestContext.Current.CancellationToken;

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/webapp/game/roll?gameId={groupChatId}",
            groupChatId,
            111,
            "Alice",
            idempotencyKey: "invalid key");

        // Act
        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.GetProperty("error").GetString().Should().Be("Invalid idempotency key.");
    }

    [Fact]
    public async Task RollEndpoint_ShouldReturn400_WhenIdempotencyKeyIsReused()
    {
        // Arrange
        const long groupChatId = 123;
        const string idempotencyKey = "roll-001";
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedStartedGame(factory, groupChatId);
        var cancellationToken = TestContext.Current.CancellationToken;

        using var firstRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/webapp/game/roll?gameId={groupChatId}",
            groupChatId,
            111,
            "Alice",
            idempotencyKey);
        using var replayRequest = CreateRequest(
            HttpMethod.Post,
            $"/api/webapp/game/roll?gameId={groupChatId}",
            groupChatId,
            111,
            "Alice",
            idempotencyKey);

        // Act
        using var firstResponse = await client.SendAsync(firstRequest, cancellationToken);
        using var replayResponse = await client.SendAsync(replayRequest, cancellationToken);
        var replayPayload = await replayResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        replayPayload.GetProperty("error").GetString().Should().Be("Duplicate idempotency key for this action.");
    }

    [Fact]
    public async Task PlaceEndpoint_ShouldReturn400_WhenPayloadExceedsMaxSize()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();
        SeedStartedGame(factory, groupChatId);
        var cancellationToken = TestContext.Current.CancellationToken;
        var oversizedCommandId = new string('A', 3_000);

        using var request = CreateRequest(
            HttpMethod.Post,
            $"/api/webapp/game/place?gameId={groupChatId}",
            groupChatId,
            111,
            "Alice",
            idempotencyKey: Guid.NewGuid().ToString("N"),
            content: new { dieIndex = 0, commandId = oversizedCommandId });

        // Act
        using var response = await client.SendAsync(request, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.GetProperty("error").GetString().Should().Be("Request payload exceeds max size.");
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string url,
        long groupChatId,
        long viewerUserId,
        string viewerDisplayName,
        string? idempotencyKey = null,
        object? content = null)
    {
        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId, viewerDisplayName, groupChatId.ToString());
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Telegram-Init-Data", initData);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.Add("X-Idempotency-Key", idempotencyKey);

        if (content is not null)
            request.Content = JsonContent.Create(content);

        return request;
    }

    private static void SeedLobby(WebApplicationFactory<Program> factory, long groupChatId)
    {
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
    }

    private static void SeedStartedGame(WebApplicationFactory<Program> factory, long groupChatId)
    {
        SeedLobby(factory, groupChatId);
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
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
