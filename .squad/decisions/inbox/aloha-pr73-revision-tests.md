# Aloha — PR73 revision tests (Issue #65)

Requester: Gianluigi Conti

## What I changed
- Added AC#1 freshness UX-oriented validator coverage in `Issue59WebAppInitDataValidationTests`:
  - `TelegramInitDataValidator_ShouldExposeExpiredStatus_ForAuthDateFreshnessUx`
  - Asserts expired initData is rejected with explicit `Status = Expired` and preserves `AuthDate` for transport-layer UX mapping.
- Added AC#4 transport E2E-ish flow coverage in `Issue64WebAppPlacementFlowTests`:
  - `WebAppTransportFlow_ShouldCoverOpenLobbyStartRollPlaceUndo`
  - Drives endpoints end-to-end: open app (`/game-state`), lobby create/join/start, roll, place, undo.
  - Verifies post-place die is used and post-undo die is restored.
- Replaced removed CockpitMessageId coverage with equivalent/stronger assertions in `Issue51CockpitLifecycleTests`:
  - `CockpitMessageId_ShouldBePersistedPerGroupSession`
  - `CockpitMessageId_ShouldKeepSingleLatestValue_WhenRecreated`

## Test run
Command:
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj -v minimal --nologo`

Result:
- **PASS** — total 108, passed 92, failed 0, skipped 16
- Build warnings (xUnit1051) are pre-existing/non-blocking in this branch.
