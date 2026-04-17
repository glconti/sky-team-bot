# Solo Mode Test Coverage — Issue #88

**Author:** Aloha (Tester)  
**Date:** 2025-01-26  
**Status:** Tests written, pending Skiles implementation

## Summary

Comprehensive test suite created for Solo Mode feature (Issue #88) before implementation complete. Tests encode expected behavior and will serve as validation once Skiles ships.

## Test Coverage

### 1. Lobby Store Tests (Direct)
- `SoloModeLobbyCreation_ShouldAutoSeatSinglePlayerInBothRoles` — verifies `CreateSoloLobby()` seats player in Pilot and Copilot
- `SoloModeLobbyCreation_ShouldReturnAlreadyExists_WhenLobbyAlreadyExistsForChat` — validates idempotency

### 2. WebApp State Tests (HTTP)
- `SoloModeWebAppState_ShouldExposeIsSoloModeFlag_WhenPilotAndCopilotShareSameUserId` — checks `IsSoloMode = true`
- `SoloModeWebAppState_ShouldNotExposeIsSoloModeFlag_WhenDifferentUsersOccupySeats` — checks `IsSoloMode = false`

### 3. HTTP Endpoint Tests
- `SoloModeEndpoint_ShouldCreateSoloLobbyAndAutoSeatViewer` — validates `POST /api/webapp/lobby/new-solo`
- `SoloModeEndpoint_ShouldReturnBadRequest_WhenLobbyAlreadyExists` — edge case handling

### 4. UI Tests (File Content)
- `SoloModeUI_ShouldContainSoloModeButton_InLobbySection` — checks for "Solo Mode" button text
- `SoloModeUI_ShouldContainSoloModeBadgeOrIndicator` — checks for badge/indicator UI
- `SoloModeUI_ShouldContainSoloModeWarning_ForTestingOnly` — validates testing warning
- `SoloModeUI_ShouldHandleIsSoloModeFlag_InStateRendering` — checks JavaScript handles `isSoloMode`

### 5. Domain Model Tests (Reflection-Based)
- `GameMode_ShouldHaveExpectedValues` — theory test for `TwoPlayer` and `Solo` enum values
- `GameMode_ShouldDefaultToTwoPlayer_WhenNotSpecified` — checks `Game.Mode` property exists

## Build Status

**Current:** 6 compilation errors (expected, features not implemented)
- Missing: `InMemoryGroupLobbyStore.CreateSoloLobby()`
- Missing: `WebAppLobbyState.IsSoloMode`
- Missing: `GameMode` enum in `SkyTeam.Domain`

**Expected:** All tests pass once Skiles completes implementation

## Notes for Skiles

When implementing Solo Mode:
1. Add `CreateSoloLobby(groupChatId, player)` to `InMemoryGroupLobbyStore` — should seat player in both roles
2. Add `IsSoloMode` property to `WebAppLobbyState` — computed from `Pilot.UserId == Copilot.UserId`
3. Create `POST /api/webapp/lobby/new-solo` endpoint
4. Add Solo Mode button, badge, and warning in `index.html`
5. Define `GameMode` enum in `SkyTeam.Domain` with `TwoPlayer` and `Solo`
6. Add `Game.Mode` property (default `TwoPlayer`)

## Decision Required?

**No.** This is test implementation aligned with existing patterns. No architectural decisions needed. Tests follow AAA + FluentAssertions conventions established in Issue #61 and #62 test suites.

Once Skiles ships, run:
```powershell
dotnet test D:\Repos\skyteam-bot\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --filter "Issue88SoloModeTests" --nologo
```
