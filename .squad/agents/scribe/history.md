# Scribe — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Learnings

### Session 2026-02-21 — PR #37 Unblock
- **Sully & Tenerife & Aloha deployment:** Fixed token pool wiring, codified loss semantics, added ExecuteCommand smoke tests.
- **Key Decision:** Token pool owned by `ConcentrationModule`; `Game.SpendCoffeeTokens(k)` delegates to module.
- **Rules Alignment:** Separated loss conditions (throw `GameRuleLossException`) from invalid moves (prevent via command validation).
- **Bugs Captured:** Axis landing check too strict (== 0 vs. ∈[-2,2]); speed comparison edge case; altitude exhaustion not explicit.
- **Next Phase:** Skiles integration testing + altitude/reroll redesign.

<!-- Append learnings below. -->
