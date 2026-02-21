namespace SkyTeam.TelegramBot;

using SkyTeam.Application.Lobby;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

internal static class Program
{
    private static readonly InMemoryGroupLobbyStore LobbyStore = new();

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
        if (message.Chat.Type is not (ChatType.Group or ChatType.Supergroup))
        {
            await botClient.SendMessage(
                chatId: message.Chat.Id,
                text: "Lobby commands are group-chat-only. Add me to a group and run /sky new.",
                cancellationToken: cancellationToken);
            return;
        }

        var subcommand = args.FirstOrDefault()?.Trim().ToLowerInvariant();
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

            default:
                await botClient.SendMessage(
                    chatId: message.Chat.Id,
                    text: "Usage: /sky new | /sky join | /sky state",
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

    private static string RenderLobby(LobbySnapshot snapshot)
    {
        var pilot = snapshot.Pilot?.DisplayName ?? "(empty)";
        var copilot = snapshot.Copilot?.DisplayName ?? "(empty)";
        var ready = snapshot.IsReady ? "Yes" : "No";

        return $"Lobby state:\nPilot: {pilot}\nCopilot: {copilot}\nReady: {ready}";
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

    private static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine(exception);
        return Task.CompletedTask;
    }
}
