# Aloha QA note — issue #61 lobby-flow tests

## Context reviewed
- `.squad/agents/aloha/history.md`
- `.squad/decisions.md`
- Issue #61 acceptance criteria (`glconti/sky-team-bot#61`)

## Acceptance criteria mapped
Issue #61 requires Mini App lobby UI/actions for **New / Join / Start**, backend API reuse of existing services, cockpit refresh/edit after successful actions, and `/sky new|join|start` fallback continuity.

## Tests added
Added `SkyTeam.Application.Tests/Telegram/Issue61WebAppLobbyFlowTests.cs`:
- Active deterministic contracts:
  - `SkyCommandFallback_ShouldKeepNewJoinStartRoutes_ForMiniAppLobbyParity`
  - `LobbyMutations_ShouldRefreshAndEditCockpit_WhenActionsSucceed`
- Drafted pending contracts (explicitly skipped until implementation lands):
  - `WebAppLobbyNew_ShouldCreateLobby_ViaBackendEndpoint`
  - `WebAppLobbyJoin_ShouldSeatViewer_ViaBackendEndpoint`
  - `WebAppLobbyStart_ShouldStartSession_ViaBackendEndpoint`
  - `WebAppLobbyActions_ShouldRefreshGroupCockpit_AfterSuccessfulMutations`

## Validation run
Executed:
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --nologo`

Result:
- **78 total, 61 passed, 0 failed, 17 skipped**

## Coverage gaps for Skiles
1. **Issue #61 backend API endpoints not implemented yet** (`POST` lobby new/join/start contracts are pending/skip).
2. **Issue #61 cockpit update path from Mini App actions is not yet testable end-to-end** (pending contract test).
3. Existing Telegram suites still contain legacy skips for #50/#51/#52 in current branch; these reduce confidence for callback/edit lifecycle regressions and should be reconciled with implemented behavior.
