# Tenerife — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Learnings

### Session 1: Milestone 1 Scope Definition (2026-02-21)
**Outcome:** Defined "Base Game Fully Working" scope for M1 as a complete, playable 2-player game from setup to landing.

**Key Decisions:**
- **Core loop identified:** Roll → Assign (alternating) → Resolve (all modules in fixed order) → Advance altitude → Repeat
- **All 7 modules MUST-have:** Axis, Engines, Brakes, Flaps, Gear, Radio, Concentration
- **Altitude track:** 7 segments (6000→0), with reroll tokens at segments 1 & 5
- **Approach track:** Montreal airport default with dynamic plane clearing via Radio module
- **Landing criteria (6 total):** Axis balanced, engines ≥9, brakes ≥6, flaps ≥4, gear deployed, approach clear

**Ambiguities Clarified:**
- Concentration module: post-assignment allocation (recommended)
- Radio die resolution: simultaneous, sum-then-clear (recommended)
- Reroll token capacity: exactly 1 reroll token per game, up to 2 dice per use (official)
- Axis/Brakes/Engines accumulation: cumulative throughout game (assumed; needs user confirmation)

**Delivered Artifacts:**
- Comprehensive M1 scope spec (`.squad/decisions/inbox/tenerife-m1-scope.md`)
- 12 vertical-slice backlog issues with acceptance criteria
- DDD aggregate structure guidance for Skiles
- Testing priority guidance for Aloha
- Risk flagging: Concentration interaction complexity

**Out-of-scope clearly marked:** Variants, AI, persistence, other airports, replay, mobile polish

### Cross-Team Context
- **Sully** established GitHub label taxonomy and issue dependency graph
- **Skiles** audited codebase, confirmed Phase 1 blocker (GameState aggregate)
- **Aloha** preparing test harness to validate module implementations

### Session 2: Concentration Coffee Tokens Finalization & M1 Rules Lock (2026-02-21)
**Outcome:** Reconciled official Sky Team Concentration rules with user clarifications on coffee tokens; produced M1 canonical rules spec; coordinated with Sully (architecture) and Skiles (domain modeling).

**Key Decisions:**
- **Official rules baseline:** Token pool max capacity = 3, gain +1 per die on Concentration, spend k tokens to shift die by ±k before placement (cost `k = |adjusted - rolled|`)
- **Multi-token interpretation locked:** User clarified: spend multiple tokens on same die for ±N shift (e.g., rolled 4 → place as 6 costs 2 tokens)
- **M1 rules canonical reference:** https://www.geekyhobbies.com/sky-team-rules/ — all 7 modules, landing criteria, altitude/approach tracks, Montreal airport
- **Boundary handling:** Die 1 + token → {1,2}; Die 6 + token → {5,6} (no wraparound); no multi-spend wraparound
- **Special case:** Spend token on die → place on Concentration → net zero token change (spend -1, earn +1)

**Delivered Artifacts:**
- M1 Rules Specification with canonical reference (`.squad/decisions.md`)
- Comprehensive Concentration spec with multi-token spend support (`.squad/decisions.md`)
- Acceptance criteria for Skiles (implementation) and Aloha (testing)
- Orchestration log: Tenerife (rules), Sully (architecture), Skiles (domain)

**Cross-Coordination:**
- **Sully** assessed architectural fit (secret placement + token UX) — Excellent ✓
- **Skiles** designed immutable CoffeeTokenPool value object + extended command shape for multi-token — Ready to code ✓
- **Aloha** can now prepare token-specific test cases per domain model and Sully architecture

### Session 3: Decision Consolidation & Team Handoff (2026-02-21)
**Outcome:** Scribe merged decision inbox, updated agent histories, committed `.squad/` state; team unblocked for Phase 1 implementation.

**Key Actions:**
- Deleted deduplicated inbox files (tenerife-rules-spec.md, sully-pr15-review.md)
- Merged multi-token spec into decisions.md (already captured in Skiles token modeling entry)
- Updated Tenerife history with multi-token clarification and rules lock
- Updated Sully history with extended command shape guidance for Skiles
- Committed `.squad/` changes (orchestration logs, session log, updated histories)

**Team Readiness:**
- ✅ M1 rules fully specified and canonical reference locked
- ✅ Architecture guidance ready (Telegram placement, secret storage, token UX)
- ✅ Domain model shape established (multi-token spend support, immutable token pool)
- ✅ Skiles unblocked for Phase 1 (GameState + ExecuteCommand)
- ✅ Aloha can begin test harness preparation (tokens, secret placement, module boundaries)

---
