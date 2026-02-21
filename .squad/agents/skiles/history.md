# Skiles — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Learnings

### Session 1: Codebase Audit & Milestone 1 Planning (2026-02-21)

**Current State Summary:**
- Game loop foundation is ~40% in place, with skeleton code and working value objects
- All 19 unit tests passing (Die, Altitude, Airport domain logic)
- ✅ Working: Die rolls (BlueDie/OrangeDie), Altitude progression, Airport/PathSegment queue, GameModule interface, AxisPositionModule wired with placeholders
- ❌ Missing: GameState aggregating dice, ExecuteCommand implementation, remaining 5+ modules (Engines, Brakes, Flaps, LandingGear, Radio, Concentration), command dispatch binding, win/loss conditions
- Code health: Guard clauses solid, early returns good, but incomplete command implementations throw NotImplementedError

**Proposed Milestone 1 (Base Game Working) — 4 Phases:**
1. **GameState entity** – Aggregate for unused dice per player (refactor from Game.cs) — **PHASE 1 BLOCKER**
2. **ExecuteCommand dispatcher** – Route commands to modules, remove dice, validate turn flow — **PHASE 1 BLOCKER**
3. **Engines + Brakes modules** – Implement with Tenerife-approved rules (parallelizable after Phase 1)
4. **Win/Loss logic** – Landing success, crash detection, reroll mechanics
5. **Round flow complete** – Integrate all loops: roll → assign → advance → reroll/land
6. **Remaining modules** – Flaps, LandingGear, Radio, Concentration (Phase 4)

**Dependency Graph (Critical):**
- **Phase 1 (Foundation):** GameState + ExecuteCommand (1–2 hours) — MUST COMPLETE FIRST
- **Phase 2 (Modules):** Engines, Brakes (parallel, awaiting Tenerife rules)
- **Phase 3 (Round flow):** Win/Loss, landing validation, reroll mechanics
- **Phase 4 (Remaining):** Flaps, LandingGear, Radio, Concentration

**Known Risks:**
- Game.cs mixes aggregate + command orchestration (architectural smell); GameState refactor needed
- AxisPositionModule command constructors still throw NotImplemented; need closure on design
- No tests for Game.ExecuteCommand yet (tests exist but reference GameState not Game)
- Random seeding in Die.Roll() makes tests non-deterministic if many games rolled in sequence (acceptable for now, flag for future)

### Cross-Team Context
- **Sully** established label taxonomy and GitHub backlog
- **Tenerife** provided comprehensive M1 rules and module specifications
- **Aloha** preparing test harness for module implementations

### Session 2: Coffee Tokens Domain Modeling (2026-02-21)
**Outcome:** Designed minimal immutable CoffeeTokenPool value object and GameState integration for Concentration module coffee token mechanic.

**Key Decisions:**
- **CoffeeTokenPool:** Immutable record with Count (0–3), Spend() (throws if empty), Earn() (capped at 3), CanSpend predicate
- **GameState ownership:** TokenPool property + EarnCoffeeToken() / SpendCoffeeToken() methods (shared across players)
- **PlaceDieOnConcentrationCommand:** UseTokenForAdjustment boolean, AdjustedValue optional, Validate() method for invariants
- **ConcentrationModule.PlaceDieOnConcentration():** Validate, deduct token if adjusted, place die, earn token immediately
- **Secret storage:** PendingPlacement internal class for Telegram secret assignments

**Design Principles:**
- Immutability enables auditing, replay, deterministic testing
- GameState-level placement: tokens are shared; natural at aggregate root
- Immediate earn-after-spend: matches board game flow
- Command-driven adjustment: UI chooses value; domain validates
- Explicit guard clauses prevent overspend

**Delivered Artifacts:**
- Minimal domain modeling spec with C# reference implementation (`.squad/decisions.md`)
- Implementation checklist (8 items)
- Test categories enumerated (token count, spend failures, boundaries, immutability, secret storage)
- One orchestration log entry: Skiles (token modeling)

**Ready to Code:**
- [ ] CoffeeTokenPool value object
- [ ] GameState.TokenPool integration
- [ ] PlaceDieOnConcentrationCommand + validation
- [ ] ConcentrationModule implementation + secret storage

**Cross-Coordination:**
- **Tenerife** finalized official rules spec (multi-token pending clarification)
- **Sully** confirmed architecture fit — minimal changes needed
- **Aloha** can now write token-specific test suite

### Session 3: Telegram Architecture + MVP Backlog Sprint (2026-02-21)
**Outcome:** Four agents drafted comprehensive Telegram bot architecture, UX specification, implementation plan; Skiles created `SkyTeam.TelegramBot` project; Sully produced 5-layer architecture + 7-Epic backlog + 8 user interview questions; Tenerife specified full Telegram UX (570+ lines, 7 transcripts).

**Key Decisions:**
- **Project Created:** `SkyTeam.TelegramBot` console app (references `SkyTeam.Domain` directly; adapter/application/presentation layers TBD per Sully architecture)
- **Architecture Drafted:** Domain → Application → Presentation → Telegram Adapter → Bot Host (5-layer clean separation)
- **Core Ports Defined:** `IChatGateway`, `IGameSessionRepository`, `IDiceRoller` (application-level contracts)
- **MVP Backlog Structured:** Epics A–G (foundation → transport → session → round interaction → domain completion → presentation → hardening)
- **UX Specification Locked:** Secret placement (DM-based), public reveal (group), button-driven token mechanics, 7 example transcripts

**Interview Questions for User (prioritized):**
1. DM onboarding required (players must `/start` bot privately)?
2. Strict alternation (one placement at a time) vs. submit-all?
3. Button/inline keyboard UX vs. typed commands?
4. Token transparency (announce immediately vs. round-end reveal)?
5. Persistence required across bot restart?
6. Undo policy (undo-last vs. cancel-round-only)?
7. 2+ humans in group: enforce 2 seated + spectators?
8. Must-have non-base-game UX (pin cockpit, auto-advance, reminders)?

**Delivered Artifacts:**
- `.squad/orchestration-log/2026-02-21T08-22-30Z-sully.md` — Architecture orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-31Z-skiles.md` — Project orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-32Z-tenerife.md` — UX orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-33Z-aloha.md` — QA orchestration log
- `.squad/log/2026-02-21T08-22-00Z-telegram-bot-backlog.md` — Session log
- `.squad/decisions.md` — Merged 4 new decisions (user directive, architecture, project, UX spec)

**Pending Actions:**
- User answers interview questions (UX clarifications)
- Skiles begins Phase 1: GameState + ExecuteCommand (unblocks all downstream Epics)
- Tenerife validates module implementations vs. UX spec
- Aloha integrates test harness with implementation phases

---
