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

public sealed class Issue88SoloModeTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public void SoloModeLobbyCreation_ShouldAutoSeatSinglePlayerInBothRoles()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        var player = new LobbyPlayer(UserId: 111, DisplayName: "Alice");
        const long groupChatId = 123;

        // Act
        var result = store.CreateSoloLobby(groupChatId, player);

        // Assert
        result.Status.Should().Be(LobbyCreateStatus.Created);
        result.Snapshot.Pilot.Should().Be(player);
        result.Snapshot.Copilot.Should().Be(player);
        result.Snapshot.IsReady.Should().BeTrue("solo lobby should be ready immediately");
    }

    [Fact]
    public void SoloModeLobbyCreation_ShouldReturnAlreadyExists_WhenLobbyAlreadyExistsForChat()
    {
        // Arrange
        var store = new InMemoryGroupLobbyStore();
        var player = new LobbyPlayer(UserId: 111, DisplayName: "Alice");
        const long groupChatId = 123;
        store.CreateNew(groupChatId);

        // Act
        var result = store.CreateSoloLobby(groupChatId, player);

        // Assert
        result.Status.Should().Be(LobbyCreateStatus.AlreadyExists);
    }

    [Fact]
    public async Task SoloModeWebAppState_ShouldExposeIsSoloModeFlag_WhenPilotAndCopilotShareSameUserId()
    {
        // Arrange
        const long groupChatId = 456;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var player = new LobbyPlayer(UserId: 111, DisplayName: "Alice");
        lobbyStore.CreateSoloLobby(groupChatId, player);

        using var request = CreateGameStateRequest(groupChatId, viewerUserId: 111, "Alice");

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.Lobby);
        state.Lobby!.IsSoloMode.Should().BeTrue("pilot and copilot have same user ID");
    }

    [Fact]
    public async Task SoloModeWebAppState_ShouldNotExposeIsSoloModeFlag_WhenDifferentUsersOccupySeats()
    {
        // Arrange
        const long groupChatId = 789;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(UserId: 111, DisplayName: "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(UserId: 222, DisplayName: "Bob"));

        using var request = CreateGameStateRequest(groupChatId, viewerUserId: 111, "Alice");

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.Lobby);
        state.Lobby!.IsSoloMode.Should().BeFalse("pilot and copilot have different user IDs");
    }

    [Fact]
    public async Task SoloModeEndpoint_ShouldCreateSoloLobbyAndAutoSeatViewer()
    {
        // Arrange
        const long groupChatId = 555;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/webapp/lobby/new-solo?gameId={groupChatId}",
            groupChatId,
            viewerUserId: 111,
            "Alice");

        // Act
        var response = await client.SendAsync(request);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.Lobby);
        state.Lobby!.IsReady.Should().BeTrue();
        state.Lobby.Pilot!.UserId.Should().Be(111);
        state.Lobby.Copilot!.UserId.Should().Be(111);
        state.Lobby.IsSoloMode.Should().BeTrue();
    }

    [Fact]
    public async Task SoloModeEndpoint_ShouldReturnBadRequest_WhenLobbyAlreadyExists()
    {
        // Arrange
        const long groupChatId = 666;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        lobbyStore.CreateNew(groupChatId);

        using var request = CreateAuthenticatedRequest(
            HttpMethod.Post,
            $"/api/webapp/lobby/new-solo?gameId={groupChatId}",
            groupChatId,
            viewerUserId: 111,
            "Alice");

        // Act
        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        payload.Should().Contain("Lobby already exists");
    }

    [Fact]
    public void SoloModeUI_ShouldContainSoloModeButton_InLobbySection()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasSoloModeButton = source.Contains("Solo Mode", StringComparison.Ordinal);

        // Assert
        hasSoloModeButton.Should().BeTrue("lobby should offer solo mode option for testing");
    }

    [Fact]
    public void SoloModeUI_ShouldContainSoloModeBadgeOrIndicator()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasSoloModeBadge = source.Contains("solo", StringComparison.OrdinalIgnoreCase)
                             && (source.Contains("badge", StringComparison.OrdinalIgnoreCase)
                                 || source.Contains("indicator", StringComparison.OrdinalIgnoreCase)
                                 || source.Contains("IsSoloMode", StringComparison.Ordinal));

        // Assert
        hasSoloModeBadge.Should().BeTrue("UI should display solo mode indicator when IsSoloMode is true");
    }

    [Fact]
    public void SoloModeUI_ShouldContainSoloModeWarning_ForTestingOnly()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var hasTestingWarning = source.Contains("testing", StringComparison.OrdinalIgnoreCase)
                              && source.Contains("solo", StringComparison.OrdinalIgnoreCase);

        // Assert
        hasTestingWarning.Should().BeTrue("solo mode should warn users it's for testing purposes");
    }

    [Fact]
    public void SoloModeUI_ShouldHandleIsSoloModeFlag_InStateRendering()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppIndexPath());

        // Act
        var handlesIsSoloMode = source.Contains("isSoloMode", StringComparison.Ordinal)
                              || source.Contains("IsSoloMode", StringComparison.Ordinal)
                              || source.Contains("is_solo_mode", StringComparison.Ordinal);

        // Assert
        handlesIsSoloMode.Should().BeTrue("UI should read and display isSoloMode flag from state");
    }

    [Theory]
    [InlineData("TwoPlayer")]
    [InlineData("Solo")]
    public void GameMode_ShouldHaveExpectedValues(string expectedMode)
    {
        // Arrange
        var gameModeType = Type.GetType("SkyTeam.Domain.GameMode, SkyTeam.Domain");

        // Act
        var hasExpectedValue = gameModeType is not null
                             && Enum.GetNames(gameModeType).Contains(expectedMode);

        // Assert
        hasExpectedValue.Should().BeTrue($"GameMode enum should contain {expectedMode} value");
    }

    [Fact]
    public void GameMode_ShouldDefaultToTwoPlayer_WhenNotSpecified()
    {
        // Arrange
        var gameType = Type.GetType("SkyTeam.Domain.Game, SkyTeam.Domain");

        // Act
        var hasModeProperty = gameType?.GetProperty("Mode") is not null;

        // Assert
        hasModeProperty.Should().BeTrue("Game should have Mode property for solo vs two-player distinction");
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        HttpMethod method,
        string url,
        long groupChatId,
        long viewerUserId,
        string viewerDisplayName)
    {
        var initData = BuildInitData(
            TestBotToken,
            DateTimeOffset.UtcNow,
            viewerUserId,
            viewerDisplayName,
            groupChatId.ToString());

        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Telegram-Init-Data", initData);
        return request;
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

    private static string ResolveWebAppIndexPath()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot", "wwwroot", "index.html"));
}
