# Issue #78 Mini App Lobby UI — Implementation Notes

**Date:** 2026-03-03  
**Author:** Skiles (Domain Developer)  
**Issue:** https://github.com/glconti/sky-team-bot/issues/78  
**Requested by:** Gianluigi Conti

## Context

Issue #78 required implementing a Mini App lobby UI with specific elements verified by tests in `Issue78WebAppLobbyUiTests.cs`. The task was to ensure all test assertions pass.

## Findings

Upon inspection, the lobby UI was **already fully implemented** in `SkyTeam.TelegramBot\wwwroot\index.html` as part of the earlier WebApp foundation work. All required elements were present and all tests passing.

## Implementation Details (Already Present)

### UI Elements (Lines 238-244 in index.html)
- Lobby section with seat display grid
- Action button row
- Status line with badges
- Collapsible create/join forms

### JavaScript Implementation (Lines 482-615)

**Seat Rendering:**
- `createSeatCard()` function (lines 815-842) handles seat display
- Shows pilot/copilot avatars with initials
- Displays "Waiting for Pilot…" or "Waiting for Copilot…" when seats are empty
- Applies `truncateDisplayName()` to seat names

**Action Buttons:**
- "New Lobby" button (line 501) — toggles create form
- "Join Lobby" button (line 563) — toggles join form  
- "Start Game" button (line 606) — disabled until both seats filled

**Create Lobby Form (Lines 511-559):**
- Game name input (required, max 64 chars)
- Player count input (number, default 2)
- Lobby settings input (optional, max 120 chars)
- Client-side validation with exact error messages

**Join Lobby Form (Lines 572-602):**
- Game code input (numeric)
- Validation for numeric-only codes

**Display Name Truncation (Lines 291, 327-331):**
- `maxDisplayNameLength = 32` constant
- `truncateDisplayName()` function clips at 32 chars with ellipsis

**Validation Messages (Lines 335-354):**
- "Game name is required." (line 335)
- "Player count must be ${requiredLobbyPlayerCount} (Pilot + Copilot)." (line 340)
- "Enter a numeric game code." (line 354)

## Test Results

All Issue78WebAppLobbyUiTests passing:
1. ✅ `LobbyView_ShouldExposeSeatPlaceholdersAndActions_ForMiniAppLobbyUi`
2. ✅ `LobbyView_ShouldTruncateDisplayNames_ToTelegramLimit`
3. ✅ `LobbyView_ShouldExposeValidationMessages_ForInvalidCreateAndJoinInput`

## API Integration (Already Wired)

- `GET /api/webapp/game-state` returns lobby state
- `POST /api/webapp/lobby/new` creates lobby
- `POST /api/webapp/lobby/join` joins lobby
- `POST /api/webapp/lobby/start` starts game
- All endpoints protected by `TelegramInitDataFilter` and `WebAppAbuseProtectionFilter`

## Conclusion

No implementation work was required. The lobby UI was already complete and met all acceptance criteria. This validates that earlier WebApp foundation work (Sessions 14-16) properly covered the lobby phase UI requirements.

## Related Files

- `SkyTeam.TelegramBot\wwwroot\index.html` — Lobby UI implementation
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs` — Backend API
- `SkyTeam.Application.Tests\Telegram\Issue78WebAppLobbyUiTests.cs` — Test specifications
- `SkyTeam.Application\Lobby\InMemoryGroupLobbyStore.cs` — Lobby state management

## Next Steps

Issue #78 can be marked as complete. All automated tests pass and the UI implementation is production-ready pending manual Telegram client QA (which is tracked separately for Epic #75).
