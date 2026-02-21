# Sky Team Telegram Bot

## Prerequisites
- .NET SDK 10
- A Telegram bot token (from BotFather)

## Run the bot
1. Set the bot token:
   - PowerShell:
     ```powershell
     $env:TELEGRAM_BOT_TOKEN = "your-token-here"
     ```
2. Run the bot:
   ```powershell
   dotnet run --project .\SkyTeam.TelegramBot\
   ```

The bot uses long polling and keeps in-memory state while the process is running.

## How to play

### Group chat flow
1. Add the bot to a group.
2. Create and fill the lobby:
   - `/sky new`
   - `/sky join` (Pilot)
   - `/sky join` (Copilot)
3. Start and roll:
   - `/sky start`
   - `/sky roll`

### Private chat flow (for seated players)
Each seated player must open a private chat with the bot and run `/start` once (so the bot can DM dice), then use:
- `/sky hand` to view current dice and available commands
- `/sky place <dieIndex> <commandId>` to place a die
- `/sky undo` to undo the last placement (only before the opponent places)

### Useful command
- `/sky state` in group chat to view current lobby/game status.
