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
using SkyTeam.TelegramBot.Persistence;
using SkyTeam.TelegramBot.WebApp;

public sealed class Issue80FileBackedRestartPersistenceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private const string TestBotToken = "TEST_BOT_TOKEN:123456";

    [Fact]
    public async Task GameStateEndpoint_ShouldRestoreInProgressSession_WhenStoreIsRehydratedFromJsonFile()
    {
        // Arrange
        const long groupChatId = 8080;
        var persistenceDirectory = Path.Combine(Path.GetTempPath(), $"skyteam-restart-{Guid.NewGuid():N}");
        var persistenceFilePath = Path.Combine(persistenceDirectory, "game-sessions.json");
        Directory.CreateDirectory(persistenceDirectory);

        try
        {
            await using (var firstFactory = CreateFactory(persistenceFilePath))
            {
                SeedInGameSession(firstFactory, groupChatId);

                var repository = firstFactory.Services.GetRequiredService<IGameSessionPersistence>();
                repository.GetById(groupChatId).Should().NotBeNull();
                repository.List().Should().ContainSingle(session => session.GroupChatId == groupChatId);
            }

            await using var restartedFactory = CreateFactory(persistenceFilePath);
            using var client = restartedFactory.CreateClient();
            using var request = CreateGameStateRequest(groupChatId, viewerUserId: 111, viewerDisplayName: "Alice");

            // Act
            var response = await client.SendAsync(request);
            var state = await response.Content.ReadFromJsonAsync<WebAppGameStateResponse>(JsonOptions);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            state.Should().NotBeNull();
            state!.Phase.Should().Be(WebAppGamePhase.InGame);
            state.Cockpit.Should().NotBeNull();
            state.Cockpit!.PlacementsMade.Should().Be(1);
        }
        finally
        {
            if (Directory.Exists(persistenceDirectory))
                Directory.Delete(persistenceDirectory, recursive: true);
        }
    }

    private static void SeedInGameSession(WebApplicationFactory<Program> factory, long groupChatId)
    {
        var lobbyStore = factory.Services.GetRequiredService<InMemoryGroupLobbyStore>();
        var gameSessionStore = factory.Services.GetRequiredService<InMemoryGroupGameSessionStore>();

        lobbyStore.CreateNew(groupChatId);
        lobbyStore.Join(groupChatId, new LobbyPlayer(111, "Alice"));
        lobbyStore.Join(groupChatId, new LobbyPlayer(222, "Bob"));
        gameSessionStore.Start(groupChatId, lobbyStore.GetSnapshot(groupChatId), requestingUserId: 111);
        var rollResult = gameSessionStore.RegisterRoll(groupChatId, new SecretDiceRoll([1, 2, 3, 4], [1, 2, 3, 4]));
        var currentUser = rollResult.StartingPlayer == PlayerSeat.Pilot ? 111 : 222;
        var hand = gameSessionStore.GetHand(groupChatId, currentUser);
        var move = SelectPlayableMove(hand);

        gameSessionStore.PlaceDie(groupChatId, currentUser, move.DieIndex, move.CommandId).Status
            .Should()
            .Be(GamePlacementStatus.Placed);
    }

    private static (int DieIndex, string CommandId) SelectPlayableMove(GameHandResult hand)
    {
        hand.Status.Should().Be(GameHandStatus.Ok);
        hand.Hand.Should().NotBeNull();
        hand.AvailableCommands.Should().NotBeNull();

        for (var dieIndex = 0; dieIndex < hand.Hand!.Dice.Count; dieIndex++)
        {
            var dieValue = hand.Hand.Dice[dieIndex].Value.Value;
            var matchingCommand = hand.AvailableCommands!
                .FirstOrDefault(command => CommandMatchesDieValue(command.CommandId, dieValue));

            if (matchingCommand is not null)
                return (dieIndex, matchingCommand.CommandId);
        }

        throw new InvalidOperationException("No playable command found for current hand.");
    }

    private static bool CommandMatchesDieValue(string commandId, int dieValue)
    {
        var colonIndex = commandId.LastIndexOf(':');
        if (colonIndex < 0 || colonIndex == commandId.Length - 1)
            return false;

        var endIndex = commandId.IndexOf('>', colonIndex + 1);
        if (endIndex < 0)
            endIndex = commandId.Length;

        return int.TryParse(commandId.AsSpan(colonIndex + 1, endIndex - colonIndex - 1), out var value)
               && value == dieValue;
    }

    private static HttpRequestMessage CreateGameStateRequest(long groupChatId, long viewerUserId, string viewerDisplayName)
    {
        var initData = BuildInitData(TestBotToken, DateTimeOffset.UtcNow, viewerUserId, viewerDisplayName, groupChatId.ToString());
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/webapp/game-state?gameId={groupChatId}");
        request.Headers.Add("X-Telegram-Init-Data", initData);
        return request;
    }

    private static WebApplicationFactory<Program> CreateFactory(string persistenceFilePath)
        => new TelegramBotWebAppFactory(persistenceFilePath);

    private sealed class TelegramBotWebAppFactory(string persistenceFilePath) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TELEGRAM_BOT_TOKEN"] = TestBotToken,
                    ["WebApp:InitDataMaxAgeSeconds"] = "300",
                    ["Persistence:GameSessionsFilePath"] = persistenceFilePath
                });
            });

            builder.ConfigureServices(services =>
            {
                var hostedServiceDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToList();
                foreach (var descriptor in hostedServiceDescriptors)
                    services.Remove(descriptor);

                var persistenceDescriptors = services.Where(descriptor => descriptor.ServiceType == typeof(IGameSessionPersistence)).ToList();
                foreach (var descriptor in persistenceDescriptors)
                    services.Remove(descriptor);

                services.AddSingleton<IGameSessionPersistence, JsonGameSessionPersistence>();
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
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));

        var secretKey = System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes("WebAppData"),
            System.Text.Encoding.UTF8.GetBytes(botToken));

        var expectedHashBytes = System.Security.Cryptography.HMACSHA256.HashData(secretKey, System.Text.Encoding.UTF8.GetBytes(dataCheckString));
        fields["hash"] = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();

        return string.Join("&", fields.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
    }
}
