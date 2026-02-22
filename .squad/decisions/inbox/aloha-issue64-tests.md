# Aloha QA Note — Issue #64 Proactive Tests

Requested by: Gianluigi Conti  
Date: 2026-02-23

## Scope covered
- Added proactive WebApp/game-action test file: `SkyTeam.Application.Tests\Telegram\Issue64WebAppPlacementUndoTests.cs`.
- Captured acceptance-criteria contracts for:
  - placement endpoint exposure (`/api/webapp/game/place`)
  - undo endpoint exposure (`/api/webapp/game/undo`)
  - cockpit refresh bridge after successful placement/undo
  - token-adjusted command selection path for placement
  - no secret-option leakage (no DM/group secret payload path)

## Execution result
- Command run:
  - `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --filter "FullyQualifiedName~Issue63WebAppInGameActionsTests|FullyQualifiedName~Issue64WebAppPlacementUndoTests"`
- Result:
  - **Total: 10**
  - **Passed: 5** (Issue #63)
  - **Failed: 0**
  - **Skipped: 5** (Issue #64 proactive contracts)

## Gaps / blockers
- Issue #64 WebApp handlers/routes are not implemented yet in `WebAppEndpoints`, so Issue #64 tests are intentionally `Skip`-guarded.
- Once `/game/place` and `/game/undo` handlers land, unskip and wire assertions against live endpoint behavior (status mapping, refresh-on-success only, viewer-scoped response privacy).
