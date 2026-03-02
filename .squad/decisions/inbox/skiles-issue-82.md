# Skiles — Issue #82 first implementation slice (2026-03-02T02:10:00Z)

## Context
Issue #82 requires optimistic concurrency on game mutations and an explicit safe conflict path for stale mutation attempts.

## Decision
For slice 1, add expected-version compare-and-swap checks directly in `InMemoryGroupGameSessionStore` mutation methods (`RegisterRoll`, `PlaceDie`, `UndoLastPlacement`) and map stale writes to explicit `409 Conflict` WebApp responses with `ConcurrencyConflict` payload including `CurrentVersion`.

## Done in this slice
- Added mutation-level version conflict statuses and result metadata:
  - `GameSessionRollStatus.VersionConflict`
  - `GamePlacementStatus.VersionConflict`
  - `GameUndoStatus.VersionConflict`
  - `CurrentVersion` on mutation result records
- Added expected-version overloads:
  - `RegisterRoll(..., expectedVersion, ...)`
  - `PlaceDie(..., expectedVersion)`
  - `UndoLastPlacement(..., expectedVersion)`
- Enforced compare-and-swap guard before mutation side effects.
- Exposed session `Version` in `GameSessionSnapshot` and WebApp game-state response (`Version`).
- Added explicit WebApp conflict payload:
  - `WebAppConcurrencyConflictResponse { Error = "ConcurrencyConflict", CurrentVersion, Message }`
- Added tests:
  - `Update_ShouldReturnVersionConflict_WhenExpectedVersionIsOutdated` (unskipped, now active)
  - `PlaceEndpoint_ShouldReturnConcurrencyConflict_WhenExpectedVersionIsOutdated`

## Remaining scope
- Propagate expected-version enforcement to any non-WebApp mutation surfaces that may be introduced.
- Align Mini App client retry UX to always send `expectedVersion` for mutating actions.
- Expand concurrency tests for parallel stale writes across roll/place/undo paths.
