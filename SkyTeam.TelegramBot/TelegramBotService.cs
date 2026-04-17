using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Options;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Presentation;
using SkyTeam.Application.Round;
using SkyTeam.TelegramBot.WebApp;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SkyTeam.TelegramBot;

public sealed class TelegramBotService(
    InMemoryGroupLobbyStore lobbyStore,
    InMemoryGroupGameSessionStore gameSessionStore,
    IOptions<TelegramBotOptions> botOptions,
    IOptions<WebAppOptions> webAppOptions,
    ILogger<TelegramBotService> logger) : BackgroundService
{
    private const int MaxRecentUpdateIds = 1_000;
    private const int MaxRecentTurnNotificationKeys = 2_000;
    private const string NewCallbackAction = "new";
    private const string JoinCallbackAction = "join";
    private const string StartCallbackAction = "start";
    private const string RollCallbackAction = "roll";
    private const string PlaceDmCallbackAction = "place-dm";
    private const string RefreshCallbackAction = "refresh";
    private const string ExpiredMenuToast = "Menu expired — press /sky state";
    private const string DuplicateCallbackToast = "Already processed.";

    private static readonly string NewCallbackData = CallbackDataCodec.EncodeGroupAction(NewCallbackAction);
    private static readonly string JoinCallbackData = CallbackDataCodec.EncodeGroupAction(JoinCallbackAction);
    private static readonly string StartCallbackData = CallbackDataCodec.EncodeGroupAction(StartCallbackAction);
    private static readonly string RollCallbackData = CallbackDataCodec.EncodeGroupAction(RollCallbackAction);
    private static readonly string PlaceDmCallbackData = CallbackDataCodec.EncodeGroupAction(PlaceDmCallbackAction);
    private static readonly string RefreshCallbackData = CallbackDataCodec.EncodeGroupAction(RefreshCallbackAction);

    private readonly ConcurrentDictionary<long, SemaphoreSlim> _chatLocks = new();
    private readonly CallbackMenuStateStore _callbackMenuStateStore = new();

    private ITelegramBotClient? _botClient;
    private string? _botUsername;

    private readonly Lock _updateDedupSync = new();
    private readonly Queue<int> _recentUpdateIds = new();
    private readonly HashSet<int> _recentUpdateIdSet = [];
    private readonly Lock _turnNotificationDedupSync = new();
    private readonly Queue<string> _recentTurnNotificationKeys = new();
    private readonly HashSet<string> _recentTurnNotificationKeySet = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = botOptions.Value.BotToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogError("Missing TELEGRAM_BOT_TOKEN environment variable.");
            return;
        }

        var botClient = new TelegramBotClient(token);
        _botClient = botClient;

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            new() { AllowedUpdates = [] },
            stoppingToken);

        var me = await botClient.GetMe(stoppingToken);
        _botUsername = me.Username;
        logger.LogInformation("Started Telegram bot @{Username}.", me.Username);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (IsDuplicateUpdate(update.Id)) return;
        var lockKey = GetLockKey(update);
        if (lockKey is null) return;

        var gate = _chatLocks.GetOrAdd(lockKey.Value, _ => new(1, 1));

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (update.Message is { Text: { } text } message)
            {
                await HandleMessageAsync(botClient, message, text, cancellationToken);
                return;
            }

            if (update.CallbackQuery is { } callbackQuery)
                await HandleCallbackQueryAsync(botClient, callbackQuery, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task HandleMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        string text,
        CancellationToken cancellationToken)
    {
        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        if (IsCommand(parts[0], "/start"))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Sky Team bot is online. In a group chat, try: /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        if (IsCommand(parts[0], "/sky"))
        {
            await HandleSkyAsync(botClient, message, parts.Skip(1).ToArray(), cancellationToken);
            return;
        }

        await botClient.SendMessage(
            message.Chat.Id,
            "Unknown command. Try /sky new",
            cancellationToken: cancellationToken);
    }

    private async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is null || callbackQuery.Data is null)
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, ExpiredMenuToast, cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var toast = await HandleGroupCockpitCallbackAsync(botClient, callbackQuery, cancellationToken);

            await botClient.AnswerCallbackQuery(callbackQuery.Id, toast, cancellationToken: cancellationToken);
        }
        catch
        {
            await botClient.AnswerCallbackQuery(callbackQuery.Id, ExpiredMenuToast, cancellationToken: cancellationToken);
        }
    }

    private async Task<string?> HandleGroupCockpitCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var groupChatId = callbackQuery.Message!.Chat.Id;

        if (!CallbackDataCodec.TryDecodeGroupAction(callbackQuery.Data, out var callbackData))
            return ExpiredMenuToast;

        var menuValidation = _callbackMenuStateStore.ValidateAndMarkProcessed(
            callbackQuery.From.Id,
            groupChatId,
            callbackQuery.Message.MessageId,
            callbackData);

        if (menuValidation == CallbackMenuValidationStatus.UnknownOrExpired)
            return ExpiredMenuToast;

        if (menuValidation == CallbackMenuValidationStatus.Duplicate)
            return DuplicateCallbackToast;

        return callbackData switch
        {
            _ when callbackData == NewCallbackData => await HandleLobbyNewFromCallbackAsync(botClient, groupChatId, cancellationToken),
            _ when callbackData == JoinCallbackData => await HandleLobbyJoinFromCallbackAsync(botClient, callbackQuery.From, groupChatId, cancellationToken),
            _ when callbackData == StartCallbackData => await HandleLobbyStartFromCallbackAsync(botClient, callbackQuery.From, groupChatId, cancellationToken),
            _ when callbackData == RollCallbackData => await HandleInGameRollFromCallbackAsync(botClient, groupChatId, cancellationToken),
            _ when callbackData == PlaceDmCallbackData => await HandleInGamePlaceFromCallbackAsync(botClient, callbackQuery.From, cancellationToken),
            _ when callbackData == RefreshCallbackData => await HandleLobbyRefreshFromCallbackAsync(botClient, groupChatId, cancellationToken),
            _ => ExpiredMenuToast
        };
    }

    private async Task<string?> HandleLobbyNewFromCallbackAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var result = lobbyStore.CreateNew(groupChatId);
        if (result.Status != LobbyCreateStatus.Created)
            return "Lobby already exists.";

        await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
        return null;
    }

    private async Task<string?> HandleLobbyJoinFromCallbackAsync(
        ITelegramBotClient botClient,
        User user,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var player = new LobbyPlayer(user.Id, GetDisplayName(user));
        var result = lobbyStore.Join(groupChatId, player);

        if (result.Status is LobbyJoinStatus.JoinedAsPilot or LobbyJoinStatus.JoinedAsCopilot)
        {
            await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
            return null;
        }

        return result.Status switch
        {
            LobbyJoinStatus.NoLobby => "No lobby yet. Press New first.",
            LobbyJoinStatus.AlreadySeated => "You are already seated.",
            LobbyJoinStatus.Full => "Lobby is full.",
            _ => "Cannot join lobby."
        };
    }

    private async Task<string?> HandleLobbyStartFromCallbackAsync(
        ITelegramBotClient botClient,
        User user,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var lobbySnapshot = lobbyStore.GetSnapshot(groupChatId);
        var result = gameSessionStore.Start(groupChatId, lobbySnapshot, user.Id);

        if (result.Status == GameSessionStartStatus.Started)
            ForgetTurnNotificationKeysForGroup(groupChatId);

        if (result.Status is GameSessionStartStatus.Started or GameSessionStartStatus.AlreadyStarted)
        {
            await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
            return null;
        }

        return result.Status switch
        {
            GameSessionStartStatus.NoLobby => "No lobby yet. Press New first.",
            GameSessionStartStatus.LobbyNotReady => "Lobby needs two players before start.",
            GameSessionStartStatus.NotSeated => "Only seated players can start.",
            _ => "Cannot start game."
        };
    }

    private async Task<string?> HandleLobbyRefreshFromCallbackAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
        return null;
    }

    private async Task<string?> HandleInGameRollFromCallbackAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        await HandleGroupRollAsync(botClient, groupChatId, cancellationToken);
        return null;
    }

    private async Task<string?> HandleInGamePlaceFromCallbackAsync(
        ITelegramBotClient botClient,
        User user,
        CancellationToken cancellationToken)
    {
        if (!gameSessionStore.TryGetGroupChatIdForUserId(user.Id, out _))
            return "No active game session found for you.";

        return "Secret hand/place/undo actions are Mini App-only. Press Open app.";
    }

    private async Task HandleSkyAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        var subcommand = args.FirstOrDefault()?.Trim().ToLowerInvariant();

        if (message.Chat.Type == ChatType.Private)
            switch (subcommand)
            {
                case "hand":
                    await HandleSkyHandAsync(botClient, message, cancellationToken);
                    return;

                case "place":
                    await HandleSkyPlaceAsync(botClient, message, args.Skip(1).ToArray(), cancellationToken);
                    return;

                case "undo":
                    await HandleSkyUndoAsync(botClient, message, cancellationToken);
                    return;

                default:
                    await botClient.SendMessage(
                        message.Chat.Id,
                        "In a group chat, try: /sky new | /sky app\n\nIn private chat: /sky hand | /sky place <dieIndex> <commandId> | /sky undo",
                        cancellationToken: cancellationToken);
                    return;
            }

        if (message.Chat.Type is not (ChatType.Group or ChatType.Supergroup))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Lobby commands are group-chat-only. Add me to a group and run /sky new.",
                cancellationToken: cancellationToken);
            return;
        }

        switch (subcommand)
        {
            case "new":
                await HandleSkyNewAsync(botClient, message, cancellationToken);
                return;

            case "join":
                await HandleSkyJoinAsync(botClient, message, cancellationToken);
                return;

            case "state":
                await HandleSkyStateAsync(botClient, message, cancellationToken);
                return;

            case "start":
                await HandleSkyStartAsync(botClient, message, cancellationToken);
                return;

            case "roll":
                await HandleSkyRollAsync(botClient, message, cancellationToken);
                return;

            case "app":
                await HandleSkyAppAsync(botClient, message, cancellationToken);
                return;

            case "hand":
            case "place":
                await RedirectSecretPathToMiniAppAsync(botClient, message, cancellationToken);
                return;

            case "undo":
                await RedirectSecretPathToMiniAppAsync(botClient, message, cancellationToken);
                return;

            default:
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Usage: /sky new | /sky join | /sky state | /sky start | /sky roll | /sky app",
                    cancellationToken: cancellationToken);
                return;
        }
    }

    private async Task HandleSkyNewAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        lobbyStore.CreateNew(message.Chat.Id);
        await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
    }

    private async Task HandleSkyJoinAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(message.Chat.Id, "Cannot identify you in this chat.", cancellationToken: cancellationToken);
            return;
        }

        var player = new LobbyPlayer(message.From.Id, GetDisplayName(message.From));

        var result = lobbyStore.Join(message.Chat.Id, player);
        if (result.Status == LobbyJoinStatus.NoLobby)
        {
            await botClient.SendMessage(message.Chat.Id, "No lobby yet. Create one with /sky new", cancellationToken: cancellationToken);
            return;
        }

        await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
    }

    private async Task HandleSkyStateAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
        => await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);

    private async Task HandleSkyAppAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var botUsername = await GetBotUsernameAsync(botClient, cancellationToken);
        if (string.IsNullOrWhiteSpace(botUsername))
        {
            await botClient.SendMessage(message.Chat.Id, "Bot username is not available yet. Try again in a moment.", cancellationToken: cancellationToken);
            return;
        }

        var groupChatId = message.Chat.Id;
        if (!TryBuildStartAppUrl(botUsername, groupChatId, webAppOptions.Value.MiniAppShortName, out var startAppUrl))
        {
            await botClient.SendMessage(
                groupChatId,
                "Open app link is temporarily unavailable. Run /sky state in this group and try again.",
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(
            groupChatId,
            "Open the Sky Team Mini App:",
            replyMarkup: BuildOpenAppKeyboard(startAppUrl),
            cancellationToken: cancellationToken);
    }

    private async Task<string?> GetBotUsernameAsync(ITelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_botUsername))
            return _botUsername;

        var me = await botClient.GetMe(cancellationToken);
        _botUsername = me.Username;
        return _botUsername;
    }

    private async Task HandleSkyStartAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(message.Chat.Id, "Cannot identify you in this chat.", cancellationToken: cancellationToken);
            return;
        }

        var lobbySnapshot = lobbyStore.GetSnapshot(message.Chat.Id);
        var result = gameSessionStore.Start(message.Chat.Id, lobbySnapshot, message.From.Id);

        var text = result.Status switch
        {
            GameSessionStartStatus.NoLobby => "No lobby yet. Create one with /sky new",
            GameSessionStartStatus.LobbyNotReady => "Lobby is not ready yet. Two players must /sky join before starting.",
            GameSessionStartStatus.NotSeated => "Only seated players (Pilot/Copilot) can start the game. Join with /sky join",
            GameSessionStartStatus.AlreadyStarted => "Game already started. Next: /sky roll",
            GameSessionStartStatus.Started => "Game started. Round 1 initialized (no placements yet).\n\nNext: /sky roll",
            _ => "Cannot start game."
        };

        if (result.Status == GameSessionStartStatus.Started)
            ForgetTurnNotificationKeysForGroup(message.Chat.Id);

        if (result.Status is GameSessionStartStatus.Started or GameSessionStartStatus.AlreadyStarted)
        {
            await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
            return;
        }

        await botClient.SendMessage(message.Chat.Id, text, cancellationToken: cancellationToken);
    }

    private async Task HandleSkyRollAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
        => await HandleGroupRollAsync(botClient, message.Chat.Id, cancellationToken);

    private async Task HandleGroupRollAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        if (gameSessionStore.GetSnapshot(groupChatId) is null)
        {
            var snapshot = lobbyStore.GetSnapshot(groupChatId);
            if (snapshot is null)
            {
                await botClient.SendMessage(groupChatId, "No lobby yet. Create one with /sky new", cancellationToken: cancellationToken);
                return;
            }

            if (!snapshot.IsReady)
            {
                await botClient.SendMessage(groupChatId, "Lobby is not ready yet. Two players must /sky join before rolling dice.", cancellationToken: cancellationToken);
                return;
            }

            await botClient.SendMessage(groupChatId, "Game is not started yet. Run /sky start first.", cancellationToken: cancellationToken);
            return;
        }

        var roll = SecretDiceRoller.Roll(() => Random.Shared.Next(1, 7));
        var rollResult = gameSessionStore.RegisterRoll(groupChatId, roll);

        if (rollResult.Status == GameSessionRollStatus.RoundNotAwaitingRoll)
        {
            await botClient.SendMessage(groupChatId, "This round has already been rolled. Open /sky app to view your private hand and place dice.", cancellationToken: cancellationToken);
            return;
        }

        await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
        await NotifyCurrentTurnAsync(
            botClient,
            groupChatId,
            transitionKey: $"roll:{rollResult.Snapshot?.Round.RoundNumber ?? 0}",
            cancellationToken: cancellationToken);
        await botClient.SendMessage(groupChatId, "Dice rolled. Open /sky app to view your private hand and continue.", cancellationToken: cancellationToken);
    }

    private async Task HandleSkyHandAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        await RedirectSecretPathToMiniAppAsync(botClient, message, cancellationToken);
    }

    private async Task HandleSkyPlaceAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        await RedirectSecretPathToMiniAppAsync(botClient, message, cancellationToken);
    }

    private async Task HandleSkyUndoAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        await RedirectSecretPathToMiniAppAsync(botClient, message, cancellationToken);
    }

    private async Task RedirectSecretPathToMiniAppAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(message.Chat.Id, "Cannot identify you in this chat.", cancellationToken: cancellationToken);
            return;
        }

        if (!TryResolveSecretFlowGroupChatId(message, out var groupChatId))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Secret hand/place/undo actions are Mini App-only. Start or reopen the game from the group chat with /sky app.",
                cancellationToken: cancellationToken);
            return;
        }

        var botUsername = await GetBotUsernameAsync(botClient, cancellationToken);
        if (string.IsNullOrWhiteSpace(botUsername))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Secret hand/place/undo actions are Mini App-only. Use /sky app in your group chat.",
                cancellationToken: cancellationToken);
            return;
        }

        if (!TryBuildStartAppUrl(botUsername, groupChatId, webAppOptions.Value.MiniAppShortName, out var startAppUrl))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Secret hand/place/undo actions are Mini App-only. Open app link is temporarily unavailable; run /sky state in your group chat and retry.",
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(
            message.Chat.Id,
            "Secret hand/place/undo actions are Mini App-only. Open the Sky Team Mini App:",
            replyMarkup: BuildOpenAppKeyboard(startAppUrl),
            cancellationToken: cancellationToken);
    }

    private static string RenderHand(SecretDiceHand hand)
        => string.Join("\n", hand.Dice.Select(die => $"{die.Index}:{die.Value.Value}{(die.IsUsed ? " (used)" : string.Empty)}"));

    private static string RenderCommands(IReadOnlyList<AvailableGameCommand> commands)
    {
        if (commands.Count == 0)
            return "(none)";

        return string.Join("\n", commands.Select(c => $"- {c.DisplayName} ({c.CommandId})"));
    }

    private async Task RefreshGroupCockpitAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var text = RenderGroupState(groupChatId);

        if (string.IsNullOrWhiteSpace(_botUsername))
            _botUsername = await GetBotUsernameAsync(botClient, cancellationToken);

        var replyMarkup = BuildGroupStateKeyboard(groupChatId, _botUsername, webAppOptions.Value.MiniAppShortName);

        if (gameSessionStore.TryGetCockpitMessageId(groupChatId, out var cockpitMessageId))
            if (await TryEditCockpitAsync(botClient, groupChatId, cockpitMessageId, text, replyMarkup, cancellationToken))
            {
                RegisterGroupCockpitMenuState(groupChatId, cockpitMessageId);
                return;
            }

        var cockpitMessage = await botClient.SendMessage(
            groupChatId,
            text,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);

        gameSessionStore.SetCockpitMessageId(groupChatId, cockpitMessage.MessageId);
        RegisterGroupCockpitMenuState(groupChatId, cockpitMessage.MessageId);
        await TryPinCockpitAsync(botClient, groupChatId, cockpitMessage.MessageId, cancellationToken);
    }

    internal async Task RefreshGroupCockpitFromWebAppAsync(long groupChatId, CancellationToken cancellationToken)
    {
        if (_botClient is null)
            return;

        await RefreshGroupCockpitAsync(_botClient, groupChatId, cancellationToken);
    }

    internal async Task NotifyCurrentTurnFromWebAppAsync(long groupChatId, string transitionKey, CancellationToken cancellationToken)
    {
        if (_botClient is null)
            return;

        await NotifyCurrentTurnAsync(_botClient, groupChatId, transitionKey, cancellationToken);
    }

    private static async Task<bool> TryEditCockpitAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        int cockpitMessageId,
        string text,
        InlineKeyboardMarkup replyMarkup,
        CancellationToken cancellationToken)
    {
        try
        {
            await botClient.EditMessageText(
                groupChatId,
                cockpitMessageId,
                text,
                replyMarkup: replyMarkup,
                cancellationToken: cancellationToken);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task TryPinCockpitAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        int cockpitMessageId,
        CancellationToken cancellationToken)
    {
        try
        {
            await botClient.PinChatMessage(groupChatId, cockpitMessageId, cancellationToken: cancellationToken);
        }
        catch
        {
            // best effort
        }
    }

    private async Task NotifyCurrentTurnAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        string transitionKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transitionKey))
            return;

        var session = gameSessionStore.GetSnapshot(groupChatId);
        var state = gameSessionStore.GetPublicState(groupChatId);

        if (session is null || state?.CurrentPlayer is null || state.Session.Round.Status != GameRoundStatus.AwaitingPlacements)
            return;

        var seat = state.CurrentPlayer.Value;
        var recipient = seat == PlayerSeat.Pilot ? session.Pilot : session.Copilot;

        var dedupKey = $"{groupChatId}:{transitionKey}:{recipient.UserId}:{seat}";
        if (!TryRegisterTurnNotification(dedupKey))
            return;

        var placementsRemaining = state.PlacementsRemaining ?? 0;
        var placementLabel = placementsRemaining == 1 ? "placement" : "placements";

        var messageText =
            $"🔔 Your turn in Sky Team ({seat}).\n" +
            $"Round {state.Session.Round.RoundNumber}, {placementsRemaining} {placementLabel} remaining.\n" +
            $"Game status: {state.GameStatus}\n" +
            "Action required: open /sky app and place one die.";

        if (await TrySendDirectMessageAsync(botClient, recipient.UserId, messageText, cancellationToken))
            return;

        await TrySendGroupTurnFallbackAsync(botClient, groupChatId, recipient, seat, cancellationToken);
    }

    private bool TryRegisterTurnNotification(string key)
    {
        lock (_turnNotificationDedupSync)
        {
            if (!_recentTurnNotificationKeySet.Add(key))
                return false;

            _recentTurnNotificationKeys.Enqueue(key);

            while (_recentTurnNotificationKeys.Count > MaxRecentTurnNotificationKeys)
            {
                var oldest = _recentTurnNotificationKeys.Dequeue();
                _recentTurnNotificationKeySet.Remove(oldest);
            }

            return true;
        }
    }

    private void ForgetTurnNotificationKeysForGroup(long groupChatId)
    {
        var keyPrefix = $"{groupChatId}:";

        lock (_turnNotificationDedupSync)
        {
            if (_recentTurnNotificationKeys.Count == 0)
                return;

            var retainedKeys = new Queue<string>(_recentTurnNotificationKeys.Count);

            while (_recentTurnNotificationKeys.Count > 0)
            {
                var existingKey = _recentTurnNotificationKeys.Dequeue();
                if (existingKey.StartsWith(keyPrefix, StringComparison.Ordinal))
                    continue;

                retainedKeys.Enqueue(existingKey);
            }

            _recentTurnNotificationKeySet.Clear();

            while (retainedKeys.Count > 0)
            {
                var retainedKey = retainedKeys.Dequeue();
                _recentTurnNotificationKeys.Enqueue(retainedKey);
                _recentTurnNotificationKeySet.Add(retainedKey);
            }
        }
    }

    private async Task TrySendGroupTurnFallbackAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        LobbyPlayer recipient,
        PlayerSeat seat,
        CancellationToken cancellationToken)
    {
        var recipientDisplayName = string.IsNullOrWhiteSpace(recipient.DisplayName)
            ? seat.ToString()
            : recipient.DisplayName;

        try
        {
            await botClient.SendMessage(
                groupChatId,
                $"🔔 {recipientDisplayName} ({seat}), your turn. Open /sky app and place one die.",
                cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Turn notification fallback failed for chat {GroupChatId} and user {UserId}.",
                groupChatId,
                recipient.UserId);
        }
    }

    private static async Task<bool> TrySendDirectMessageAsync(
        ITelegramBotClient botClient,
        long userId,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            await botClient.SendMessage(userId, text, cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> TrySendSecretDiceAsync(
        ITelegramBotClient botClient,
        LobbyPlayer recipient,
        string seat,
        IReadOnlyList<int> dice,
        bool isYourTurn,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(dice);

        var diceText = string.Join(", ", dice.Select((value, index) => $"{index}:{value}"));
        var turnText = isYourTurn ? "It's your turn to place first." : "Wait for the other player to place first.";

        var messageText =
            $"{seat} secret dice: {diceText}\n\n{turnText}\n\nCommands:\n/sky hand\n/sky place <dieIndex> <commandId>\n/sky undo";

        try
        {
            await botClient.SendMessage(recipient.UserId, messageText, cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string RenderGroupState(long groupChatId, InMemoryGroupLobbyStore lobbyStore, InMemoryGroupGameSessionStore gameSessionStore)
    {
        var inGame = gameSessionStore.GetPublicState(groupChatId);
        if (inGame is not null)
            return GroupChatCockpitRenderer.RenderInGame(inGame);

        var snapshot = lobbyStore.GetSnapshot(groupChatId);
        return snapshot is null
            ? "No lobby yet. Create one with /sky new"
            : GroupChatCockpitRenderer.RenderLobby(snapshot);
    }

    private string RenderGroupState(long groupChatId)
        => RenderGroupState(groupChatId, lobbyStore, gameSessionStore);

    private void RegisterGroupCockpitMenuState(long groupChatId, int messageId)
    {
        _callbackMenuStateStore.RegisterGroupMenu(
            groupChatId,
            messageId,
            [NewCallbackData, JoinCallbackData, StartCallbackData, RollCallbackData, PlaceDmCallbackData, RefreshCallbackData]);
    }

    private static InlineKeyboardMarkup BuildGroupStateKeyboard(long groupChatId, string? botUsername, string? miniAppShortName)
    {
        InlineKeyboardButton[] secondRow = TryBuildStartAppUrl(botUsername, groupChatId, miniAppShortName, out var startAppUrl)
            ? [InlineKeyboardButton.WithUrl("Open app", startAppUrl), InlineKeyboardButton.WithCallbackData("Refresh", RefreshCallbackData)]
            : [InlineKeyboardButton.WithCallbackData("Refresh", RefreshCallbackData)];

        return new([
            [
                InlineKeyboardButton.WithCallbackData("New", NewCallbackData),
                InlineKeyboardButton.WithCallbackData("Join", JoinCallbackData),
                InlineKeyboardButton.WithCallbackData("Start", StartCallbackData)
            ],
            secondRow
        ]);
    }

    private static InlineKeyboardMarkup BuildOpenAppKeyboard(string startAppUrl)
        => new([[InlineKeyboardButton.WithUrl("Open app", startAppUrl)]]);

    private static bool TryBuildStartAppUrl(string? botUsername, long groupChatId, string? miniAppShortName, out string startAppUrl)
    {
        startAppUrl = string.Empty;
        if (groupChatId == 0 || string.IsNullOrWhiteSpace(botUsername))
            return false;

        var username = botUsername.Trim().TrimStart('@');
        if (!IsValidBotUsername(username))
            return false;

        startAppUrl = BuildStartAppUrl(username, groupChatId, miniAppShortName);
        return true;
    }

    private static bool IsValidBotUsername(string username)
    {
        if (username.Length is < 5 or > 32)
            return false;

        return username.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
    }

    private static bool TryNormalizeMiniAppShortName(string? value, out string? miniAppShortName)
    {
        miniAppShortName = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var candidate = value.Trim();
        if (candidate.StartsWith('/'))
            candidate = candidate.TrimStart('/');

        if (candidate.Length is < 3 or > 32)
            return false;

        if (!candidate.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
            return false;

        miniAppShortName = candidate;
        return true;
    }

    private static string BuildStartAppUrl(string botUsername, long groupChatId, string? miniAppShortName)
    {
        // var encodedGroupChatId = Uri.EscapeDataString(groupChatId.ToString(CultureInfo.InvariantCulture));
        // if (TryNormalizeMiniAppShortName(miniAppShortName, out var normalizedMiniAppShortName) && normalizedMiniAppShortName is not null)
        //     return $"https://t.me/{botUsername}/{normalizedMiniAppShortName}?startapp={encodedGroupChatId}";

        return $"https://t.me/{botUsername}?startapp";
    }

    private bool TryResolveSecretFlowGroupChatId(Message message, out long groupChatId)
    {
        switch (message.Chat.Type)
        {
            case ChatType.Group:
            case ChatType.Supergroup:
                groupChatId = message.Chat.Id;
                return true;
            case ChatType.Private when message.From is not null:
                return gameSessionStore.TryGetGroupChatIdForUserId(message.From.Id, out groupChatId);
            default:
                groupChatId = default;
                return false;
        }
    }

    private long? GetLockKey(Update update)
    {
        if (update.Message is not null)
            return GetLockKey(update.Message);

        if (update.CallbackQuery is not null)
            return GetLockKey(update.CallbackQuery);

        return null;
    }

    private long GetLockKey(Message message)
    {
        if (message.Chat.Type != ChatType.Private) return message.Chat.Id;
        if (message.From is null) return message.Chat.Id;

        return gameSessionStore.TryGetGroupChatIdForUserId(message.From.Id, out var groupChatId)
            ? groupChatId
            : message.Chat.Id;
    }

    private long GetLockKey(CallbackQuery callbackQuery)
    {
        if (callbackQuery.Message is not null)
            return GetLockKey(callbackQuery.Message);

        return gameSessionStore.TryGetGroupChatIdForUserId(callbackQuery.From.Id, out var groupChatId)
            ? groupChatId
            : callbackQuery.From.Id;
    }

    private bool IsDuplicateUpdate(int updateId)
    {
        lock (_updateDedupSync)
        {
            if (!_recentUpdateIdSet.Add(updateId)) return true;

            _recentUpdateIds.Enqueue(updateId);

            while (_recentUpdateIds.Count > MaxRecentUpdateIds)
            {
                var oldest = _recentUpdateIds.Dequeue();
                _recentUpdateIdSet.Remove(oldest);
            }

            return false;
        }
    }

    private static string GetDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Username))
            return "@" + user.Username;

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            return user.FirstName;

        return user.Id.ToString();
    }

    private static bool IsCommand(string token, string command)
    {
        if (!token.StartsWith(command, StringComparison.OrdinalIgnoreCase)) return false;
        if (token.Length == command.Length) return true;
        return token.Length > command.Length && token[command.Length] == '@';
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling error.");
        return Task.CompletedTask;
    }
}
