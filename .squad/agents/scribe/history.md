# Scribe — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Learnings

### Session 2026-03-02 — Ralph Round 5 & Aloha Batch
- **Aloha #86 Completion:** Manual QA matrix for Telegram Mini App integrated into readme.md. Matrix covers 8 client variants, 2 launch surfaces, happy path, error scenarios, multi-player sync, edge cases, and 9-point release checklist.
- **Decisions Inbox Merge:** Merged `decisions/inbox/aloha-issue-86.md` → `decisions.md` (append-only ledger). Deleted inbox file after merge.
- **Orchestration Logs:** Created `2026-03-02T00Z34Z00-aloha.md` (Aloha #86 completion) and session log `2026-03-02T00Z35Z00-ralph-round5.md`.
- **Coordinator Update:** Updated `identity/now.md` with current focus (Telegram Mini App rollout: persistence, launch flow, QA hardening). Active issues: [76, 80, 85, 86, 87].
- **Key Decision:** QA matrix as living document in readme.md (always versioned, discoverable, updated with features). Tester-facing, practical (30–60 min execution), concurrency-aware, accessibility-first.
- **Next Phase:** Git commit `.squad/` changes. Monitor #76/#80/#85/#87 in Ralph rounds. Propose "QA sign-off" merge gate to Sully once matrix stabilizes.

### Session 2026-02-21 — PR #37 Unblock
- **Sully & Tenerife & Aloha deployment:** Fixed token pool wiring, codified loss semantics, added ExecuteCommand smoke tests.
- **Key Decision:** Token pool owned by `ConcentrationModule`; `Game.SpendCoffeeTokens(k)` delegates to module.
- **Rules Alignment:** Separated loss conditions (throw `GameRuleLossException`) from invalid moves (prevent via command validation).
- **Bugs Captured:** Axis landing check too strict (== 0 vs. ∈[-2,2]); speed comparison edge case; altitude exhaustion not explicit.
- **Next Phase:** Skiles integration testing + altitude/reroll redesign.

<!-- Append learnings below. -->
