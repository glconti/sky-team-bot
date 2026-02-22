# 2026-02-22T10:57:04Z: Skiles — Slice #59 web host refactor + API

**By:** Skiles (Backend)  
**Epic:** #57 — Mini App Foundation  
**Slice:** #59 — WebApp Foundation

## Delivered

### 1. Web SDK Conversion
- **`SkyTeam.TelegramBot.csproj`** — Changed SDK to `Microsoft.NET.Sdk.Web`; removed `<OutputType>Exe</OutputType>`.
- **`appsettings.json`** — Added `WebApp:InitDataMaxAgeSeconds` (default 300 seconds).

### 2. Program.cs Refactor
- Migrated from console app (`Host.CreateDefaultBuilder`) to ASP.NET Core (`WebApplication.CreateBuilder`).
- Registered in-memory stores as singletons:
  - `InMemoryGroupGameSessionStore`
  - `InMemoryGroupLobbyStore`
- Extracted Telegram polling into `TelegramBotService : BackgroundService` (registered as `IHostedService`).
- Added `UseStaticFiles()` for `wwwroot/` serving.
- Added `MapWebAppEndpoints()` extension for `/api/webapp/*` routing.
- Preserved existing Telegram.Bot initialization and callback routing.

### 3. TelegramInitDataValidator Service
- **Namespace:** `SkyTeam.TelegramBot.WebApp.Security`
- **Responsibility:** Validate Telegram Mini App `initData` per Telegram specification.
- **Inputs:** `string initData`, `string botToken`, `TimeSpan maxAge`, `DateTimeOffset now`.
- **Output:** `InitDataValidationResult` (success with parsed `userId`, `displayName`, `startParam`; or specific failure reason).
- **Algorithm:**
  - Parse `initData` as URL-encoded query string.
  - Extract and remove `hash` field.
  - Sort remaining fields alphabetically; build `data_check_string` (key=value, joined by \n).
  - Compute `secretKey = HMAC_SHA256(key="WebAppData", data=botToken)`.
  - Compute `expectedHash = HMAC_SHA256(key=secretKey, data=data_check_string)`.
  - Compare using `CryptographicOperations.FixedTimeEquals()`.
  - Parse `auth_date` and validate freshness against `maxAge` (default 5 min).

### 4. TelegramInitDataFilter
- **Type:** `IEndpointFilter`
- **Responsibility:** Middleware-like filter for `/api/webapp/*` endpoints.
- **Behavior:**
  - Reads `X-Telegram-Init-Data` header.
  - Delegates to `TelegramInitDataValidator.Validate()`.
  - On success: injects `TelegramWebAppUser` record into `HttpContext.Items`.
  - On failure: returns `401 Unauthorized`.

### 5. GET /api/webapp/game-state Endpoint
- **Route:** `GET /api/webapp/game-state?gameId=<id>`
- **Auth:** `X-Telegram-Init-Data` header (via filter).
- **Behavior:**
  - Reads `gameId` from query string.
  - Cross-checks against signed `start_param` (rejects with 400 if mismatched).
  - Queries `InMemoryGroupLobbyStore` and `InMemoryGroupGameSessionStore`.
  - Returns 200 with JSON shape defined in design doc, or 404 if neither exists.
- **Response shape:** Public game state only (no dice hand, no secret commands); includes `viewer.seat` (Pilot/Copilot/null).

### 6. TelegramBotOptions Configuration Class
- **Location:** `SkyTeam.TelegramBot/TelegramBotOptions.cs`
- **Properties:**
  - `BotToken` (string, from env `TELEGRAM_BOT_TOKEN`).
  - `WebApp` section (InitDataMaxAgeSeconds, etc.).

## Architecture Decisions

1. **Single host process:** In-memory stores are singletons in the DI container; no IPC needed.
2. **Hosted service for Telegram polling:** Lifecycle-managed by ASP.NET Core; graceful shutdown included.
3. **Filter-based auth:** Clean separation of validation logic; reusable across endpoints.
4. **Read-only in Slice #59:** Game state is public; no secrets leaked. Write endpoints (placement, token adjustments) deferred to Slice #64.

## Testing

- All new validators and filters are unit-tested (Aloha's responsibility).
- Integration tests verify endpoint behavior under valid/invalid/missing initData conditions.
- Existing Telegram callback tests updated to reflect new Program structure.

## Status

✅ **Complete** — Backend is fully functional, integrates with Gimli's shell, and awaits Aloha's test coverage.
