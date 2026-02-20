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

### Session 2: Concentration Coffee Tokens Finalization (2026-02-21)
**Outcome:** Reconciled official Sky Team Concentration rules with user clarifications on coffee tokens; coordinated with Sully (architecture) and Skiles (domain modeling).

**Key Decisions:**
- **Official rules baseline:** Token pool max capacity = 3, gain +1 per die on Concentration, spend 1 to adjust die by ±1 before placement
- **User clarifications locked:** Shared pool, cap at 3, multi-token spend (pending interpretation clarification), token-cost options visually distinct in Telegram UI
- **Open Question:** Does multi-token mean: (A) spend multiple on same die for ±N shift, or (B) multiple dice per round? Escalated to Gianluigi for clarification
- **Boundary handling:** Die 1 + token → {1,2}; Die 6 + token → {5,6} (no wraparound)
- **Special case:** Spend token on die → place on Concentration → net zero token change (spend -1, earn +1)

**Delivered Artifacts:**
- Comprehensive official spec with edge case resolutions (`.squad/decisions.md`)
- Acceptance criteria for Skiles (implementation) and Aloha (testing)
- Three orchestration log entries: Tenerife (rules), Sully (architecture), Skiles (domain)

**Cross-Coordination:**
- **Sully** assessed architectural fit (secret placement + token UX) — Excellent ✓
- **Skiles** designed immutable CoffeeTokenPool value object + GameState integration — Ready to code ✓
- **Aloha** can now prepare token-specific test cases per domain model

---
