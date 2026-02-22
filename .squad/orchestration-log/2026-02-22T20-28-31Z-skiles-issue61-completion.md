# 2026-02-22T20:28:31Z: Skiles — Issue #61 WebApp Lobby completion

**By:** Skiles (Backend)  
**Epic:** #57 — Mini App Foundation  
**Issue:** #61 — WebApp Lobby New/Join/Start

## Delivered

### 1. Backend API Endpoints
- **`POST /api/webapp/lobby/new`** — Creates new group lobby via Mini App button; returns `groupId` and redirect hint.
- **`POST /api/webapp/lobby/join`** — Seats authenticated viewer in existing lobby; validates group membership; returns current roster.
- **`POST /api/webapp/lobby/start`** — Transitions lobby to in-game session; reuses existing state stores and status contracts.
- **Authentication:** All three endpoints validate `X-Telegram-Init-Data` header via `TelegramInitDataFilter`.
- **Reuse:** Leveraged existing `InMemoryGroupLobbyStore` and `InMemoryGroupGameSessionStore` (no new persistent layer).

### 2. Mini App Shell Integration
- **`wwwroot/index.html`** — Updated to render lobby action buttons (`New`, `Join`, `Start`).
- **Client-side routing:** JavaScript captures button clicks; sends `X-Telegram-Init-Data` header in POST body.
- **Response handling:** On success, refreshes shell state; on error, shows toast.

### 3. Cockpit Synchronization
- **Wired `RefreshGroupCockpitFromWebAppAsync`** in `TelegramBotService` to trigger group message update when WebApp actions succeed.
- **Preserves single-cockpit-message model:** Successful Mini App mutations delegate to existing `RefreshGroupCockpitAsync` lifecycle.

### 4. Test Coverage
- **File:** `SkyTeam.Application.Tests/Telegram/Issue61WebAppLobbyEndpointsTests.cs`
- **Scenarios:**
  - `New` creates lobby with unique `groupId`.
  - `Join` seats authenticated viewer; enforces group membership.
  - `Start` transitions to in-game when ready; preserves player roles.
- **Result:** 7 tests pass; endpoints integration-tested against real request/response contract.

### 5. Fallback Continuity
- **`/sky new|join|start` command handlers** remain unchanged and continue to refresh cockpit state.
- No regressions in text-command flow; Mini App and CLI paths converge at same state mutation points.

## Validation
- `dotnet build .\SkyTeam.TelegramBot\SkyTeam.TelegramBot.csproj -c Release` ✅
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj -c Release --no-build` ✅ (72 passed)
- Issue #61 acceptance criteria locked into CI.

## Status
✅ **Complete** — Backend endpoints fully functional, Mini App shell wired, fallback parity confirmed.
