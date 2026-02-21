namespace SkyTeam.TelegramBot;

using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.Application.Presentation;
using SkyTeam.Application.Round;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

internal static class Program
{
    private static readonly InMemoryGroupLobbyStore LobbyStore = new();
    private static readonly InMemoryGroupGameSessionStore GameSessionStore = new();

    public static async Task Main()
    {
        var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            Console.Error.WriteLine("Missing TELEGRAM_BOT_TOKEN environment variable.");
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
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
            cancellationToken: cts.Token);

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

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } text } message) return;

        var parts = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        if (IsCommand(parts[0], "/start"))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Sky Team bot is online. In a group chat, try: /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        if (IsCommand(parts[0], "/sky"))
        {
            await HandleSkyAsync(botClient, message, parts.Skip(1).ToArray(), cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: "Unknown command. Try /sky new",
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyAsync(ITelegramBotClient botClient, Message message, string[] args, CancellationToken cancellationToken)
    {
        var subcommand = args.FirstOrDefault()?.Trim().ToLowerInvariant();

        if (message.Chat.Type == ChatType.Private)
        {
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
                        chatId: message.Chat.Id,
                        text: "In a group chat, try: /sky new\n\nIn private chat: /sky hand | /sky place <dieIndex> <commandId> | /sky undo",

                        cancellationToken: cancellationToken);
                    return;
            }
        }

        if (message.Chat.Type is not (ChatType.Group or ChatType.Supergroup))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Lobby commands are group-chat-only. Add me to a group and run /sky new.",
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
                    chatId: message.Chat.Id,
                    text: "Use /sky hand and /sky place in a private chat with me.",
                    cancellationToken: cancellationToken);
                return;

            case "undo":
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Use /sky undo in a private chat with me.",
                    cancellationToken: cancellationToken);
                return;

            default:
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Usage: /sky new | /sky join | /sky state | /sky start | /sky roll",
                    cancellationToken: cancellationToken);
                return;
        }
    }

    private static async Task HandleSkyNewAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var result = LobbyStore.CreateNew(message.Chat.Id);

        var header = result.Status switch
        {
            LobbyCreateStatus.Created => "Sky Team lobby created for this group.",
            LobbyCreateStatus.AlreadyExists => "A Sky Team lobby already exists for this group.",
            _ => "Sky Team lobby."
        };

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"{header}\n\n{RenderLobby(result.Snapshot)}\n\nJoin with: /sky join",
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyJoinAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var displayName = GetDisplayName(message.From);
        var player = new LobbyPlayer(message.From.Id, displayName);

        var result = LobbyStore.Join(message.Chat.Id, player);
        if (result.Status == LobbyJoinStatus.NoLobby)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "No lobby yet. Create one with /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        var header = result.Status switch
        {
            LobbyJoinStatus.JoinedAsPilot => "Seated as Pilot.",
            LobbyJoinStatus.JoinedAsCopilot => "Seated as Copilot.",
            LobbyJoinStatus.AlreadySeated => "You're already seated.",
            LobbyJoinStatus.Full => "Lobby is full (2/2). You're a spectator.",
            _ => "Lobby updated."
        };

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: $"{header}\n\n{RenderLobby(result.Snapshot!)}",
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyStateAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var inGame = GameSessionStore.GetPublicState(message.Chat.Id);
        if (inGame is not null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: GroupChatCockpitRenderer.RenderInGame(inGame),
                cancellationToken: cancellationToken);
            return;
        }

        var snapshot = LobbyStore.GetSnapshot(message.Chat.Id);
        if (snapshot is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "No lobby yet. Create one with /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: RenderLobby(snapshot),
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyStartAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var lobbySnapshot = LobbyStore.GetSnapshot(message.Chat.Id);
        var result = GameSessionStore.Start(message.Chat.Id, lobbySnapshot, message.From.Id);

        var text = result.Status switch
        {
            GameSessionStartStatus.NoLobby => "No lobby yet. Create one with /sky new",
            GameSessionStartStatus.LobbyNotReady => "Lobby is not ready yet. Two players must /sky join before starting.",
            GameSessionStartStatus.NotSeated => "Only seated players (Pilot/Copilot) can start the game. Join with /sky join",
            GameSessionStartStatus.AlreadyStarted => "Game already started. Next: /sky roll",
            GameSessionStartStatus.Started => "Game started. Round 1 initialized (no placements yet).\n\nNext: /sky roll",
            _ => "Cannot start game."
        };

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyRollAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var snapshot = LobbyStore.GetSnapshot(message.Chat.Id);
        if (snapshot is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "No lobby yet. Create one with /sky new",
                cancellationToken: cancellationToken);
            return;
        }

        if (!snapshot.IsReady)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Lobby is not ready yet. Two players must /sky join before rolling dice.",
                cancellationToken: cancellationToken);
            return;
        }

        if (GameSessionStore.GetSnapshot(message.Chat.Id) is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Game is not started yet. Run /sky start first.",
                cancellationToken: cancellationToken);
            return;
        }

        var roll = SecretDiceRoller.Roll(() => Random.Shared.Next(1, 7));
        var rollResult = GameSessionStore.RegisterRoll(message.Chat.Id, roll, startingPlayer: PlayerSeat.Pilot);

        if (rollResult.Status == GameSessionRollStatus.RoundNotAwaitingRoll)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "This round has already been rolled. Place dice with /sky place (in private chat).",
                cancellationToken: cancellationToken);
            return;
        }

        var startingPlayer = rollResult.StartingPlayer ?? PlayerSeat.Pilot;

        var failedRecipients = new List<string>(capacity: 2);

        if (!await TrySendSecretDiceAsync(botClient, snapshot.Pilot!, "Pilot", roll.PilotDice, isYourTurn: startingPlayer == PlayerSeat.Pilot, cancellationToken))
            failedRecipients.Add(snapshot.Pilot!.DisplayName);

        if (!await TrySendSecretDiceAsync(botClient, snapshot.Copilot!, "Copilot", roll.CopilotDice, isYourTurn: startingPlayer == PlayerSeat.Copilot, cancellationToken))
            failedRecipients.Add(snapshot.Copilot!.DisplayName);

        var groupText = failedRecipients.Count == 0
            ? $"Dice rolled and sent privately to seated players. {startingPlayer} places first (use /sky place in private chat)."
            : $"Dice rolled, but I couldn't DM: {string.Join(", ", failedRecipients)}. Each seated player must /start me in a private chat first.";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: groupText,
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyHandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        var result = GameSessionStore.GetHand(message.From.Id);

        var text = result.Status switch
        {
            GameHandStatus.NoActiveSession => "No active game session found for you. Start a game in a group chat first.",
            GameHandStatus.NotSeated => "You are not seated as Pilot/Copilot in the active game.",
            GameHandStatus.RoundNotRolled => "This round has not been rolled yet. In the group chat, run: /sky roll",
            GameHandStatus.Ok => $"{result.Seat} hand:\n{RenderHand(result.Hand!)}\n\nCurrent turn: {result.CurrentPlayer}\nPlacements remaining: {result.PlacementsRemaining}\n\nAvailable commands:\n{RenderCommands(result.AvailableCommands!)}",
            _ => "Cannot show hand."
        };

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: text,
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
                chatId: message.Chat.Id,
                text: "Cannot identify you in this chat.",
                cancellationToken: cancellationToken);
            return;
        }

        if (args.Length < 2)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Usage: /sky place <dieIndex> <commandId>\nTip: /sky undo",
                cancellationToken: cancellationToken);
            return;
        }

        if (!int.TryParse(args[0], out var dieIndex))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Invalid die index. Usage: /sky place <dieIndex> <commandId>",
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
                GamePlacementStatus.NoActiveSession => "No active game session found for you. Start a game in a group chat first.",
                GamePlacementStatus.NotSeated => "You are not seated as Pilot/Copilot in the active game.",
                GamePlacementStatus.RoundNotRolled => "This round has not been rolled yet. In the group chat, run: /sky roll",
                GamePlacementStatus.RoundNotAcceptingPlacements => "This round is not accepting placements.",
                GamePlacementStatus.NotPlayersTurn => "It is not your turn." + currentTurnText,
                GamePlacementStatus.InvalidDieIndex => "Invalid die index (expected 0-3).",
                GamePlacementStatus.DieAlreadyUsed => "That die has already been used.",
                GamePlacementStatus.InvalidTarget => "A command id is required.",
                GamePlacementStatus.CommandDoesNotMatchDie => "That command does not match the selected die.",
                GamePlacementStatus.CommandNotAvailable => "That command is not currently available.",
                GamePlacementStatus.DomainError => result.ErrorMessage ?? "Cannot place die (domain error).",

                _ => "Cannot place die."
            };

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: errorText,
                cancellationToken: cancellationToken);
            return;
        }

        var info = result.PublicInfo!;

        var groupText = $"{info.Player.DisplayName} ({info.Seat}) placed {info.Value.Value}: {info.CommandDisplayName}.";
        groupText += info.PlacementsRemaining == 0
            ? "\n\nAll placements done. Resolving the round..."
            : $"\nNext: {info.NextPlayer}. Remaining placements: {info.PlacementsRemaining}.";

        await botClient.SendMessage(
            chatId: info.GroupChatId,
            text: groupText,
            cancellationToken: cancellationToken);

        var updatedHand = GameSessionStore.GetHand(message.From.Id);
        var dmText = updatedHand.Status == GameHandStatus.Ok
            ? $"Placement recorded: {info.CommandDisplayName} ({info.CommandId})\n\nYour hand:\n{RenderHand(updatedHand.Hand!)}\n\nCurrent turn: {updatedHand.CurrentPlayer}\n\nPlacements remaining: {updatedHand.PlacementsRemaining}\n\nAvailable commands:\n{RenderCommands(updatedHand.AvailableCommands!)}"
            : $"Placement recorded: {info.CommandDisplayName} ({info.CommandId})";

        if (result.ResolutionInfo is not null)
        {
            await botClient.SendMessage(
                chatId: info.GroupChatId,
                text: RenderResolution(result.ResolutionInfo),
                cancellationToken: cancellationToken);
        }

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: dmText,
            cancellationToken: cancellationToken);
    }

    private static async Task HandleSkyUndoAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.From is null)
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Cannot identify you in this chat.",
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
                GameUndoStatus.NoActiveSession => "No active game session found for you. Start a game in a group chat first.",
                GameUndoStatus.NotSeated => "You are not seated as Pilot/Copilot in the active game.",
                GameUndoStatus.RoundNotRolled => "This round has not been rolled yet. In the group chat, run: /sky roll",
                GameUndoStatus.UndoNotAllowed => "Undo not allowed. You can only undo your last placement before the other player places." + currentTurnText,
                GameUndoStatus.DomainError => result.ErrorMessage ?? "Cannot undo (domain error).",
                _ => "Cannot undo."
            };

            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: errorText,
                cancellationToken: cancellationToken);
            return;
        }

        var info = result.PublicInfo!;

        var groupText = $"{info.Player.DisplayName} ({info.Seat}) undid placement {info.Value.Value}: {info.CommandDisplayName}.\nNext: {info.NextPlayer}. Remaining placements: {info.PlacementsRemaining}.";

        await botClient.SendMessage(
            chatId: info.GroupChatId,
            text: groupText,
            cancellationToken: cancellationToken);

        var updatedHand = GameSessionStore.GetHand(message.From.Id);
        var dmText = updatedHand.Status == GameHandStatus.Ok
            ? $"Undo recorded: {info.CommandDisplayName} ({info.CommandId})\n\nYour hand:\n{RenderHand(updatedHand.Hand!)}\n\nCurrent turn: {updatedHand.CurrentPlayer}\n\nPlacements remaining: {updatedHand.PlacementsRemaining}\n\nAvailable commands:\n{RenderCommands(updatedHand.AvailableCommands!)}"
            : $"Undo recorded: {info.CommandDisplayName} ({info.CommandId})";

        await botClient.SendMessage(
            chatId: message.Chat.Id,
            text: dmText,
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

    private static string RenderResolution(GameRoundResolutionPublicInfo info)
        => GroupChatCockpitRenderer.RenderRoundResolution(info);

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

        var messageText = $"{seat} secret dice: {diceText}\n\n{turnText}\n\nCommands:\n/sky hand\n/sky place <dieIndex> <commandId>\n/sky undo";

        try
        {
            await botClient.SendMessage(
                chatId: recipient.UserId,
                text: messageText,
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

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine(exception);
        return Task.CompletedTask;
    }
}
