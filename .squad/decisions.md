# Decisions

> Append-only ledger of team decisions. Never retroactively edit entries.

## 2026-02-20T22:20:31Z: User directive

**By:** Gianluigi Conti (via Copilot)  
**Decision:** Initialize GitHub issues with color-coded labels, create milestones (Milestone 1 focuses only on having the base game fully working without optional modules), and plan incremental features by priority and dependencies; prefer working vertical slices; highlight things to clarify.  
**Rationale:** User request — captured for team memory

---

## 2026-02-20T22:20:31Z: GitHub label taxonomy & M1 backlog structure (Sully)

**By:** Sully (Architect)  
**Decision:** Established 25 color-coded labels across 5 categories (Type, Priority, Status, Area, Routing) and created 14 vertical-slice M1 issues (#1–#14) with explicit dependency linking.  
**Key Labels:**
- **Type** (Purple): domain, module, command, test, infra
- **Priority** (Gradient): critical → high → medium → low
- **Status** (Signals): ready, blocked, review
- **Area** (Dark Green): game-aggregate + 7 module areas
- **Routing** (Lavender): squad member assignment

**Key Issues:**
- #1–2: Foundation (Game init, Round advancement) — status: ready
- #3–9: Core modules (Axis through Concentration) — status: blocked (waiting on rules)
- #10–11: ExecuteCommand + Win/Loss — status: blocked
- #12–13: Tests — status: blocked
- #14: Rules clarification — status: ready (single source of truth)

**Rationale:** Vertical slices enable end-to-end testing and incremental delivery. Dependency graph makes critical path explicit.

---

## 2026-02-21T00:00:00Z: Milestone 1 scope definition (Tenerife)

**By:** Tenerife (Rules Expert)  
**Decision:** M1 = "Base Game Fully Working" = complete playable 2-player game from setup to landing (all 7 modules mandatory).

**Core Loop:** Roll (4 blue/4 orange) → Assign (alternating) → Resolve (fixed module order) → Advance altitude → Repeat

**All 7 Modules (MUST-have):**
1. **Axis:** Balance check ([-2, 2]), loses if out of range at landing
2. **Engines:** Thrust accumulation (≥9 at landing to pass)
3. **Brakes:** Descent control (≥6 at landing)
4. **Flaps:** Deployment (≥4 at landing)
5. **Landing Gear:** Binary deployment (required)
6. **Radio:** Clear planes from Approach track before altitude 0
7. **Concentration:** Wildcard boost (post-assignment, flexible reallocation)

**Landing Criteria (all 6 must pass):**
- Axis balanced [-2, 2]
- Engines ≥ 9
- Brakes ≥ 6
- Flaps ≥ 4
- Landing Gear deployed
- Approach track fully cleared

**Altitude Track:** 7 segments (6000→0 ft), reroll tokens at segments 1 & 5, player alternates after each descent

**Approach Track:** Montreal airport, 7 segments [0, 0, 1, 2, 1, 3, 2] planes per segment

**Clarifications Made:**
- Concentration: post-assignment allocation (recommended)
- Radio: simultaneous resolution, sum-then-clear (recommended)
- Reroll: exactly 1 token per game, up to 2 dice per use (official rule)
- Accumulation: Axis/Engines/Brakes all cumulative (assumed, needs user confirmation)

**Out-of-Scope:** Variants, AI, persistence, other airports, mobile polish, replay

**Rationale:** Comprehensive rules documentation unblocks module implementation and enables parallel team work.

---

## 2026-02-21T00:00:00Z: Codebase audit & M1 work breakdown (Skiles)

**By:** Skiles (Implementation Lead)  
**Decision:** Identified current state (~40% complete), critical blockers, and proposed 4-phase work breakdown for M1.

**Current Working Foundation:**
- ✅ Value Objects: Die, BlueDie, OrangeDie, PathSegment, AltitudeSegment
- ✅ Entities: Altitude (7-segment), Airport (Montreal), GameModule interface
- ✅ Tests: 19 unit tests passing (die randomness, altitude progression, airport logic)

**Critical Blockers:**
1. **GameState aggregate:** Missing class; tests reference it but code doesn't exist → blocks all game flow
2. **ExecuteCommand dispatcher:** Empty stub; cannot route commands to modules
3. **Module implementations:** Engines, Brakes, Flaps, LandingGear, Radio, Concentration not started
4. **Win/Loss conditions:** Not implemented

**Proposed 4-Phase Work:**
- **Phase 1 (Foundation):** GameState aggregate + ExecuteCommand dispatcher (1–2 hours) — must complete first
- **Phase 2 (Modules):** Engines + Brakes (parallelizable, awaiting Tenerife rules)
- **Phase 3 (Round flow):** Win/loss validation, landing checks, reroll mechanics
- **Phase 4 (Remaining):** Flaps, LandingGear, Radio, Concentration

**Architectural Notes:**
- Game class mixes aggregate + command orchestration (refactor with GameState)
- AxisPositionModule command constructors throw NotImplemented (design closure needed)
- Die.Roll() non-deterministic (acceptable for now; flag for future)

**Rationale:** Phase 1 is the critical path. Once complete, team can parallelize module work while following Tenerife's rules.

---

## 2026-02-20T22:39:45Z: Telegram dice assignments are secret

**By:** Gianluigi Conti  
**Decision:** For Telegram play, dice assignments are **secret**: each player submits placements privately to the bot, and the bot reveals/announces outcomes at resolution time.
**Rationale:** Preserves the base game's hidden-information constraint while remaining playable in chat.

---
