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

### Session 5: Issue #31 Completion Round (2026-02-21T10:21:03Z)
**Outcome:** Tenerife finalized comprehensive 500+ line spec documenting all 7 modules, landing win/loss criteria, resolution order, and edge cases. Skiles delivered draft PR #37 with all 7 module implementations and coffee-token multi-spend. Aloha created draft PR #38 with test coverage for boundaries, landing outcomes, and token mechanics.

**Tenerife's Deliverables:**
- **Spec document:** Issue #31 specification (tenerife-issue31-spec.md) covering modules 1–7 with detailed state, placement rules, resolution timing, landing criteria per module, edge cases, and 10-section verification checklist
- **Module Order:** Axis → Engines → Brakes → Flaps → Landing Gear → Radio → Concentration (fixed, documented)
- **Landing Criteria:** 6 conditions (all must pass for win: axis [-2,+2], engines ≥9, brakes ==3 and >speed, flaps ==4, gear ==3, approach clear)
- **Loss Conditions:** Axis imbalance (immediate), altitude exhausted (final round), landing failure (any criterion fails)
- **Clarifications:** Brakes criterion, Engines final round suppression, Landing Gear idempotence, multi-token spend bounds, token pool scoping, net token change (spend + concentration), reroll out-of-scope

**Aloha's Findings:**
- **Spec Mismatch #1:** Brakes landing criterion inconsistent — spec says `BrakesValue == 3 AND BrakesValue > LastSpeed` but BrakesValue is switch count (0–3); if LastSpeed ≥ 9, condition is impossible
- **Spec Mismatch #2:** Current code treats BrakesValue as last activated value (2/4/6) and checks `BrakesValue >= 6` without speed comparison
- **Recommendation:** Clarify intended landing check before finalizing tests
- **Token-Adjusted Commands:** Validated design surface (e.g., `Axis.AssignBlue:1>3`); tests confirm command surfacing, spend behavior, pool/die bounds

**Skiles' Implementations:**
- All 7 modules working in draft PR #37
- Landing validation logic complete
- Command ID surface for token-adjusted placements operational
- GameState refactor complete; ExecuteCommand dispatcher wired

**Cross-Agent Dependencies:**
- Awaiting Sully code review (module design, command dispatcher, aggregate cohesion)
- Awaiting user clarification on Brakes landing criterion semantics
- Concentration token design complete; ready for Telegram adapter once Epic B baseline established

**Delivered Artifacts (Session 5):**
- `.squad/orchestration-log/2026-02-21T10-21-03Z-skiles.md` — Skiles orchestration log
- `.squad/orchestration-log/2026-02-21T10-21-03Z-tenerife.md` — Tenerife orchestration log
- `.squad/orchestration-log/2026-02-21T10-21-03Z-aloha.md` — Aloha orchestration log
- `.squad/log/2026-02-21T10-21-03Z-ralph-round.md` — Session log
- `.squad/decisions.md` — Merged Tenerife spec + Aloha findings + user directive (placement undo)
- Updated agent histories (Tenerife, Skiles, Aloha)

**Pending Escalations:**
1. **Brakes Landing Criterion:** Reconcile spec vs. code semantics before finalizing tests
2. **Token-Adjusted Command IDs:** Await Telegram button rendering spec (Sully + Tenerife)

### Session 6: PR #37 Unblock & Loss Semantics Finalization (2026-02-21T18:06:26Z)
**Outcome:** Sully fixed token pool wiring in PR #37. Tenerife produced comprehensive loss condition checklist (15 explicit losses, 8 invalid-move categories, 3 TODOs). Aloha added ExecuteCommand smoke tests. Scribe logged all work and merged decisions.

**Key Contributions:**
- Finalized loss-condition semantics with decision checklist (explicit losses vs. invalid moves)
- Documented all bug findings: axis landing check, speed comparison, altitude exhaustion, reroll mechanics
- Provided implementation validation checklist (15 must-have loss conditions, 8 prevent-via-validation categories)
- Ready for Skiles to validate against current PR #37 code and address identified bugs

**Delivered Artifacts (Session 6):**
- `.squad/orchestration-log/2026-02-21T18-06-26Z-tenerife.md` — Loss semantics orchestration log
- `.squad/decisions.md` — Merged loss condition checklist (comprehensive taxonomy + bug findings)

**Pending Actions:**
- Sully validates PR #37 fixes against loss semantics checklist
- Skiles addresses identified bugs (axis check, speed comparison, altitude exhaustion)
- Aloha finalizes test coverage once bugs clarified

### Session 7: Solo Testing Mode Specification (2026-03-02)
**Outcome:** Tenerife produced comprehensive solo mode specification clarifying that solo mode is a **non-rule change** (purely a "who controls what" switching mechanism) with no domain model impacts.

**Key Decisions:**
- **Solo mode definition:** Single player controls both Pilot (blue) and Copilot (orange) seats; all 7 modules and win/loss conditions remain identical
- **Placement mechanics (Option B selected):** Player rolls both hands, places all 8 dice simultaneously with full visibility (preserves game integrity, supports scenario testing, minimizes code complexity)
- **Domain model:** Game aggregate requires NO changes; mode-agnostic by design
- **Session layer:** Recommended optional `GameMode` enum (TwoPlayer/Solo) at application layer for lobby routing and UI adaptation
- **Module behavior:** Concentration and Radio modules work identically; solo player implicitly "controls both" for agreement purposes
- **Win/loss parity:** All landing criteria, loss conditions, altitude track, approach track, coffee tokens, reroll tokens unchanged
- **Concentration/Radio:** No special solo handling; existing mechanics apply identically

**Rationale for Recommended Approach:**
- **Visible simultaneous placement** (Option B) best supports testing use cases (developer can set up exact dice scenarios) while preserving game rules and minimizing aggregate complexity
- **Sequential placement** (Option A) rejected: introduces asymmetric information not present in 2-player mode
- **Hidden then revealed** (Option C) rejected: adds friction to testing workflow

**Delivered Artifacts:**
- `.squad/agents/tenerife/solo-mode-spec.md` — Full specification (10 sections, 100+ rule preservation checks, implementation roadmap for Sully/Skiles/Aloha)
- `.squad/decisions/inbox/tenerife-solo-mode-spec.md` — Architectural decisions summary with cross-team coordination guidance

**Cross-Team Coordination:**
- **Sully:** Review session layer mode flag recommendations; update Telegram/WebApp UI to show all 8 dice (solo) vs. current player 4 dice (2-player)
- **Skiles:** Validate domain implementations are mode-agnostic (no code changes needed); run domain tests with solo instances
- **Aloha:** Prepare solo test harness; verify win/loss parity with 2-player rules

**Critical Design Insight:**
Solo mode is a **session/presentation concern**, not a domain rule concern. The `Game` class alternates Pilot/Copilot regardless of how many humans play. This separation keeps the domain pure and testing flexible.

---
