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

public sealed class Issue64WebAppPlacementFlowTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public void WebAppEndpointSource_ShouldMapPlacementAndUndoEndpoints_WithCockpitRefreshBridge()
    {
        // Arrange
        var source = File.ReadAllText(ResolveWebAppEndpointsSourcePath());

        // Act
        var mapsPlaceEndpoint = source.Contains("group.MapPost(\"/game/place\", PlaceDie);", StringComparison.Ordinal);
        var mapsUndoEndpoint = source.Contains("group.MapPost(\"/game/undo\", UndoLastPlacement);", StringComparison.Ordinal);
        var refreshesCockpitAfterActions = source.Contains("await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId!.Value, cancellationToken);", StringComparison.Ordinal);

        // Assert
        mapsPlaceEndpoint.Should().BeTrue("issue #64 requires a Mini App placement endpoint");
        mapsUndoEndpoint.Should().BeTrue("issue #64 requires a Mini App undo endpoint");
        refreshesCockpitAfterActions.Should().BeTrue("successful place/undo must refresh the group cockpit");
    }

    [Fact]
    public async Task PlaceEndpoint_ShouldExecuteTokenAdjustedPlacement_WhenAdjustedCommandIsSelected()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var (userId, commandId, dieIndex) = SeedTurnWithTokenAdjustedOption(factory, groupChatId);
        using var placeRequest = CreatePlaceRequest(groupChatId, userId, "Player", dieIndex, commandId);

        // Act
        var response = await client.SendAsync(placeRequest);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.InGame);
        state.PrivateHand.Should().NotBeNull();
        state.PrivateHand!.Dice.Single(d => d.Index == dieIndex).IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task UndoEndpoint_ShouldRestoreUsedDie_WhenUndoIsAllowed()
    {
        // Arrange
        const long groupChatId = 123;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var (userId, dieIndex, commandId) = SeedTurnForPlacement(factory, groupChatId);
        using var placeRequest = CreatePlaceRequest(groupChatId, userId, "Player", dieIndex, commandId);
        var placeResponse = await client.SendAsync(placeRequest);
        placeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var undoRequest = CreateUndoRequest(groupChatId, userId, "Player");

        // Act
        var undoResponse = await client.SendAsync(undoRequest);
        var state = await undoResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        undoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.PrivateHand.Should().NotBeNull();
        state.PrivateHand!.Dice.Single(d => d.Index == dieIndex).IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task PlaceEndpoint_ShouldBindPlacementToRequestedGame_WhenViewerHasAnotherActiveSession()
    {
        // Arrange
        const long primaryGroupChatId = 123;
        const long secondaryGroupChatId = 456;
        const long sharedUserId = 111;
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var (dieIndex, commandId) = SeedPlacementAcrossMultipleGames(factory, primaryGroupChatId, secondaryGroupChatId, sharedUserId);
        using var placeRequest = CreatePlaceRequest(primaryGroupChatId, sharedUserId, "Alice", dieIndex, commandId);

        // Act
        var response = await client.SendAsync(placeRequest);
        var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();
        var primaryState = gameSessionStore.GetPublicState(primaryGroupChatId);
        var secondaryState = gameSessionStore.GetPublicState(secondaryGroupChatId);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        state!.Phase.Should().Be(WebAppGamePhase.InGame);
        primaryState!.PlacementsMade.Should().Be(1);
        secondaryState!.PlacementsMade.Should().Be(0);
    }

    [Fact]
    public async Task WebAppTransportFlow_ShouldCoverOpenLobbyStartRollPlaceUndo()
    {
        // Arrange
        const long groupChatId = 123;
        const long pilotUserId = 111;
        const long copilotUserId = 222;

        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Act
        var createLobbyResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Post, $"/api/webapp/lobby/new?gameId={groupChatId}", pilotUserId, "Alice"));
        var joinPilotResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Post, $"/api/webapp/lobby/join?gameId={groupChatId}", pilotUserId, "Alice"));
        var joinCopilotResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Post, $"/api/webapp/lobby/join?gameId={groupChatId}", copilotUserId, "Bob"));
        var startResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Post, $"/api/webapp/lobby/start?gameId={groupChatId}", pilotUserId, "Alice"));
        var rollResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Post, $"/api/webapp/game/roll?gameId={groupChatId}", pilotUserId, "Alice"));

        var pilotStateResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}", pilotUserId, "Alice"));
        var pilotState = await pilotStateResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);
        var currentPlayer = pilotState!.PrivateHand!.CurrentPlayer;
        var currentUserId = currentPlayer == "Pilot" ? pilotUserId : copilotUserId;
        var currentDisplayName = currentUserId == pilotUserId ? "Alice" : "Bob";

        var currentStateResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}", currentUserId, currentDisplayName));
        var currentState = await currentStateResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);
        var commandId = currentState!.PrivateHand!.AvailableCommands.First().CommandId;
        var dieIndex = FindDieIndex(currentState.PrivateHand.Dice, commandId);

        var placeResponse = await client.SendAsync(CreateActionRequest(
            HttpMethod.Post,
            $"/api/webapp/game/place?gameId={groupChatId}",
            currentUserId,
            currentDisplayName,
            new { dieIndex, commandId }));
        var placedState = await placeResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        var undoResponse = await client.SendAsync(CreateActionRequest(HttpMethod.Post, $"/api/webapp/game/undo?gameId={groupChatId}", currentUserId, currentDisplayName));
        var undoneState = await undoResponse.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

        // Assert
        createLobbyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        joinPilotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        joinCopilotResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        rollResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        placeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        undoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        placedState!.Phase.Should().Be(WebAppGamePhase.InGame);
        placedState.PrivateHand!.Dice.Single(d => d.Index == dieIndex).IsUsed.Should().BeTrue();
        undoneState!.PrivateHand!.Dice.Single(d => d.Index == dieIndex).IsUsed.Should().BeFalse();
    }

    private static (long UserId, string CommandId, int DieIndex) SeedTurnWithTokenAdjustedOption(WebApplicationFactory<Program> factory, long groupChatId)
    {
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();

        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
        gameSessionStore.RegisterRoll(groupChatId, new SecretDiceRoll([2, 3, 4, 5], [1, 2, 3, 4]));

        var pilotHand = gameSessionStore.GetHand(111);
        var startingSeat = pilotHand.CurrentPlayer!.Value;
        var currentUserId = startingSeat == PlayerSeat.Pilot ? 111 : 222;
        var otherUserId = currentUserId == 111 ? 222 : 111;

        var currentHand = gameSessionStore.GetHand(currentUserId);
        var tokenCommand = currentHand.AvailableCommands!.First(c => c.CommandId.StartsWith("Concentration.Assign", StringComparison.Ordinal)).CommandId;
        var tokenDieIndex = FindDieIndex(currentHand.Hand!, tokenCommand);
        gameSessionStore.PlaceDie(currentUserId, tokenDieIndex, tokenCommand).Status.Should().Be(GamePlacementStatus.Placed);

        var otherHand = gameSessionStore.GetHand(otherUserId);
        var otherCommand = otherHand.AvailableCommands!.First().CommandId;
        var otherDieIndex = FindDieIndex(otherHand.Hand!, otherCommand);
        gameSessionStore.PlaceDie(otherUserId, otherDieIndex, otherCommand).Status.Should().Be(GamePlacementStatus.Placed);

        var adjustedHand = gameSessionStore.GetHand(currentUserId);
        var adjustedCommand = adjustedHand.AvailableCommands!.First(c => c.CommandId.Contains('>')).CommandId;
        var adjustedDieIndex = FindDieIndex(adjustedHand.Hand!, adjustedCommand);

        return (currentUserId, adjustedCommand, adjustedDieIndex);
    }

    private static (long UserId, int DieIndex, string CommandId) SeedTurnForPlacement(WebApplicationFactory<Program> factory, long groupChatId)
    {
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();

        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
        gameSessionStore.RegisterRoll(groupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));

        var pilotHand = gameSessionStore.GetHand(111);
        var currentUserId = pilotHand.CurrentPlayer == PlayerSeat.Pilot ? 111 : 222;

        var hand = gameSessionStore.GetHand(currentUserId);
        var command = hand.AvailableCommands!.First().CommandId;
        var dieIndex = FindDieIndex(hand.Hand!, command);

        return (currentUserId, dieIndex, command);
    }

    private static (int DieIndex, string CommandId) SeedPlacementAcrossMultipleGames(
        WebApplicationFactory<Program> factory,
        long primaryGroupChatId,
        long secondaryGroupChatId,
        long sharedUserId)
    {
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();

        lobbyStore.CreateNew(primaryGroupChatId);
        lobbyStore.Join(primaryGroupChatId, new LobbyPlayer(sharedUserId, "Alice"));
        lobbyStore.Join(primaryGroupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(primaryGroupChatId, lobbyStore.GetSnapshot(primaryGroupChatId), requestingUserId: sharedUserId);
        gameSessionStore.RegisterRoll(primaryGroupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));

        var primaryHand = gameSessionStore.GetHand(primaryGroupChatId, sharedUserId);
        var commandId = primaryHand.AvailableCommands!.First().CommandId;
        var dieIndex = FindDieIndex(primaryHand.Hand!, commandId);

        lobbyStore.CreateNew(secondaryGroupChatId);
        lobbyStore.Join(secondaryGroupChatId, new LobbyPlayer(sharedUserId, "Alice"));
        lobbyStore.Join(secondaryGroupChatId, new LobbyPlayer(333, "Charlie"));
        gameSessionStore.Start(secondaryGroupChatId, lobbyStore.GetSnapshot(secondaryGroupChatId), requestingUserId: sharedUserId);
        gameSessionStore.RegisterRoll(secondaryGroupChatId, new SecretDiceRoll([6, 6, 6, 6], [6, 6, 6, 6]));

        return (dieIndex, commandId);
    }

    private static int FindDieIndex(SecretDiceHand hand, string commandId)
    {
        var markerStart = commandId.LastIndexOf(':');
        markerStart.Should().BeGreaterThanOrEqualTo(0);

        var markerEnd = commandId.IndexOf('>', markerStart + 1);
        if (markerEnd < 0)
            markerEnd = commandId.Length;

        var rolledValueText = commandId[(markerStart + 1)..markerEnd];
        var rolledValue = int.Parse(rolledValueText);

        return hand.Dice.First(d => !d.IsUsed && d.Value.Value == rolledValue).Index;
    }

    private static int FindDieIndex(IReadOnlyList<WebAppHandDie> dice, string commandId)
    {
        var markerStart = commandId.LastIndexOf(':');
        markerStart.Should().BeGreaterThanOrEqualTo(0);

        var markerEnd = commandId.IndexOf('>', markerStart + 1);
        if (markerEnd < 0)
            markerEnd = commandId.Length;

        var rolledValueText = commandId[(markerStart + 1)..markerEnd];
        var rolledValue = int.Parse(rolledValueText);

        return dice.First(d => !d.IsUsed && d.Value == rolledValue).Index;
    }

    private static HttpRequestMessage CreatePlaceRequest(long groupChatId, long viewerUserId, string viewerDisplayName, int dieIndex, string commandId)
    {
        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId, viewerDisplayName, groupChatId.ToString());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webapp/game/place?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);
        request.Content = JsonContent.Create(new { dieIndex, commandId });
        return request;
    }

    private static HttpRequestMessage CreateUndoRequest(long groupChatId, long viewerUserId, string viewerDisplayName)
    {
        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId, viewerDisplayName, groupChatId.ToString());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/webapp/game/undo?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);
        return request;
    }

    private static HttpRequestMessage CreateActionRequest(HttpMethod method, string url, long viewerUserId, string viewerDisplayName, object? content = null)
    {
        var gameId = url[(url.LastIndexOf("gameId=", StringComparison.Ordinal) + "gameId=".Length)..];
        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId, viewerDisplayName, gameId);

        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Telegram-Init-Data", initData);

        if (content is not null)
            request.Content = JsonContent.Create(content);

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
