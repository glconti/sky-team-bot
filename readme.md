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
Set a public HTTPS URL for the Mini App shell (served by this host):
- `SKYTEAM_MINI_APP_URL` (or `WebApp:MiniAppUrl`)

Bot commands remain as fallback and will redirect you to the Mini App when secret info is required.

### BotFather Main Mini App setup (Issue #76)
1. Deploy the Mini App host on a public HTTPS domain (valid CA cert, no self-signed certs).
2. In **BotFather** run `/mybots` → select your bot → **Bot Settings** → **Main Mini App** (or **Configure Mini App**).
3. Set the Mini App URL to the same value configured in `SKYTEAM_MINI_APP_URL` (for example `https://skyteam.example/`).
4. Ensure the BotFather short name is at most 32 characters.
5. Optional secondary launch surface: run `/setmenubutton` and set the same Mini App URL.

### startapp link syntax
- Primary deep link (group/game-aware):
  - `https://t.me/<bot_username>?startapp=<groupChatId>`
- Optional app-short-name variant (fallback if needed on some clients):
  - `https://t.me/<bot_username>/<app_short_name>?startapp=<groupChatId>`

### Operator verification checklist
- `SKYTEAM_MINI_APP_URL` resolves directly over HTTPS (no redirect loops, no TLS warnings).
- Tapping **Open app** or a `startapp` link opens the Mini App on Telegram iOS, Android, and Desktop.
- `/setmenubutton` can be toggled on/off and keeps pointing to the same HTTPS Mini App URL.
- If an older client falls back to chat-first behavior, use the app-short-name deep link variant above.

### Useful command
- `/sky state` in group chat to view current lobby/game status.
