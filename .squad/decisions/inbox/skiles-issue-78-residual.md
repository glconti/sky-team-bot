# 2026-03-02T09:35:00Z — Issue #78 Residual Lobby Form + Join Code (Skiles)

**Issue:** https://github.com/glconti/sky-team-bot/issues/78  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Requested by:** Gianluigi Conti

## Context
- Residual checklist for #78 required:
  1. Create flow opens a form and submits structured input.
  2. Join flow accepts explicit game code/ID.
  3. Chat-context-safe behavior and clear invalid input/code messaging.

## Decision
- Keep chat context enforcement at the existing WebApp boundary (`ResolveRequestContext`) and layer join-by-code on top with signed-context validation.
- Add a lightweight inline lobby form in `wwwroot/index.html` (game name, player count, optional settings) and a join-by-code form with client-side validation.
- Validate create payload and gameId/context mismatches in `WebAppEndpoints` with explicit `error` + `retryHint` responses.

## Delivered Artifacts
- `SkyTeam.TelegramBot/wwwroot/index.html`
  - New Lobby now opens/close form before POST.
  - Join Lobby now opens join-by-code form (`gameCode`).
  - Added validation/error messaging for invalid create fields and non-numeric codes.
- `SkyTeam.TelegramBot/WebApp/WebAppEndpoints.cs`
  - Added optional create/join request contracts (`WebAppCreateLobbyRequest`, `WebAppJoinLobbyRequest`).
  - Added create payload validation and clearer request-context error hints.
  - Join endpoint supports explicit `gameCode` while preserving signed chat/start_param safety.
- `SkyTeam.Application.Tests/Telegram/Issue78WebAppLobbyUiTests.cs`
  - Added assertions for create/join form inputs and validation message coverage.
- `SkyTeam.Application.Tests/Telegram/Issue61WebAppLobbyEndpointsTests.cs`
  - Added integration coverage for invalid create payload, invalid join code, and signed-context mismatch.

## Validation
- `dotnet test SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --filter "FullyQualifiedName~Issue61WebAppLobbyEndpointsTests|FullyQualifiedName~Issue78WebAppLobbyUiTests" --nologo`
  - **Passed:** 13, **Failed:** 0, **Skipped:** 0.
- `dotnet build skyteam-bot.slnx --nologo`
  - **Succeeded**.
- `dotnet test skyteam-bot.slnx --nologo`
  - **Passed:** 294, **Failed:** 0, **Skipped:** 16 (pre-existing).

## Done Scope
- Residual #78 create/join input flows implemented in Mini App UI.
- Explicit validation/error messaging added for invalid code/input.
- Chat-context-safe join behavior preserved and regression-tested.

## Remaining Scope
- Manual cross-platform lobby QA evidence (iOS/Android/Desktop) still requires execution/logging to close the final checklist item.
