# 2026-02-22T10:57:04Z: Aloha — Slice #59 tests

**By:** Aloha (Tester)  
**Epic:** #57 — Mini App Foundation  
**Slice:** #59 — WebApp Foundation

## Delivered

### 1. Unit Tests: TelegramInitDataValidator
- **File:** `SkyTeam.Application.Tests/Telegram/Issue59WebAppInitDataValidationTests.cs`
- **Coverage:**
  - Valid `initData` → Returns success with correct `userId`, `displayName`, `start_param`.
  - Tampered `hash` → Returns `InvalidHash`.
  - Expired `auth_date` → Returns `Expired` (auth_date outside maxAge window).
  - Missing `hash` field → Returns `InvalidHash`.
  - Empty/null `initData` → Returns failure.
  - Constant-time comparison: code review (FixedTimeEquals usage verified).
- **Test data:** Generated signed `initData` using deterministic test bot token and known payloads.

### 2. Integration Tests: GET /api/webapp/game-state Endpoint
- **File:** `SkyTeam.Application.Tests/Telegram/Issue59WebAppGameStateEndpointTests.cs`
- **Setup:** `WebApplicationFactory<SkyTeam.TelegramBot.Program>` with `ConfigureWebHost` override:
  - Disables `TelegramBotService` (no Telegram polling in tests).
  - Seeds test lobby/game sessions into stores.
- **Test scenarios:**
  - Valid `initData` + existing lobby → 200 with lobby phase and correct player state.
  - Valid `initData` + existing game session → 200 with cockpit phase and current state.
  - Valid `initData` + no game → 404 Not Found.
  - Missing `X-Telegram-Init-Data` header → 401 Unauthorized.
  - Invalid `initData` (bad hash, expired) → 401 Unauthorized.
  - Mismatched `gameId` vs signed `start_param` → 400 Bad Request.
  - Empty/malformed `gameId` → 400 Bad Request.
- **Deterministic signatures:** Tests compute valid HMAC signatures matching production algorithm.

### 3. Issue #53 Integration Tests (In-game Callbacks)
- **File:** `SkyTeam.Application.Tests/Telegram/Issue53InGameCockpitButtonFlowTests.cs`
- **Scope:** Validates in-game cockpit callback routing (Roll, Place (DM), Refresh) and group privacy.
- **Test scenarios:**
  - Roll callback → edits cockpit message, rolls dice, renders updated hand (DM-only, not group).
  - Place (DM) callback → sends placement DM with onboarding hint if user has no private chat.
  - Refresh callback → re-renders cockpit without state change.
  - Group privacy contract: no secret hand/placement data leaks to group chat.
  - Fallback continuity: `/sky roll` and `/sky hand` still work.
- **Result:** All tests pass; Issue #53 acceptance criteria locked into CI.

## Testing Strategy

1. **Unit tests** — Pure validator logic, no I/O, deterministic and fast.
2. **Integration tests** — Full ASP.NET Core host, in-memory stores, realistic request/response flow.
3. **Deterministic initData generation** — Test utilities produce valid signatures using hardcoded test token.
4. **Disabled polling** — Tests override `Program` to exclude `TelegramBotService`, avoiding network calls.

## Status

✅ **Complete** — All Slice #59 and Issue #53 tests are implemented, passing, and commit-ready.
- **Test run:** `dotnet test --nologo` → 206 total, 193 passed, 13 skipped, 0 failed.
