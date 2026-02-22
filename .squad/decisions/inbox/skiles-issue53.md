# Skiles — Issue #53 (Slice 4/7) Implementation Note

Date: 2026-02-22  
Requested by: Gianluigi Conti  
Branch context: continued on `skiles/issues-50-51-publish` (PR #58 continuity)

## Delivered
- Added in-game group callback payloads in `SkyTeam.TelegramBot\Program.cs`:
  - `v1:grp:roll`
  - `v1:grp:place-dm`
  - existing `v1:grp:refresh` preserved
- Added callback handlers:
  - `HandleInGameRollFromCallbackAsync(...)` reusing shared roll flow.
  - `HandleInGamePlaceFromCallbackAsync(...)` sending private hand via DM only.
- Extracted shared roll path `HandleGroupRollAsync(...)` so callback and `/sky roll` remain consistent.
- Made cockpit keyboard state-aware:
  - Lobby: `New / Join / Start / Refresh`
  - In-game: `Roll / Place (DM) / Refresh`
- Added DM onboarding hint:
  - Callback toast when DM send fails: “Open a private chat with me and send /start, then press Place (DM) again.”
  - In-game cockpit text includes private-chat setup reminder.

## Safety / Guardrails
- No secret hand/command data is posted to group chat by Place (DM); secret payload stays DM-only.
- Callback errors still fail soft with `AnswerCallbackQuery` to avoid spinner hangs.
- `/sky roll` and `/sky hand` command fallbacks are preserved.

## Tests
- Updated Telegram tests for keyboard builder rename/signature changes.
- Added/updated Issue #53 test coverage in `SkyTeam.Application.Tests\Telegram\Issue53InGameCockpitButtonFlowTests.cs`.
- Executed: `dotnet test --nologo`
  - Result: **Passed** — total 206, passed 193, skipped 13, failed 0.

## PR #58 Summary Update
- PR #58 scope now extends from slices #50/#51/#52 to include slice #53:
  - in-game cockpit callbacks
  - DM onboarding hint
  - callback-safe, non-leaking group behavior
