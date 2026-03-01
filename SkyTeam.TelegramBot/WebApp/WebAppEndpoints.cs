using System.Globalization;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Round;

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

public sealed record WebAppHandDie(int Index, int Value, bool IsUsed);

public sealed record WebAppHandCommand(string CommandId, string DisplayName);

public sealed record WebAppPrivateHandState(
    string Seat,
    string CurrentPlayer,
    int PlacementsRemaining,
    IReadOnlyList<WebAppHandDie> Dice,
    IReadOnlyList<WebAppHandCommand> AvailableCommands);

public sealed record WebAppPlaceDieRequest(int DieIndex, string CommandId);

public sealed record WebAppGameStateResponse(
    long GameId,
    WebAppGamePhase Phase,
    WebAppLobbyState? Lobby,
    WebAppCockpitState? Cockpit,
    string GameStatus,
    WebAppViewer Viewer,
    WebAppPrivateHandState? PrivateHand = null);

public static class WebAppEndpoints
{
    public static void MapWebAppEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webapp")
            .AddEndpointFilter<TelegramInitDataFilter>();

        group.MapGet("/game-state", GetGameState);
        group.MapPost("/lobby/new", CreateLobby);
        group.MapPost("/lobby/join", JoinLobby);
        group.MapPost("/lobby/start", StartGame);
        group.MapPost("/game/roll", RollGame);
        group.MapPost("/game/place", PlaceDie);
        group.MapPost("/game/undo", UndoLastPlacement);
    }

    private static IResult GetGameState(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupLobbyStore lobbyStore,
        InMemoryGroupGameSessionStore gameSessionStore)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        var sessionState = gameSessionStore.GetPublicState(result.GroupChatId!.Value);
        if (sessionState is not null)
        {
            return Results.Ok(MapStateResponse(
                sessionState,
                result.GroupChatId.Value,
                result.Context!.Viewer.UserId,
                gameSessionStore.GetHand(result.GroupChatId.Value, result.Context.Viewer.UserId)));
        }

        var lobby = lobbyStore.GetSnapshot(result.GroupChatId.Value);
        if (lobby is null)
            return Results.NotFound();

        return Results.Ok(MapStateResponse(lobby, result.GroupChatId.Value, result.Context!.Viewer.UserId));
    }

    private static async Task<IResult> CreateLobby(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupLobbyStore lobbyStore,
        InMemoryGroupGameSessionStore gameSessionStore,
        TelegramBotService telegramBotService,
        CancellationToken cancellationToken)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        var createResult = lobbyStore.CreateNew(result.GroupChatId!.Value);
        if (createResult.Status == LobbyCreateStatus.Created)
            await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId.Value, cancellationToken);

        return Results.Ok(MapStateResponse(createResult.Snapshot, result.GroupChatId.Value, result.Context!.Viewer.UserId));
    }

    private static async Task<IResult> JoinLobby(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupLobbyStore lobbyStore,
        InMemoryGroupGameSessionStore gameSessionStore,
        TelegramBotService telegramBotService,
        CancellationToken cancellationToken)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        var player = new LobbyPlayer(result.Context!.Viewer.UserId, result.Context.Viewer.DisplayName);
        var joinResult = lobbyStore.Join(result.GroupChatId!.Value, player);

        if (joinResult.Status is LobbyJoinStatus.JoinedAsPilot or LobbyJoinStatus.JoinedAsCopilot)
            await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId.Value, cancellationToken);

        return joinResult.Status switch
        {
            LobbyJoinStatus.JoinedAsPilot or LobbyJoinStatus.JoinedAsCopilot or LobbyJoinStatus.AlreadySeated
                => Results.Ok(MapStateResponse(lobbyStore.GetSnapshot(result.GroupChatId.Value)!, result.GroupChatId.Value, result.Context.Viewer.UserId)),
            LobbyJoinStatus.NoLobby => Results.Conflict(new { error = "No lobby yet. Press New first." }),
            LobbyJoinStatus.Full => Results.Conflict(new { error = "Lobby is full." }),
            _ => Results.Conflict(new { error = "Cannot join lobby." })
        };
    }

    private static async Task<IResult> StartGame(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupLobbyStore lobbyStore,
        InMemoryGroupGameSessionStore gameSessionStore,
        TelegramBotService telegramBotService,
        CancellationToken cancellationToken)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        var lobby = lobbyStore.GetSnapshot(result.GroupChatId!.Value);
        var startResult = gameSessionStore.Start(result.GroupChatId.Value, lobby, result.Context!.Viewer.UserId);

        if (startResult.Status is GameSessionStartStatus.Started or GameSessionStartStatus.AlreadyStarted)
        {
            await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId.Value, cancellationToken);
            var state = gameSessionStore.GetPublicState(result.GroupChatId.Value)!;
            return Results.Ok(MapStateResponse(
                state,
                result.GroupChatId.Value,
                result.Context.Viewer.UserId,
                gameSessionStore.GetHand(result.GroupChatId.Value, result.Context.Viewer.UserId)));
        }

        return startResult.Status switch
        {
            GameSessionStartStatus.NoLobby => Results.Conflict(new { error = "No lobby yet. Press New first." }),
            GameSessionStartStatus.LobbyNotReady => Results.Conflict(new { error = "Lobby needs two players before start." }),
            GameSessionStartStatus.NotSeated => Results.Conflict(new { error = "Only seated players can start." }),
            _ => Results.Conflict(new { error = "Cannot start game." })
        };
    }

    private static async Task<IResult> RollGame(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupLobbyStore lobbyStore,
        InMemoryGroupGameSessionStore gameSessionStore,
        TelegramBotService telegramBotService,
        CancellationToken cancellationToken)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        var lobby = lobbyStore.GetSnapshot(result.GroupChatId!.Value);
        if (lobby is null)
            return Results.Conflict(new { error = "No lobby yet. Press New first." });

        if (!lobby.IsReady)
            return Results.Conflict(new { error = "Lobby needs two players before roll." });

        if (gameSessionStore.GetSnapshot(result.GroupChatId.Value) is null)
            return Results.Conflict(new { error = "Game is not started yet. Press Start first." });

        var rollResult = gameSessionStore.RegisterRoll(result.GroupChatId.Value, SecretDiceRoller.Roll(() => Random.Shared.Next(1, 7)));
        if (rollResult.Status == GameSessionRollStatus.RoundNotAwaitingRoll)
            return Results.Conflict(new { error = "This round has already been rolled." });

        if (rollResult.Status == GameSessionRollStatus.NoSession)
            return Results.Conflict(new { error = "Game is not started yet. Press Start first." });

        await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId.Value, cancellationToken);
        var state = gameSessionStore.GetPublicState(result.GroupChatId.Value)!;

        return Results.Ok(MapStateResponse(
            state,
            result.GroupChatId.Value,
            result.Context!.Viewer.UserId,
            gameSessionStore.GetHand(result.GroupChatId.Value, result.Context.Viewer.UserId)));
    }

    private static async Task<IResult> PlaceDie(
        string? gameId,
        WebAppPlaceDieRequest request,
        HttpContext httpContext,
        InMemoryGroupGameSessionStore gameSessionStore,
        TelegramBotService telegramBotService,
        CancellationToken cancellationToken)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        if (request.DieIndex is < 0 or >= SecretDiceHand.DicePerHand)
            return Results.BadRequest(new { error = "Invalid die index." });

        if (string.IsNullOrWhiteSpace(request.CommandId))
            return Results.BadRequest(new { error = "Missing commandId." });

        var placement = gameSessionStore.PlaceDie(result.Context!.Viewer.UserId, request.DieIndex, request.CommandId);
        if (placement.Status != GamePlacementStatus.Placed)
            return Results.Conflict(new { error = MapPlacementError(placement) });

        await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId!.Value, cancellationToken);

        var state = gameSessionStore.GetPublicState(result.GroupChatId.Value)!;
        return Results.Ok(MapStateResponse(
            state,
            result.GroupChatId.Value,
            result.Context.Viewer.UserId,
            gameSessionStore.GetHand(result.GroupChatId.Value, result.Context.Viewer.UserId)));
    }

    private static async Task<IResult> UndoLastPlacement(
        string? gameId,
        HttpContext httpContext,
        InMemoryGroupGameSessionStore gameSessionStore,
        TelegramBotService telegramBotService,
        CancellationToken cancellationToken)
    {
        var result = ResolveRequestContext(gameId, httpContext);
        if (result.Error is not null)
            return result.Error;

        var undo = gameSessionStore.UndoLastPlacement(result.Context!.Viewer.UserId);
        if (undo.Status != GameUndoStatus.Undone)
            return Results.Conflict(new { error = MapUndoError(undo) });

        await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId!.Value, cancellationToken);

        var state = gameSessionStore.GetPublicState(result.GroupChatId.Value)!;
        return Results.Ok(MapStateResponse(
            state,
            result.GroupChatId.Value,
            result.Context.Viewer.UserId,
            gameSessionStore.GetHand(result.GroupChatId.Value, result.Context.Viewer.UserId)));
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

    private static WebAppGameStateResponse MapStateResponse(GameSessionPublicState sessionState, long groupChatId, long viewerUserId, GameHandResult handResult)
    {
        var phase = sessionState.GameStatus is "Won" or "Lost" || sessionState.Session.Round.Status == GameRoundStatus.GameOver
            ? WebAppGamePhase.GameOver
            : WebAppGamePhase.InGame;

        return new WebAppGameStateResponse(
            GameId: groupChatId,
            Phase: phase,
            Lobby: null,
            Cockpit: MapCockpit(sessionState),
            GameStatus: sessionState.GameStatus,
            Viewer: new WebAppViewer(viewerUserId, MapViewerSeat(sessionState.Session, viewerUserId)),
            PrivateHand: MapPrivateHand(handResult));
    }

    private static WebAppGameStateResponse MapStateResponse(LobbySnapshot lobby, long groupChatId, long viewerUserId)
    {
        var lobbyState = new WebAppLobbyState(
            Pilot: lobby.Pilot is null ? null : new WebAppLobbySeat(lobby.Pilot.UserId, lobby.Pilot.DisplayName),
            Copilot: lobby.Copilot is null ? null : new WebAppLobbySeat(lobby.Copilot.UserId, lobby.Copilot.DisplayName),
            IsReady: lobby.IsReady);

        return new WebAppGameStateResponse(
            GameId: groupChatId,
            Phase: WebAppGamePhase.Lobby,
            Lobby: lobbyState,
            Cockpit: null,
            GameStatus: "InProgress",
            Viewer: new WebAppViewer(viewerUserId, MapViewerSeat(lobby, viewerUserId)),
            PrivateHand: null);
    }

    private static WebAppPrivateHandState? MapPrivateHand(GameHandResult handResult)
    {
        if (handResult.Status != GameHandStatus.Ok || handResult.Seat is null || handResult.Hand is null || handResult.CurrentPlayer is null || handResult.PlacementsRemaining is null)
            return null;

        return new(
            Seat: handResult.Seat.Value.ToString(),
            CurrentPlayer: handResult.CurrentPlayer.Value.ToString(),
            PlacementsRemaining: handResult.PlacementsRemaining.Value,
            Dice: handResult.Hand.Dice.Select(d => new WebAppHandDie(d.Index, d.Value.Value, d.IsUsed)).ToArray(),
            AvailableCommands: (handResult.AvailableCommands ?? []).Select(c => new WebAppHandCommand(c.CommandId, c.DisplayName)).ToArray());
    }

    private static string MapPlacementError(GamePlacementResult result)
        => result.Status switch
        {
            GamePlacementStatus.NoActiveSession => "No active game session found.",
            GamePlacementStatus.NotSeated => "You are not seated as Pilot/Copilot in the active game.",
            GamePlacementStatus.RoundNotRolled => "This round has not been rolled yet.",
            GamePlacementStatus.RoundNotAcceptingPlacements => "This round is not accepting placements.",
            GamePlacementStatus.NotPlayersTurn => "It is not your turn.",
            GamePlacementStatus.InvalidDieIndex => "Invalid die index.",
            GamePlacementStatus.DieAlreadyUsed => "That die has already been used.",
            GamePlacementStatus.InvalidTarget => "A command id is required.",
            GamePlacementStatus.CommandDoesNotMatchDie => "That command does not match the selected die.",
            GamePlacementStatus.CommandNotAvailable => "That command is not currently available.",
            GamePlacementStatus.DomainError => result.ErrorMessage ?? "Cannot place die (domain error).",
            _ => "Cannot place die."
        };

    private static string MapUndoError(GameUndoResult result)
        => result.Status switch
        {
            GameUndoStatus.NoActiveSession => "No active game session found.",
            GameUndoStatus.NotSeated => "You are not seated as Pilot/Copilot in the active game.",
            GameUndoStatus.RoundNotRolled => "This round has not been rolled yet.",
            GameUndoStatus.UndoNotAllowed => "Undo not allowed. You can only undo your last placement before the other player places.",
            GameUndoStatus.DomainError => result.ErrorMessage ?? "Cannot undo (domain error).",
            _ => "Cannot undo."
        };

    private static (long? GroupChatId, TelegramInitDataContext? Context, IResult? Error) ResolveRequestContext(string? gameId, HttpContext httpContext)
    {
        var tg = httpContext.GetTelegramInitDataContext();

        long? requestedGameId = null;
        if (!string.IsNullOrWhiteSpace(gameId))
        {
            if (!long.TryParse(gameId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedGameId))
                return (null, tg, Results.BadRequest(new { error = "Invalid gameId." }));

            requestedGameId = parsedGameId;
        }

        if (tg.Chat is not null)
        {
            if (requestedGameId is not null && requestedGameId.Value != tg.Chat.ChatId)
                return (null, tg, Results.BadRequest(new { error = "gameId does not match signed chat context." }));

            return (tg.Chat.ChatId, tg, null);
        }

        if (!string.IsNullOrWhiteSpace(tg.StartParam))
        {
            if (!long.TryParse(tg.StartParam, NumberStyles.Integer, CultureInfo.InvariantCulture, out var groupChatId))
                return (null, tg, Results.BadRequest(new { error = "Invalid gameId." }));

            if (requestedGameId is not null && requestedGameId.Value != groupChatId)
                return (null, tg, Results.BadRequest(new { error = "gameId does not match signed start_param." }));

            return (groupChatId, tg, null);
        }

        return (null, tg, Results.BadRequest(new { error = "Missing chat context and signed start_param." }));
    }
}
