using System.Collections.Concurrent;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Presentation;
using SkyTeam.Application.Round;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SkyTeam.TelegramBot;

static class Program
{
    private const int MaxRecentUpdateIds = 1_000;
    private const string NewCallbackData = "v1:grp:new";
    private const string JoinCallbackData = "v1:grp:join";
    private const string StartCallbackData = "v1:grp:start";
    private const string RefreshCallbackData = "v1:grp:refresh";
    private const string ExpiredMenuToast = "Menu expired — press /sky state";

    private static readonly InMemoryGroupLobbyStore LobbyStore = new();
    private static readonly InMemoryGroupGameSessionStore GameSessionStore = new();

    private static readonly ConcurrentDictionary<long, SemaphoreSlim> ChatLocks = new();

    private static readonly Lock UpdateDedupSync = new();
    private static readonly Queue<int> RecentUpdateIds = new();
    private static readonly HashSet<int> RecentUpdateIdSet = [];

    public static async Task Main()
    {
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            await Console.Error.WriteLineAsync("Missing TELEGRAM_BOT_TOKEN environment variable.");
            return;
        }

        var botClient = new TelegramBotClient(token);

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            new() { AllowedUpdates = [] },
            cts.Token);

        var me = await botClient.GetMe(cts.Token);
        Console.WriteLine($"Started Telegram bot @{me.Username}. Press Ctrl+C to stop.");

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (IsDuplicateUpdate(update.Id)) return;
        var lockKey = GetLockKey(update);
        if (lockKey is null) return;

        var gate = ChatLocks.GetOrAdd(lockKey.Value, _ => new(1, 1));

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

    private static async Task HandleMessageAsync(
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
            await HandleSkyAsync(botClient, message, parts.Skip(1).ToArray(),
                cancellationToken);
            return;
        }

        await botClient.SendMessage(
            message.Chat.Id,
            "Unknown command. Try /sky new",
            cancellationToken: cancellationToken);
    }

    private static async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        if (callbackQuery.Message is null || callbackQuery.Data is null)
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                ExpiredMenuToast,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var toast = await HandleGroupCockpitCallbackAsync(
                botClient,
                callbackQuery,
                cancellationToken);

            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                toast,
                cancellationToken: cancellationToken);
        }
        catch
        {
            await botClient.AnswerCallbackQuery(
                callbackQuery.Id,
                ExpiredMenuToast,
                cancellationToken: cancellationToken);
        }
    }

    private static async Task<string?> HandleGroupCockpitCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var groupChatId = callbackQuery.Message!.Chat.Id;
        return callbackQuery.Data switch
        {
            NewCallbackData => await HandleLobbyNewFromCallbackAsync(
                botClient,
                groupChatId,
                cancellationToken),
            JoinCallbackData => await HandleLobbyJoinFromCallbackAsync(
                botClient,
                callbackQuery.From,
                groupChatId,
                cancellationToken),
            StartCallbackData => await HandleLobbyStartFromCallbackAsync(
                botClient,
                callbackQuery.From,
                groupChatId,
                cancellationToken),
            RefreshCallbackData => await HandleLobbyRefreshFromCallbackAsync(
                botClient,
                groupChatId,
                cancellationToken),
            _ => ExpiredMenuToast
        };
    }

    private static async Task<string?> HandleLobbyNewFromCallbackAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var result = LobbyStore.CreateNew(groupChatId);
        if (result.Status != LobbyCreateStatus.Created)
            return "Lobby already exists.";

        await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
        return null;
    }

    private static async Task<string?> HandleLobbyJoinFromCallbackAsync(
        ITelegramBotClient botClient,
        User user,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var player = new LobbyPlayer(user.Id, GetDisplayName(user));
        var result = LobbyStore.Join(groupChatId, player);

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

    private static async Task<string?> HandleLobbyStartFromCallbackAsync(
        ITelegramBotClient botClient,
        User user,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var lobbySnapshot = LobbyStore.GetSnapshot(groupChatId);
        var result = GameSessionStore.Start(groupChatId, lobbySnapshot, user.Id);

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

    private static async Task<string?> HandleLobbyRefreshFromCallbackAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        await RefreshGroupCockpitAsync(botClient, groupChatId, cancellationToken);
        return null;
    }

    private static async Task HandleSkyAsync(ITelegramBotClient botClient, Message message,
        string[] args, CancellationToken cancellationToken)
    {
        var subcommand = args.FirstOrDefault()?.Trim().ToLowerInvariant();

        if (message.Chat.Type == ChatType.Private)
            switch (subcommand)
            {
                case "hand":
                    await HandleSkyHandAsync(botClient, message, cancellationToken);
                    return;

                case "place":
                    await HandleSkyPlaceAsync(botClient, message, args.Skip(1).ToArray(),
                        cancellationToken);
                    return;

                case "undo":
                    await HandleSkyUndoAsync(botClient, message, cancellationToken);
                    return;

                default:
                    await botClient.SendMessage(
                        message.Chat.Id,
                        "In a group chat, try: /sky new\n\nIn private chat: /sky hand | /sky place <dieIndex> <commandId> | /sky undo",
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

            case "hand":
            case "place":
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Use /sky hand and /sky place in a private chat with me.",
                    cancellationToken: cancellationToken);
                return;

            case "undo":
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Use /sky undo in a private chat with me.",
                    cancellationToken: cancellationToken);
                return;

            default:
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Usage: /sky new | /sky join | /sky state | /sky start | /sky roll",
                    cancellationToken: cancellationToken);
                return;
        }
    }

    private static async Task HandleSkyNewAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        LobbyStore.CreateNew(message.Chat.Id);
        await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
    }

    private static async Task HandleSkyJoinAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var displayName = GetDisplayName(message.From);
        var player = new LobbyPlayer(message.From.Id, displayName);

        var result = LobbyStore.Join(message.Chat.Id, player);
        if (result.Status == LobbyJoinStatus.NoLobby)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "No lobby yet. Create one with /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
    }

    private static async Task HandleSkyStateAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
    }

    private static async Task HandleSkyStartAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var lobbySnapshot = LobbyStore.GetSnapshot(message.Chat.Id);
        var result = GameSessionStore.Start(message.Chat.Id, lobbySnapshot, message.From.Id);

        var text = result.Status switch
        {
            GameSessionStartStatus.NoLobby => "No lobby yet. Create one with /sky new",
            GameSessionStartStatus.LobbyNotReady =>
                "Lobby is not ready yet. Two players must /sky join before starting.",
            GameSessionStartStatus.NotSeated =>
                "Only seated players (Pilot/Copilot) can start the game. Join with /sky join",
            GameSessionStartStatus.AlreadyStarted => "Game already started. Next: /sky roll",
            GameSessionStartStatus.Started =>
                "Game started. Round 1 initialized (no placements yet).\n\nNext: /sky roll",
            _ => "Cannot start game."
        };

        if (result.Status is GameSessionStartStatus.Started or GameSessionStartStatus.AlreadyStarted)
        {
            await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);
            return;
        }

        await botClient.SendMessage(message.Chat.Id, text, cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyRollAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        var snapshot = LobbyStore.GetSnapshot(message.Chat.Id);
        if (snapshot is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "No lobby yet. Create one with /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        if (!snapshot.IsReady)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Lobby is not ready yet. Two players must /sky join before rolling dice.",
                cancellationToken: cancellationToken);
            return;
        }

        if (GameSessionStore.GetSnapshot(message.Chat.Id) is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Game is not started yet. Run /sky start first.",
                cancellationToken: cancellationToken);
            return;
        }

        var roll = SecretDiceRoller.Roll(() => Random.Shared.Next(1, 7));
        var rollResult = GameSessionStore.RegisterRoll(message.Chat.Id, roll);

        if (rollResult.Status == GameSessionRollStatus.RoundNotAwaitingRoll)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "This round has already been rolled. Place dice with /sky place (in private chat).",
                cancellationToken: cancellationToken);
            return;
        }

        var startingPlayer = rollResult.StartingPlayer ?? PlayerSeat.Pilot;

        var failedRecipients = new List<string>(2);

        if (!await TrySendSecretDiceAsync(botClient, snapshot.Pilot!, "Pilot", roll.PilotDice,
                startingPlayer == PlayerSeat.Pilot, cancellationToken))
            failedRecipients.Add(snapshot.Pilot!.DisplayName);

        if (!await TrySendSecretDiceAsync(botClient, snapshot.Copilot!, "Copilot", roll.CopilotDice,
                startingPlayer == PlayerSeat.Copilot, cancellationToken))
            failedRecipients.Add(snapshot.Copilot!.DisplayName);

        await RefreshGroupCockpitAsync(botClient, message.Chat.Id, cancellationToken);

        if (failedRecipients.Count > 0)
            await botClient.SendMessage(
                message.Chat.Id,
                $"Dice rolled, but I couldn't DM: {string.Join(", ", failedRecipients)}. Each seated player must /start me in a private chat first.",
                cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyHandAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var result = GameSessionStore.GetHand(message.From.Id);

        var text = result.Status switch
        {
            GameHandStatus.NoActiveSession =>
                "No active game session found for you. Start a game in a group chat first.",
            GameHandStatus.NotSeated => "You are not seated as Pilot/Copilot in the active game.",
            GameHandStatus.RoundNotRolled =>
                "This round has not been rolled yet. In the group chat, run: /sky roll",
            GameHandStatus.Ok =>
                $"{result.Seat} hand:\n{RenderHand(result.Hand!)}\n\nCurrent turn: {result.CurrentPlayer}\nPlacements remaining: {result.PlacementsRemaining}\n\nAvailable commands:\n{RenderCommands(result.AvailableCommands!)}",
            _ => "Cannot show hand."
        };

        await botClient.SendMessage(
            message.Chat.Id,
            text,
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyPlaceAsync(
        ITelegramBotClient botClient,
        Message message,
        string[] args,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        if (args.Length < 2)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Usage: /sky place <dieIndex> <commandId>\nTip: /sky undo",
                cancellationToken: cancellationToken);
            return;
        }

        if (!int.TryParse(args[0], out var dieIndex))
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Invalid die index. Usage: /sky place <dieIndex> <commandId>",
                cancellationToken: cancellationToken);
            return;
        }

        var commandId = string.Join(" ", args.Skip(1));
        var result = GameSessionStore.PlaceDie(message.From.Id, dieIndex, commandId);

        if (result.Status != GamePlacementStatus.Placed)
        {
            var hand = GameSessionStore.GetHand(message.From.Id);
            var currentTurnText = hand.Status == GameHandStatus.Ok
                ? $" Current turn: {hand.CurrentPlayer}."
                : string.Empty;

            var errorText = result.Status switch
            {
                GamePlacementStatus.NoActiveSession =>
                    "No active game session found for you. Start a game in a group chat first.",
                GamePlacementStatus.NotSeated =>
                    "You are not seated as Pilot/Copilot in the active game.",
                GamePlacementStatus.RoundNotRolled =>
                    "This round has not been rolled yet. In the group chat, run: /sky roll",
                GamePlacementStatus.RoundNotAcceptingPlacements =>
                    "This round is not accepting placements.",
                GamePlacementStatus.NotPlayersTurn => "It is not your turn." + currentTurnText,
                GamePlacementStatus.InvalidDieIndex => "Invalid die index (expected 0-3).",
                GamePlacementStatus.DieAlreadyUsed => "That die has already been used.",
                GamePlacementStatus.InvalidTarget => "A command id is required.",
                GamePlacementStatus.CommandDoesNotMatchDie =>
                    "That command does not match the selected die.",
                GamePlacementStatus.CommandNotAvailable =>
                    "That command is not currently available.",
                GamePlacementStatus.DomainError => result.ErrorMessage ??
                                                   "Cannot place die (domain error).",

                _ => "Cannot place die."
            };

            await botClient.SendMessage(
                message.Chat.Id,
                errorText,
                cancellationToken: cancellationToken);
            return;
        }

        var info = result.PublicInfo!;

        await RefreshGroupCockpitAsync(botClient, info.GroupChatId, cancellationToken);

        var updatedHand = GameSessionStore.GetHand(message.From.Id);
        var dmText = updatedHand.Status == GameHandStatus.Ok
            ? $"Placement recorded: {info.CommandDisplayName} ({info.CommandId})\n\nYour hand:\n{RenderHand(updatedHand.Hand!)}\n\nCurrent turn: {updatedHand.CurrentPlayer}\n\nPlacements remaining: {updatedHand.PlacementsRemaining}\n\nAvailable commands:\n{RenderCommands(updatedHand.AvailableCommands!)}"
            : $"Placement recorded: {info.CommandDisplayName} ({info.CommandId})";

        await botClient.SendMessage(
            message.Chat.Id,
            dmText,
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyUndoAsync(ITelegramBotClient botClient, Message message,
        CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                message.Chat.Id,
                "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var result = GameSessionStore.UndoLastPlacement(message.From.Id);
        if (result.Status != GameUndoStatus.Undone)
        {
            var hand = GameSessionStore.GetHand(message.From.Id);
            var currentTurnText = hand.Status == GameHandStatus.Ok
                ? $" Current turn: {hand.CurrentPlayer}."
                : string.Empty;

            var errorText = result.Status switch
            {
                GameUndoStatus.NoActiveSession =>
                    "No active game session found for you. Start a game in a group chat first.",
                GameUndoStatus.NotSeated =>
                    "You are not seated as Pilot/Copilot in the active game.",
                GameUndoStatus.RoundNotRolled =>
                    "This round has not been rolled yet. In the group chat, run: /sky roll",
                GameUndoStatus.UndoNotAllowed =>
                    "Undo not allowed. You can only undo your last placement before the other player places." +
                    currentTurnText,
                GameUndoStatus.DomainError => result.ErrorMessage ?? "Cannot undo (domain error).",
                _ => "Cannot undo."
            };

            await botClient.SendMessage(
                message.Chat.Id,
                errorText,
                cancellationToken: cancellationToken);
            return;
        }

        var info = result.PublicInfo!;

        await RefreshGroupCockpitAsync(botClient, info.GroupChatId, cancellationToken);

        var updatedHand = GameSessionStore.GetHand(message.From.Id);
        var dmText = updatedHand.Status == GameHandStatus.Ok
            ? $"Undo recorded: {info.CommandDisplayName} ({info.CommandId})\n\nYour hand:\n{RenderHand(updatedHand.Hand!)}\n\nCurrent turn: {updatedHand.CurrentPlayer}\n\nPlacements remaining: {updatedHand.PlacementsRemaining}\n\nAvailable commands:\n{RenderCommands(updatedHand.AvailableCommands!)}"
            : $"Undo recorded: {info.CommandDisplayName} ({info.CommandId})";

        await botClient.SendMessage(
            message.Chat.Id,
            dmText,
            cancellationToken: cancellationToken);
    }

    private static string RenderHand(SecretDiceHand hand)
        => string.Join("\n",
            hand.Dice.Select(die =>
                $"{die.Index}:{die.Value.Value}{(die.IsUsed ? " (used)" : string.Empty)}"));

    private static string RenderCommands(IReadOnlyList<AvailableGameCommand> commands)
    {
        if (commands.Count == 0)
            return "(none)";

        return string.Join("\n", commands.Select(c => $"- {c.DisplayName} ({c.CommandId})"));
    }

    private static async Task RefreshGroupCockpitAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        CancellationToken cancellationToken)
    {
        var text = RenderGroupState(groupChatId);

        if (GameSessionStore.TryGetCockpitMessageId(groupChatId, out var cockpitMessageId))
            if (await TryEditCockpitAsync(botClient, groupChatId, cockpitMessageId, text, cancellationToken))
                return;

        var cockpitMessage = await botClient.SendMessage(
            groupChatId,
            text,
            replyMarkup: BuildGroupStateKeyboard(),
            cancellationToken: cancellationToken);

        GameSessionStore.SetCockpitMessageId(groupChatId, cockpitMessage.MessageId);
        await TryPinCockpitAsync(botClient, groupChatId, cockpitMessage.MessageId, cancellationToken);
    }

    private static async Task<bool> TryEditCockpitAsync(
        ITelegramBotClient botClient,
        long groupChatId,
        int cockpitMessageId,
        string text,
        CancellationToken cancellationToken)
    {
        try
        {
            await botClient.EditMessageText(
                groupChatId,
                cockpitMessageId,
                text,
                replyMarkup: BuildGroupStateKeyboard(),
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
            await botClient.PinChatMessage(
                groupChatId,
                cockpitMessageId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // best effort
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
        var turnText = isYourTurn
            ? "It's your turn to place first."
            : "Wait for the other player to place first.";

        var messageText =
            $"{seat} secret dice: {diceText}\n\n{turnText}\n\nCommands:\n/sky hand\n/sky place <dieIndex> <commandId>\n/sky undo";

        try
        {
            await botClient.SendMessage(
                recipient.UserId,
                messageText,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return false;
        }
    }

    private static string RenderLobby(LobbySnapshot snapshot)
        => GroupChatCockpitRenderer.RenderLobby(snapshot);

    private static string RenderGroupState(long groupChatId)
    {
        var inGame = GameSessionStore.GetPublicState(groupChatId);
        if (inGame is not null)
            return GroupChatCockpitRenderer.RenderInGame(inGame);

        var snapshot = LobbyStore.GetSnapshot(groupChatId);
        return snapshot is null
            ? "No lobby yet. Create one with /sky new"
            : RenderLobby(snapshot);
    }

    private static InlineKeyboardMarkup BuildGroupStateKeyboard()
        => new([
            [
                InlineKeyboardButton.WithCallbackData("New", NewCallbackData),
                InlineKeyboardButton.WithCallbackData("Join", JoinCallbackData),
                InlineKeyboardButton.WithCallbackData("Start", StartCallbackData)
            ],
            [InlineKeyboardButton.WithCallbackData("Refresh", RefreshCallbackData)]
        ]);

    private static long? GetLockKey(Update update)
    {
        if (update.Message is not null)
            return GetLockKey(update.Message);

        if (update.CallbackQuery is not null)
            return GetLockKey(update.CallbackQuery);

        return null;
    }

    private static long GetLockKey(Message message)
    {
        if (message.Chat.Type != ChatType.Private) return message.Chat.Id;
        if (message.From is null) return message.Chat.Id;

        return GameSessionStore.TryGetGroupChatIdForUserId(message.From.Id, out var groupChatId)
            ? groupChatId
            : message.Chat.Id;
    }

    private static long GetLockKey(CallbackQuery callbackQuery)
    {
        if (callbackQuery.Message is not null)
            return GetLockKey(callbackQuery.Message);

        return GameSessionStore.TryGetGroupChatIdForUserId(callbackQuery.From.Id, out var groupChatId)
            ? groupChatId
            : callbackQuery.From.Id;
    }

    private static bool IsDuplicateUpdate(int updateId)
    {
        lock (UpdateDedupSync)
        {
            if (!RecentUpdateIdSet.Add(updateId)) return true;

            RecentUpdateIds.Enqueue(updateId);

            while (RecentUpdateIds.Count > MaxRecentUpdateIds)
            {
                var oldest = RecentUpdateIds.Dequeue();
                RecentUpdateIdSet.Remove(oldest);
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

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        Console.Error.WriteLine(exception);
        return Task.CompletedTask;
    }
}
