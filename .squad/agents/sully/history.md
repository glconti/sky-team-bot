# Sully — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Learnings

### Session 1: Backlog Setup & GitHub Label Taxonomy (2026-02-20)
- Created 25 GitHub labels across 5 categories: Type, Priority, Status, Area, and Routing
- Established 14 vertical-slice issues for M1 foundation work
- Key design decision: All issues represent end-to-end playable increments, not infrastructure-only work
- Dependency graph: Rules clarification (#14) is the critical path blocker for module work
- Squad routing embedded in labels for easy filtering and handoff
- Status model: `ready` for foundation, `blocked` for work waiting on clarification, `review` for PR gates
- Milestones structured for incremental delivery: MVP → Bot → Polish → Advanced

### Cross-Team Context
- **Tenerife** completed M1 rules specification (all 7 modules, landing criteria, clarifications)
- **Skiles** identified Phase 1 blocker: GameState aggregate + ExecuteCommand dispatcher must be built first
- **Aloha** standing by with test harness for module implementations

### Session 2: Telegram Placement + Concentration Token Architecture (2026-02-21)
**Outcome:** Assessed secret placement and coffee token UX fit against DDD aggregate pattern; produced extended interaction contract and command model for Skiles.

**Key Decisions:**
- **Secret placement:** Architecturally Excellent — aligns with DDD game aggregate. No public infrastructure changes needed.
- **Token command model:** Option A (Recommended) — Token spend as multi-token parameter on PlaceDieCommand (`SpendTokens: int?` or `AdjustedValue: int?` with derived cost). Prevents ordering ambiguity and state-machine complexity.
- **Extended command shape:** Single `PlaceDieCommand` with optional adjustment parameters: `UseTokenForAdjustment: bool`, `AdjustedValue: int?`, `TokensToSpend: int` (derived). Validation method checks pool availability and value validity.
- **Telegram contract:** Ephemeral UI (private keyboards, color-coded options), readiness handling, reveal broadcasting. Domain stays UI-agnostic.
- **Module resolution order locked:** Land on Concentration → Gain token → Advance (prevents race conditions)

**Delivered Artifacts:**
- Telegram placement + token architecture assessment (`.squad/decisions.md`)
- Extended interaction contract: Bot ↔ Domain interface with multi-token support
- Command shape guidance for Skiles: `PlaceDieOnConcentrationCommand` with spend validation
- One orchestration log entry: Sully (architecture assessment)

**Cross-Coordination:**
- **Tenerife** finalized official rules spec with multi-token spend locked (cost `k = |adjusted - rolled|`)
- **Skiles** extended domain model to support multi-token spend per this architecture guidance
- **Aloha** can incorporate secret storage + multi-token testing per Skiles' extended proposal

### Session 3: Decision Consolidation & Team Handoff (2026-02-21)
**Outcome:** Scribe merged decision inbox, updated agent histories, committed `.squad/` state; team unblocked for Phase 1 implementation with multi-token spec fully validated.

**Key Actions:**
- Merged multi-token spec into decisions.md (Tenerife canonical reference + Skiles extended command model)
- Deleted deduplicated inbox files
- Updated Sully history with multi-token command shape guidance
- Committed `.squad/` changes (orchestration logs, session log, updated histories)

**Team Readiness:**
- ✅ Multi-token spec complete: cost `k = |adjusted - rolled|`, no wraparound, full die value range supported
- ✅ Command shape extended: `AdjustedValue` optional parameter with derived spend cost
- ✅ Architecture validated: Single command, no separate token spend command, domain ↔ UI boundary clear
- ✅ Skiles can implement Phase 1 with confidence in token model and command shape
- ✅ Aloha can test multi-token flows (1-token, 2-token, 3-token, insufficient tokens edge cases)

### Session 4: Telegram Architecture + MVP Backlog Sprint (2026-02-21)
**Outcome:** Four agents drafted comprehensive Telegram bot architecture, UX specification, implementation plan, and test strategy; produced 4 orchestration logs + session log + merged decisions.

**Key Decisions:**
- **Sully:** 5-layer architecture (Domain → Application → Presentation → Telegram Adapter → Bot Host); 7 Epic MVP backlog (A–G) with vertical slices; 8 user interview questions
- **Skiles:** Created `SkyTeam.TelegramBot` console project + integrated into solution (`.slnx`)
- **Tenerife:** Comprehensive Telegram UX specification (570+ lines, 7 example transcripts, secret placement + button-driven token mechanics)
- **Aloha:** Test-backlog recommendations (verbal; integrated into decisions if formal artifact needed)

**Delivered Artifacts:**
- `.squad/orchestration-log/2026-02-21T08-22-30Z-sully.md` — Architecture + MVP backlog orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-31Z-skiles.md` — Project initialization orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-32Z-tenerife.md` — UX specification orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-33Z-aloha.md` — QA recommendations orchestration log
- `.squad/log/2026-02-21T08-22-00Z-telegram-bot-backlog.md` — Session log
- `.squad/decisions.md` — Merged 4 new decision entries (user directive, Sully architecture, Skiles project, Tenerife UX)

**Team Synchronization:**
- **Sully → Skiles:** Epic roadmap defines implementation phases (A–G); 8 interview questions clarify UX tradeoffs
- **Tenerife → Skiles:** UX spec provides binding contract for Telegram adapter (button rendering, state display, message formats)
- **Sully ↔ Tenerife:** Architecture/UX alignment on secret placement (DM-based), public reveal (group broadcast), token UX (buttons, not commands)
- **Aloha → Team:** Test recommendations ready for Epic-by-Epic implementation (unit → integration → E2E)

**Pending Actions:**
- User answers Sully's 8 interview questions (UX clarifications: DM onboarding, turn discipline, persistence, undo/cancel policy, etc.)
- Skiles begins Phase 1: GameState aggregate + ExecuteCommand dispatcher (critical path for all Epics B–G)
- Tenerife validates module implementations against UX spec (readiness gate per Epic D–F)
- Aloha integrates test harness with Skiles' implementation phases

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

**Sully's PR #37 Fix:**
- **Token Pool Ownership:** Coffee token pool owned by `ConcentrationModule` (authoritative source)
- **Spend Delegation:** `Game.SpendCoffeeTokens(k)` → `ConcentrationModule.SpendCoffeeTokens(k)`
- **Landing Checks:** 6 independent criteria (Engines ≥9, Brakes ≥6, Flaps ≥4, Gear ≥3, Axis ∈[-2,2], Approach cleared); no mandatory placement loss gate
- **Tests:** Green; wiring minimal and consistent

**Tenerife's Loss Semantics Checklist:**
- **Explicit Losses (throw `GameRuleLossException`):**
  1. Axis Out of Balance at Landing: `AxisPosition < -2 OR > 2`
  2. Speed Too High at Landing: `BrakesValue < EnginesValue`
  3. Approach Track Collision: ANY plane token remains
  4. Altitude Exhausted: No segments left without landing
  5. Mid-Round Axis Invariant: Position ≥ ±3 after both dice placed
- **Invalid Moves (prevent via command validation):**
  1. Brakes/Flaps sequence violations
  2. Duplicate placements (Landing Gear, Axis, Engines)
  3. Concentration/Radio exhaustion
  4. Token overspend
  5. Die not available (bot bug, not user error)
- **Bugs Noted:**
  - Axis landing check currently == 0, should check ∈[-2,2]
  - Speed comparison uses >, verify if intended
  - Altitude exhaustion not explicit; needs implementation
  - Reroll mechanics not visible
- **Rationale:** Separating losses from validation errors enables proper exception handling and deterministic game-state management

**Aloha's ExecuteCommand Smoke Tests:**
- Added base-scenario coverage: valid command execution, token spend, invalid rejection
- AAA pattern, FluentAssertions, data-driven matrix
- Tests green; ready for broader integration suite

**Scribe's Logging & Consolidation:**
- **Orchestration Logs (3):** Sully PR#37, Tenerife loss semantics, Aloha ExecuteCommand
- **Session Log:** PR#37 unblock summary
- **Decisions Merge:** Moved inbox files into decisions.md; deleted processed inbox files
- **Agent Histories:** Updated Scribe, Sully, Tenerife, Aloha with session context
- **Git Commit:** Staged and committed `.squad/` changes

**Team Readiness:**
- ✅ PR #37 token wiring fixed and validated
- ✅ Loss semantics documented; ready for implementation validation
- ✅ ExecuteCommand baseline established; smoke tests passing
- ✅ Next phase unblocked: Skiles integration testing + altitude/reroll redesign

---
