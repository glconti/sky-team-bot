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

### Mini App flow (for seated players)
Set `SKYTEAM_MINI_APP_URL` and use the Mini App for secret actions (hand/dice/place/undo).
Bot commands remain as fallback and will redirect you to the Mini App when secret info is required.

### Useful command
- `/sky state` in group chat to view current lobby/game status.
