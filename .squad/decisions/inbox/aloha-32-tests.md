# Aloha — Issue #32 Test Boundary Decisions (2026-02-21)

## Decision: What “round resolution” means in application tests

Because the current application layer (`InMemoryGroupGameSessionStore` + `RoundTurnState`) does **not yet** integrate with the domain `Game` (no mapping from placement `Target` strings to domain commands/modules, and no cockpit state snapshot type), the Issue #32 tests treat *application-layer round resolution* as:

- After the **8th public placement**, the application session advances to the **next round**.
- Observable effects are limited to application state:
  - `GameRoundSnapshot.RoundNumber` increments (e.g., 1 → 2)
  - `GameRoundSnapshot.Status` returns to `AwaitingRoll`
  - `TurnState` is cleared, so `/sky hand` becomes `RoundNotRolled` until the next roll.

## Non-goals (deferred to implementation integration)

- Validating actual **domain module resolution** (Axis/Engines/etc.)
- Validating **broadcast formatting/content** beyond “state advanced”

These will require a domain snapshot/broadcast model and/or an application service that applies placements to the domain aggregate.
