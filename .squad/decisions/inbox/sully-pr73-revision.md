# Sully PR #73 Revision Note (Issue #65)

## Scope completed
- Implemented AC #3 + AC #5 code-side enforcement in `SkyTeam.TelegramBot\TelegramBotService.cs`:
  - `/sky hand`, `/sky place`, and `/sky undo` now enforce Mini App-only secret path behavior.
  - Group and private invocations now redirect users to Mini App instead of exposing DM secret hand/place/undo flows.
  - Place(DM) callback path now returns Mini App-only guidance (no secret DM payload).
- Restored equivalent CockpitMessageId coverage in `SkyTeam.Application.Tests\GameSessions\InMemoryGroupGameSessionStoreTests.cs`:
  - `CockpitMessageId_ShouldBePersistedPerGroupSession`
  - `CockpitMessageId_ShouldKeepSingleLatestValue_WhenRecreated`
- Kept idempotency behavior untouched in application store placement flow.

## Related test updates
- Updated `SkyTeam.Application.Tests\Telegram\Issue53InGameCockpitButtonFlowTests.cs` expectation for Place(DM) callback to assert Mini App-only redirect behavior.

## Validation run
- `dotnet build .\SkyTeam.TelegramBot\SkyTeam.TelegramBot.csproj -nologo` ✅
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj -nologo --no-restore` ✅
  - Result: 110 total, 94 passed, 16 skipped, 0 failed.
  - Existing xUnit1051 warnings remain pre-existing/non-blocking.

## Reviewer lockout rule
- Skiles lockout respected for this revision cycle (no Skiles contribution used).
