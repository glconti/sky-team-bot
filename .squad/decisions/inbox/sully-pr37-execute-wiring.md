# 2026-02-21: PR37 Execute wiring — token pool + landing checks

**By:** Sully (Architect)

## Decision
For PR #37 / issue #31, keep the **coffee token pool owned by `ConcentrationModule`** (as the authoritative source of token count).

- `Game.GetAvailableCommands()` now passes `ConcentrationModule.TokenPool` (fallback: 0) into module command generation.
- All token-adjusted `GameCommand.Execute(Game)` implementations spend tokens via `game.SpendCoffeeTokens(k)`, and `Game.SpendCoffeeTokens(k)` delegates to `ConcentrationModule.SpendCoffeeTokens(k)`.

Also, landing win/loss checks are evaluated as independent criteria (Engines ≥ 9, Brakes ≥ 6, Flaps ≥ 4, Landing Gear ≥ 3, Axis within [-2,2], Approach cleared), and `NextRound()` no longer enforces an additional “mandatory placements” loss gate beyond the existing command-availability rules.

## Rationale
This keeps the PR37 command-execution wiring minimal and consistent, matches the current module/test contract, and avoids catching/rewrapping non-rule exceptions (only rule losses use `GameRuleLossException`).
