# Decision Note: Issue #61 progress (WebApp lobby New/Join/Start)

**Date:** 2026-02-22
**By:** Skiles
**Requested by:** Gianluigi Conti

## Scope delivered
- Implemented Mini App lobby actions end-to-end with authenticated backend endpoints:
  - `POST /api/webapp/lobby/new`
  - `POST /api/webapp/lobby/join`
  - `POST /api/webapp/lobby/start`
- Kept `/sky new|join|start` fallback flows unchanged.
- Reused existing lobby/session stores (`InMemoryGroupLobbyStore`, `InMemoryGroupGameSessionStore`) and existing status contracts.
- Updated Mini App shell (`wwwroot/index.html`) to render lobby action buttons and call new endpoints.
- Wired successful WebApp lobby actions to refresh group cockpit through `TelegramBotService` (`RefreshGroupCockpitFromWebAppAsync`) when bot client is active.

## Validation
- `dotnet build .\SkyTeam.TelegramBot\SkyTeam.TelegramBot.csproj -c Release` ✅
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj -c Release --no-build --filter "Issue59WebAppGameStateEndpointTests|Issue60LaunchMiniAppButtonTests|Issue61WebAppLobbyEndpointsTests"` ✅ (7 passed)
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj -c Release --no-build` ✅ (72 passed)

## Test additions
- Added `Issue61WebAppLobbyEndpointsTests` covering:
  - New creates lobby
  - Join seats viewer
  - Start transitions to in-game when ready

## Notes for PR readiness
- Changes are minimal and isolated to WebApp/API wiring + focused tests.
- Branch used: `squad/61-webapp-lobby`.
