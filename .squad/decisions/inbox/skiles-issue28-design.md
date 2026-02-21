# Issue #28 — Application round/turn state + secret hands (Skiles)

## Context
We need application-layer orchestration state for Telegram UX where each player sees only their rolled dice hand, while the group chat enforces strict alternation and resolves after 8 placements.
Domain types (`Player`, `BlueDie`, `OrangeDie`, `GameState`) are internal and must remain UI-agnostic; therefore Telegram and application orchestration cannot depend on them.

## Decision
Implement a small, pure application state model in `SkyTeam.Application.Round`:

- `PlayerSeat` (`Pilot` / `Copilot`) + `Other()` helper for strict alternation.
- `DieValue` value object (1–6).
- `SecretDiceHand` (exactly 4 dice, per-player, tracks used/unused dice by index).
- `RoundTurnState`:
  - Holds `RoundNumber`, `StartingPlayer`, `CurrentPlayer`, hands, `Placements` list, and a `RoundPhase`.
  - Enforces strict alternation in `RegisterPlacement()`.
  - Transitions to `ReadyToResolve` automatically after 8 total placements.
  - Provides `UndoLastPlacement()` guard-railed by the existing UX policy: only the player who placed last can undo, and only before the other player plays.

## Rationale
- Keeps Telegram SDK types out of application and domain.
- Deterministic, immutable-ish state makes downstream testing trivial (Aloha can assert state transitions without any randomness).
- Provides explicit guardrails for upcoming issues: DM roll (#29) uses the hands; public placement (#30) uses the placement log; resolve-after-8 (#32) uses the phase transition.

## Follow-ups
- Wire this state into the session repository/use-cases once the application service layer is introduced.
- Aloha: add deterministic tests for placement alternation, max 8 placements, and undo gating.
