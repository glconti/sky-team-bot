# PR #15 — Game init refactor follow-up (Skiles)

**Context:** Sully review flagged architectural issues in PR #15: duplicated state between `Game` and `GameState`, mutable `NextRoundCommand.Instance`, and a default-game factory (`Game.New()`) inside the aggregate.

## Decision
- **Single source of truth for per-round state:** `Game` now owns a single `GameState` instance and delegates current-player + unused-dice tracking to it (no duplicated fields in `Game`).
- **Immutable singleton command:** `NextRoundCommand.Instance` is now get-only and backed by a private constructor.
- **Factory removed from aggregate:** `Game.New()` was removed; aggregate construction is now explicit via `new Game(airport, altitude, modules)`.

## Rationale
Keeps the aggregate focused on behavior and delegates mutable round state to a single internal component, while preventing global mutation of command singletons and avoiding opinionated defaults inside the domain.
