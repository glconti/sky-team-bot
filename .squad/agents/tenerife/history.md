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

### Session 4: Telegram Architecture + MVP Backlog Sprint (2026-02-21)
**Outcome:** Four agents drafted comprehensive Telegram bot architecture, UX specification, implementation plan; Sully produced 5-layer architecture + 7-Epic backlog + 8 user interview questions; Tenerife specified full Telegram UX (570+ lines, 7 transcripts); Skiles created `SkyTeam.TelegramBot` project.

**Key Decisions (Tenerife's UX Spec Input):**
- **Secret Placement:** DM-based dice assignments (inline keyboards, ephemeral rendering, no group visibility)
- **Public Reveal:** Bot announces outcomes in group chat after both players ready (full module resolutions, state updates)
- **Token Mechanics (Button-Driven):** Show token-cost options as distinct buttons (e.g., `[Axis]` vs `[Axis] 💰2`); spend declaration announced publicly (not secret); gain +1 per Concentration placement (capped at 3)
- **Round Flow:** 5 phases — Roll → Assign (secret) → Reveal & Resolve (public) → Altitude Descent → Win/Loss Check
- **Commands (Minimal):** Setup only (`/start_game`, `/join`, `/rules`, `/state`); in-game actions via buttons (no typed commands during rounds)
- **Turn Discipline:** Alternating players; 60-second timeout; bot pings at 30s, auto-skips at 120s

**7 Example Transcripts (Deterministic Test Cases):**
1. Simple round, no tokens, both cooperate
2. Token spend (multi-token adjustment)
3. Reroll declaration + new dice
4. Landing & victory (all criteria pass)
5. Collision loss (approach track full)
6. Axis imbalance loss at landing
7. Concentration token spend + earn (net zero)

**Edge Cases Specified:**
- Token pool 0 & spend attempt → buttons disable options (gray out)
- No reroll available → prevent button click
- Concentration placed + die pre-adjusted → net zero token change
- Pilot bad roll (all 1s) → Copilot sees "Pilot thinking…" up to 120 sec
- Radio clears all planes → "Approach track cleared! ✅" (no error, capped at 0)
- Altitude at 6000 ft round 7 → no landing check (only at 0)

**Implementation Hooks:**
- **Bot:** Ephemeral keyboard rendering (only to active player), session state management, broadcast & reveal after both ready
- **Domain:** Accept `PlaceDieCommand`, fixed module resolution order (Axis → Engines → Brakes → Flaps → Landing Gear → Radio → Concentration), landing check (6 criteria)
- **Presentation:** Chat UI models (`ChatMessage`, `ChatKeyboard`, `ChatUiEvent`) transport-agnostic

**Delivered Artifacts:**
- `.squad/orchestration-log/2026-02-21T08-22-32Z-tenerife.md` — UX orchestration log
- `.squad/log/2026-02-21T08-22-00Z-telegram-bot-backlog.md` — Session log
- `.squad/decisions.md` — Merged Tenerife UX spec (2026-02-21T08:20:30Z)

**Team Alignment:**
- **Sully → Tenerife:** Architecture validates UX (secret placement fits DDD, token spend as command parameter, domain UI-agnostic)
- **Sully → Skiles:** Epic roadmap provides implementation skeleton (A–G); interview questions clarify UX tradeoffs before code lockdown
- **Tenerife → Skiles:** 7 example transcripts + edge cases provide binding contract for Telegram adapter (button rendering, state display, message formats)
- **Tenerife → Aloha:** UX spec + transcripts enable deterministic testing (E2E scenarios, edge cases, rule validation)

**Pending Actions:**
- User answers Sully's 8 interview questions (UX clarifications: DM onboarding, turn discipline, persistence, undo/cancel, etc.)
- Skiles begins Phase 1: GameState + ExecuteCommand (critical path for all downstream Epics B–G)
- Aloha prepares test harness per Tenerife's 7 example transcripts (deterministic E2E tests)

---
