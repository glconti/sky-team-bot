using System.Globalization;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;

namespace SkyTeam.TelegramBot.WebApp;

public enum WebAppGamePhase
{
    Lobby,
    InGame,
    GameOver
}

public sealed record WebAppLobbySeat(long UserId, string DisplayName);

public sealed record WebAppLobbyState(
    WebAppLobbySeat? Pilot,
    WebAppLobbySeat? Copilot,
    bool IsReady);

public sealed record WebAppCockpitState(
    int RoundNumber,
    string RoundStatus,
    string? CurrentPlayer,
    int? PlacementsMade,
    int? PlacementsRemaining,
    int AxisPosition,
    int? EnginesSpeed,
    int ApproachPosition,
    int ApproachSegments,
    int PlanesRemaining,
    int CoffeeTokens,
    int BrakesActivated,
    int BrakesCapability,
    int FlapsValue,
    int LandingGearValue,
    double BlueAerodynamicsThreshold,
    double OrangeAerodynamicsThreshold);

public sealed record WebAppViewer(long UserId, string? Seat);

public sealed record WebAppGameStateResponse(
    long GameId,
    WebAppGamePhase Phase,
    WebAppLobbyState? Lobby,
    WebAppCockpitState? Cockpit,
    string GameStatus,
    WebAppViewer Viewer);

public static class WebAppEndpoints
{
    public static void MapWebAppEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webapp")
            .AddEndpointFilter<TelegramInitDataFilter>();

        group.MapGet("/game-state", GetGameState);
    }

    private static IResult GetGameState(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupLobbyStore lobbyStore,
        InMemoryGroupGameSessionStore gameSessionStore)
    {
        if (string.IsNullOrWhiteSpace(gameId))
            return Results.BadRequest(new { error = "Missing gameId." });

        var tg = httpContext.GetTelegramInitDataContext();

        if (string.IsNullOrWhiteSpace(tg.StartParam))
            return Results.BadRequest(new { error = "Missing signed start_param." });

        if (!string.Equals(gameId, tg.StartParam, StringComparison.Ordinal))
            return Results.BadRequest(new { error = "gameId does not match signed start_param." });

        if (!long.TryParse(tg.StartParam, NumberStyles.Integer, CultureInfo.InvariantCulture, out var groupChatId))
            return Results.BadRequest(new { error = "Invalid gameId." });

        var sessionState = gameSessionStore.GetPublicState(groupChatId);
        if (sessionState is not null)
        {
            var phase = sessionState.GameStatus is "Won" or "Lost" || sessionState.Session.Round.Status == GameRoundStatus.GameOver
                ? WebAppGamePhase.GameOver
                : WebAppGamePhase.InGame;

            var cockpit = MapCockpit(sessionState);
            var seat = MapViewerSeat(sessionState.Session, tg.Viewer.UserId);

            return Results.Ok(new WebAppGameStateResponse(
                GameId: groupChatId,
                Phase: phase,
                Lobby: null,
                Cockpit: cockpit,
                GameStatus: sessionState.GameStatus,
                Viewer: new WebAppViewer(tg.Viewer.UserId, seat)));
        }

        var lobby = lobbyStore.GetSnapshot(groupChatId);
        if (lobby is null)
            return Results.NotFound();

        var lobbyState = new WebAppLobbyState(
            Pilot: lobby.Pilot is null ? null : new WebAppLobbySeat(lobby.Pilot.UserId, lobby.Pilot.DisplayName),
            Copilot: lobby.Copilot is null ? null : new WebAppLobbySeat(lobby.Copilot.UserId, lobby.Copilot.DisplayName),
            IsReady: lobby.IsReady);

        var lobbySeat = MapViewerSeat(lobby, tg.Viewer.UserId);

        return Results.Ok(new WebAppGameStateResponse(
            GameId: groupChatId,
            Phase: WebAppGamePhase.Lobby,
            Lobby: lobbyState,
            Cockpit: null,
            GameStatus: "InProgress",
            Viewer: new WebAppViewer(tg.Viewer.UserId, lobbySeat)));
    }

    private static WebAppCockpitState MapCockpit(GameSessionPublicState state)
    {
        var cockpit = state.Cockpit;

        return new(
            RoundNumber: state.Session.Round.RoundNumber,
            RoundStatus: state.Session.Round.Status.ToString(),
            CurrentPlayer: state.CurrentPlayer?.ToString(),
            PlacementsMade: state.PlacementsMade,
            PlacementsRemaining: state.PlacementsRemaining,
            AxisPosition: cockpit.AxisPosition,
            EnginesSpeed: cockpit.EnginesSpeed,
            ApproachPosition: cockpit.ApproachPositionIndex + 1,
            ApproachSegments: cockpit.ApproachSegmentCount,
            PlanesRemaining: cockpit.TotalPlanesRemaining,
            CoffeeTokens: cockpit.CoffeeTokens,
            BrakesActivated: cockpit.BrakesActivatedSwitchCount,
            BrakesCapability: cockpit.BrakesCapability,
            FlapsValue: cockpit.FlapsValue,
            LandingGearValue: cockpit.LandingGearValue,
            BlueAerodynamicsThreshold: cockpit.BlueAerodynamicsThreshold,
            OrangeAerodynamicsThreshold: cockpit.OrangeAerodynamicsThreshold);
    }

    private static string? MapViewerSeat(GameSessionSnapshot session, long userId)
    {
        if (session.Pilot.UserId == userId) return "Pilot";
        if (session.Copilot.UserId == userId) return "Copilot";
        return null;
    }

    private static string? MapViewerSeat(LobbySnapshot lobby, long userId)
    {
        if (lobby.Pilot?.UserId == userId) return "Pilot";
        if (lobby.Copilot?.UserId == userId) return "Copilot";
        return null;
    }
}
