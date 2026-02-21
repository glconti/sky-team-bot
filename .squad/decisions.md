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

## 2026-02-20T22:43:49Z: Module progress is cumulative

**By:** Gianluigi Conti  
**Decision:** Engines/Brakes/Flaps (and similar tracks) **accumulate across the whole game** until landing; they do not reset each round.
**Rationale:** Matches intended base-game progression and informs module state modeling.

---

## 2026-02-20T22:51:41Z: Concentration coffee tokens — official rules + multi-token clarification

**By:** Gianluigi Conti (via Copilot)  
**Decision:** Concentration uses coffee tokens per the official Sky Team rules at https://www.geekyhobbies.com/sky-team-rules/#concentration with these locked clarifications:
- Coffee tokens form a **shared pool** with **max capacity 3** (pool can be emptied/refilled many times, but never exceeds 3).
- When a die is placed on Concentration, the pool gains **+1** token (capped at 3).
- Before placing a die on any module, a player may spend tokens to adjust the die value.
  - **Cost:** spend `k` tokens (where `k` = number of steps shifted from rolled value).
  - **Effect:** the die is treated as the adjusted value (must remain within 1–6; no wraparound).
  - Example: rolled 4 can be placed as 2/6 (spending 2 tokens), or as 3/5 (spending 1 token).
- Telegram UX: show token-cost options distinctly (e.g., special color prefix like "💰") and include the token pool count in the shared game state.

**Open Question:** Does "multiple tokens may be spent" mean:
- A: Spend multiple tokens on the *same die* in a single placement (e.g., spend 2 tokens to shift a 3 to {1,2,3,4,5})?
- B: Multiple *dice* can each receive a token spend in the same round (allowed by default)?

**Recommendation:** Escalate to Gianluigi for clarification before implementation locks to Option A or B.

**Rationale:** Align with official rules while supporting the requested multi-token usage in Telegram. Reference rules source clarifies the mechanic unambiguously.

---

## 2026-02-21T13:00:00Z: Concentration coffee tokens — official specification reconciliation (Tenerife)

**By:** Tenerife (Rules Expert)  
**Decision:** Official Sky Team Concentration rules are implemented as specified at https://www.geekyhobbies.com/sky-team-rules/#concentration with these clarifications:

**Token Gain:**
- Trigger: When a die is successfully placed on the Concentration module.
- Amount: +1 token per placement.
- Maximum Capacity: **3 tokens per game** (shared pool, cannot exceed 3).
- Timing: Tokens gained **immediately after** die is placed.
- When capacity is full: No additional tokens gained; placement resolves but no token collected.

**Token Spend:**
- When: **Before** a die is placed on any module during assignment phase.
- Effect: Die is treated as an **adjacent value** (±1 from rolled value).
  - Rolled 1 → becomes {1, 2} (no wraparound to 0).
  - Rolled 2–5 → becomes {die-1, die, die+1}.
  - Rolled 6 → becomes {5, 6} (no wraparound to 7).
- Cost: 1 token per die per placement (single-token spend locked for simplicity).
- Visibility: Spend declaration is **announced publicly** (not secret) to maintain game transparency in Telegram play.
- Consequence: Spend is irrevocable once declared; cannot be reversed after placement committed.

**Multiple Tokens Per Die:**
- Current spec locks to: **Max 1 token per die per placement**.
- Pending user clarification on multi-token interpretation (see prior decision).

**Special Case: Spend + Place on Concentration:**
- If player spends 1 token to adjust a die, then places that die on Concentration:
  - Token is spent (pool decreases by 1).
  - Die is placed on Concentration.
  - Token is earned immediately (pool increases by 1).
  - **Net result:** Token count unchanged.
- Rationale: Concentration is the investment action; token gain rewards the module choice, independent of token spend.

**Edge Cases Resolved:**
- Pool at 0 tokens: Cannot spend (no debt allowed).
- Pool at 3 tokens: Placement succeeds, no token earned (cap enforced).
- Die 1 + token spend: Becomes {1, 2} (no wraparound).
- Die 6 + token spend: Becomes {5, 6} (no wraparound).
- Reroll interaction: Tokens spent/earned before reroll. If die is rerolled, it can be re-spent in subsequent round.
- End-of-game: Unused tokens have **no effect** on landing criteria. Tokens are purely a round-to-round resource.

**Rationale:** This reconciliation is 99% faithful to official Sky Team rules. The spec enables clear implementation, deterministic testing, and fair Telegram UX. The only remaining open question is multi-token spend interpretation (depends on user clarification).

---

## 2026-02-21T13:00:01Z: Telegram secret placement + Concentration token UX — architecture assessment (Sully)

**By:** Sully (Architect)  
**Decision:** Secret placement and coffee token mechanics are architecturally clean with the following contract:

**Secret Placement:**
- **Architectural Fit:** Excellent — aligns with existing DDD game aggregate pattern.
- Players submit placements privately; bot reveals outcomes only at resolution.
- Command pattern already in place; placement commands naturally private to submission.
- **Bot Responsibility:**
  - Render ephemeral/private choice buttons (Telegram inline keyboard, visibility scoped to current player).
  - Accumulate submissions off-game-state (session dict keyed by player + turn).
  - Once both players ready, call domain `game.ExecuteRound(placements)`.
  - Reveal outcomes to both players in broadcast message.
- **Domain Responsibility:** Accept placement list, validate, execute modules, update state—oblivious to presentation.

**Token Mechanic Command Model:**
- **Recommended:** Token spend as command parameter (Option A).
- Single composable `PlaceDieCommand` with `SpendTokenForAdjacent` boolean flag.
- Prevents ordering logic ambiguity and state-machine complexity.
- **Telegram UX:** Same button, different parameter — "Place 4 here" + (if tokens > 0) "or place 3/5 (costs 1 token)".
- **Rejected Option B:** Separate `SpendTokenCommand` would split placement into two commands, break game flow, introduce ambiguous state.

**Minimal Interaction Contract:**
- Ephemeral UI rendering: Private keyboards, color-coded token-spend options.
- Readiness & timeout handling: When both players submit → call `game.ExecuteRound()`.
- Reveal broadcasting: Format round outcomes for Telegram, update shared game display (altitude, token pool, module states).

**Architectural Constraints & Mitigations:**
- No token-count leaks during submission (reveal only in final broadcast).
- No Telegram types in domain (primitives only).
- Module resolution order locked: Land on Concentration → Gain token → Advance.
- Command parameters part of submission; not retroactively editable.

**Rationale:** DDD aggregate pattern is sufficient. No core changes needed; extend `GameModule` with post-round callback. Keep domain UI-agnostic.

---

## 2026-02-21T13:00:02Z: Coffee tokens domain modeling — minimal shape (Skiles)

**By:** Skiles (Implementation Lead)  
**Decision:** Implement coffee tokens with minimal immutable domain model:

**Value Object: CoffeeTokenPool**
```csharp
record CoffeeTokenPool
{
    public int Count { get; }
    
    public CoffeeTokenPool(int initialCount = 0)
    {
        if (initialCount < 0)
            throw new ArgumentException("Token count cannot be negative.");
        if (initialCount > 3)
            throw new ArgumentException("Token count cannot exceed 3.");
        Count = initialCount;
    }
    
    public CoffeeTokenPool Spend()
    {
        if (Count <= 0)
            throw new InvalidOperationException("No tokens available to spend.");
        return new CoffeeTokenPool(Count - 1);
    }
    
    public CoffeeTokenPool Earn()
        => new(Math.Min(Count + 1, 3));  // Cap at 3
    
    public bool CanSpend => Count > 0;
}
```

**Invariants:**
- Count ≥ 0 always.
- Count ≤ 3 always (cap enforced in constructor and Earn method).
- Spend() only succeeds if Count > 0; throws otherwise.
- Immutable; all mutations return new instance.

**GameState Ownership:**
- Add `TokenPool: CoffeeTokenPool` property (shared across players).
- Add `EarnCoffeeToken()` method → `TokenPool = TokenPool.Earn()`.
- Add `SpendCoffeeToken()` method → `TokenPool = TokenPool.Spend()`.
- Add `CanSpendToken` property (delegates to `TokenPool.CanSpend`).

**PlaceDieOnConcentrationCommand:**
- `UseTokenForAdjustment: bool` flag.
- `AdjustedValue: int?` property (optional, must be die ±1 when set).
- `Validate(GameState state)` method checks invariants (tokens available, adjusted value valid).
- UI-agnostic: doesn't prescribe Telegram rendering.

**ConcentrationModule.PlaceDieOnConcentration():**
- Validate die eligibility.
- If adjusted: check token availability, deduct token.
- Store die and final value (in pending storage for secret play).
- Earn token immediately.

**Key Design Points:**
- Immutability enables auditing, replay, no side-effect bugs.
- GameState-level placement: tokens are shared; aggregating at root is natural.
- Immediate earn-after-spend: matches board game flow.
- Command-driven adjustment: UI chooses adjusted value; domain validates.
- Explicit spend validation: guard clauses prevent overspend.

**Implementation Checklist:**
- [ ] CoffeeTokenPool value object with Spend() and Earn() methods.
- [ ] GameState.TokenPool property initialized at game start (0 tokens).
- [ ] PlaceDieOnConcentrationCommand with optional adjustment flag and validation.
- [ ] ConcentrationModule.PlaceDieOnConcentration() to handle spend/earn and adjustment.
- [ ] Tests: token increment, spend failures, boundary cases, immutability, secret storage.

**Rationale:** Minimal viable design. No over-engineering. Supports audit trails, replay, and deterministic testing.

---

## 2026-02-20T22:58:00Z: Concentration multi-token spend interpretation

**By:** Gianluigi Conti  
**Decision:** “Multiple tokens may be spent” means **multiple tokens may be spent on the same die placement**.
- **Cost:** `k = |adjustedValue - rolledValue|` tokens.
- **Effect:** the die is treated as `adjustedValue` (must remain within **1–6**, no wraparound).
- Example: rolled 4 → place as 6 costs 2 tokens; rolled 1 → place as 3 costs 2 tokens.

**Notes:** This supersedes earlier single-token-per-die assumptions in prior specs; the command shape should support spending `k` tokens, not just a boolean.

---

## 2026-02-21T13:47:00Z: PR #15 — Game init refactor follow-up (Skiles)

**Context:** Sully review flagged architectural issues in PR #15: duplicated state between `Game` and `GameState`, mutable `NextRoundCommand.Instance`, and a default-game factory (`Game.New()`) inside the aggregate.

**Decision:**
- **Single source of truth for per-round state:** `Game` now owns a single `GameState` instance and delegates current-player + unused-dice tracking to it (no duplicated fields in `Game`).
- **Immutable singleton command:** `NextRoundCommand.Instance` is now get-only and backed by a private constructor.
- **Factory removed from aggregate:** `Game.New()` was removed; aggregate construction is now explicit via `new Game(airport, altitude, modules)`.

**Rationale:** Keeps the aggregate focused on behavior and delegates mutable round state to a single internal component, while preventing global mutation of command singletons and avoiding opinionated defaults inside the domain.

---

## 2026-02-21T13:47:00Z: PR #15 — Re-review (Sully)

**Decision:** ACCEPT (ready to merge).

**Notes:**
- Prior concerns are resolved:
  - **Single source of truth:** `Game` delegates per-round mutable state to `GameState` (current player + unused dice).
  - **Command immutability:** `NextRoundCommand.Instance` is get-only with private ctor.
  - **API hygiene:** removed `Game.New()` factory from the aggregate.

**Follow-up (non-blocking):**
- Once `ExecuteCommand` / placement behavior exists, update tests to avoid reflection hacks used to clear dice.

**GitHub:**
- Formal "Approve" review was not possible because the active GitHub identity is the PR author; left an architectural "LGTM" comment instead.

---

## 2026-02-21T10:21:03Z: Issue #31 specification — base game modules and landing criteria (Tenerife)

**Decision:** Comprehensive specification finalized for all 7 mandatory base-game modules and landing win/loss criteria.

**Specification:** `.squad/decisions/inbox/tenerife-issue31-spec.md` (500+ lines with detailed module specs, invariants, edge cases, verification checklist).

**Modules Specified:**
1. Axis Position — Balance check ([-2, +2]), immediate loss if out-of-bounds
2. Engines — Thrust accumulation (≥9 at landing), Approach advance
3. Brakes — Sequential switches (2 → 4 → 6), landing check requires all 3 activated AND > LastSpeed
4. Flaps — 4 switches with flexible die ranges, cumulative threshold shifts
5. Landing Gear — 3 switches with die-range mapping, idempotent re-activation
6. Radio — Clear planes from Approach track (up to 2 orange, 1 blue per round)
7. Concentration — Coffee token pool (0–3, shared, persistent), multi-token spend (cost = |adjusted - rolled|)

**Landing Criteria (all 6 must pass for WIN):**
1. Axis balanced: AxisPosition ∈ [-2, +2]
2. Engines thrust: LastSpeed ≥ 9
3. Brakes descent: BrakesValue == 3 AND BrakesValue > LastSpeed
4. Flaps deployed: FlapsValue == 4
5. Landing Gear deployed: LandingGearValue == 3
6. Approach clear: All planes cleared from track

**Loss Conditions (pre-landing, immediate):**
1. Axis imbalance: AxisPosition < -2 OR > +2 (checked during resolution, not at landing)
2. Altitude exhausted: Altitude reaches 0 with planes still on Approach (checked on final round entry)
3. Landing failure: Any criterion fails at landing check

**Module Resolution Order:** Axis → Engines → Brakes → Flaps → Gear → Radio → Concentration (fixed sequence).

**Key Clarifications & Decisions:**
- Brakes landing criterion reconciliation: BrakesValue == 3 (all switches) AND BrakesValue > LastSpeed
- Engines final round: No Approach advance during final round (altitude 0)
- Landing Gear idempotence: Duplicate switch placement silently ignored (graceful no-op)
- Concentration token spend: Multi-token spend locked (cost = |adjusted - rolled|, bounded to [1, 6], no wraparound)
- Token pool scoping: Shared across both players, capacity 3, persistent across rounds
- Token earn: +1 per Concentration die placement (capped at 3)
- Token spend + Concentration placement: Net change = 1 - k (spend cost k, earn +1)

**Verification Checklist:** All 7 modules verified against implementation; ~85% alignment. Minor clarifications captured in spec.

**Rationale:** Comprehensive rules documentation unblocks implementation work and validates existing code against official Sky Team rules.

---

## 2026-02-21T10:21:03Z: Issue #31 test findings and spec mismatches (Aloha)

**By:** Aloha (QA)  
**Decision:** Identified test coverage for Issue #31 modules and flagged spec mismatches requiring reconciliation.

**Test Coverage Added:**
- **Axis:** Out-of-bounds resolution throws immediately when < -2 or > +2; explicit boundary tests for positions -2 and +2
- **Landing:** 1 passing WIN scenario + 1 focused LOSS scenario per landing criterion (6 loss scenarios total)
  - Axis out-of-bounds at landing
  - Engines thrust below 9
  - Brakes not fully deployed
  - Flaps not fully deployed
  - Landing Gear not fully deployed
  - Approach not fully cleared
- **Concentration / Coffee tokens:** Token pool bounds (0–3), Earn/Spend transitions (including k=1, k=2), die value bounds (1–6)

**Spec Mismatches Identified:**

1. **Brakes Landing Criterion Inconsistency:**
   - Spec states: `BrakesValue == 3` AND `BrakesValue > LastSpeed`
   - Problem: BrakesValue is switch count (0–3); if LastSpeed ≥ 9, condition `BrakesValue > LastSpeed` is impossible (3 ≯ 9)
   - Current code: Treats BrakesValue as last activated required value (2/4/6) and checks `BrakesValue ≥ 6` without speed comparison
   - **Recommendation:** Clarify whether intended landing check is "all switches deployed" only, or a different brakes magnitude meant to be compared to speed

2. **Coffee-Token Die Adjustment Implementation:**
   - `Game.GetAvailableCommands()` surfaces token-adjusted command IDs like `Axis.AssignBlue:1>3` when tokens available
   - Cost: `k = |effective - rolled|` tokens
   - `Game.ExecuteCommand()` spends required tokens, consumes die, assigns effective value (bounded to 1–6)
   - Tests cover command surfacing, spend behavior, pool bounds, die-value bounds
   - Design validated; awaiting UX surface confirmation (Telegram button rendering)

**Test Framework:** xUnit + FluentAssertions, AAA pattern.

**Rationale:** Test harness validates spec compliance and uncovers implementation gaps. Brakes criterion requires reconciliation before finalizing tests.

---

## 2026-02-21T09:36:58Z: User directive — placement undo policy (Gianluigi Conti)

**By:** Gianluigi Conti (via Copilot)  
**Decision:** Dice placements are secret (private submissions), but ALL placements are public in group chat. Allow undo/cancel of last placement only if the other player has not played yet this round.

**Rationale:** UX clarification — captured for team memory. Enables secret play while preserving transparency on outcomes.

---

## 2026-02-21T00:07:39Z: Copilot coordinator directives

**By:** User (via Copilot)  
**Decision:** Team Mode coordinator governance.  
**Details:**
- Operate as Squad Coordinator in Team Mode; dispatch sub-agents per routing rules.
- Always include an agent’s charter in the spawn prompt; use at most 1–2 agents per request.
- After agent work, dispatch Scribe to log decisions and update `.squad/decisions.md`.
- Maintain a Ralph-style loop (scan issues/PRs, route, execute ready work) until task complete.
- Session start checklist: verify git config; ensure `.squad/team.md` exists; read `.squad/team.md`, `.squad/routing.md`, `.squad/casting/registry.json`.

**Rationale:** User directive.

---

## 2026-02-21T08:17:30Z: User directive — keep Telegram communication logic separated

**By:** Gianluigi Conti (via Copilot)  
**Decision:** Keep Telegram communication logic separated from the game application logic and presentation; UX must work in a 2-people Telegram group chat.  
**Rationale:** User request — captured for team memory.

---

## 2026-02-21T08:20:00Z: Telegram bot layered architecture + MVP backlog (Sully)

**By:** Sully (Architect)  
**Decision:** Proposed 5-layer architecture for clean separation of concerns:

**Layers (dependency direction: top → down):**
1. **Domain (`SkyTeam.Domain`)** — Pure DDD: Game aggregate, GameState, modules, commands, invariants. No Telegram types, no I/O, no formatting.
2. **Application (`SkyTeam.Application`)** — Orchestrates use-cases and multi-user workflows. Exposes ports: `IGameSessionRepository`, `IClock`, `IDiceRoller`, `IChatGateway`.
3. **Presentation (`SkyTeam.Presentation.Chat`)** — Converts app/domain state into transport-agnostic chat UI models: `ChatMessage`, `ChatKeyboard`, `ChatUiEvent`. No Telegram SDK references.
4. **Telegram Adapter (`SkyTeam.Adapters.Telegram`)** — Translates Telegram Updates → application commands; `ChatMessage/ChatKeyboard` → Telegram `SendMessage/EditMessageText/InlineKeyboardMarkup`.
5. **Host (`SkyTeam.Bot.Telegram` executable)** — Wiring only: DI, config, logging, token, polling/webhook.

**Recommended Project Layout:**
- `SkyTeam.Domain` (exists)
- `SkyTeam.Application` (new class library)
- `SkyTeam.Presentation.Chat` (new class library)
- `SkyTeam.Adapters.Telegram` (new class library)
- `SkyTeam.Bot.Telegram` (new console app)

**Core Application Contract (Ports):**
- `IChatGateway`: SendToGroup, SendToUser, EditGroupMessage
- `IGameSessionRepository`: GetByGroup, Upsert
- `IDiceRoller`: RollDie, RollHand

**MVP Backlog (Epics A–G):**
- **Epic A:** Solution & layering foundation (Slices A1–A3)
- **Epic B:** Telegram transport baseline (Slices B1–B3)
- **Epic C:** Group session lifecycle (Slices C1–C3)
- **Epic D:** Turn/round interaction loop (Slices D1–D3)
- **Epic E:** Domain completion to Base Game (Slices E1–E3)
- **Epic F:** Presentation: "Cockpit" + reveal output (Slices F1–F3)
- **Epic G:** MVP hardening (Slices G1–G3)

**Interview Questions for User (8 total):**
1. DM onboarding required (each player must `/start` the bot privately)?
2. Strict alternation (one placement at a time) vs. submit-all-then-resolve?
3. Preferred UX: buttons/inline keyboards, or typed commands, or both?
4. Token spends/adjustments announced immediately (transparent) or only at round end?
5. Persistence across bot restarts required for MVP?
6. Undo/cancel policy: "undo last placement" vs. "only cancel round"?
7. With 2+ humans in group, enforce exactly 2 seated players + spectators?
8. Must-have non-base-game UX: pin cockpit, auto-advance, reminders/timeouts?

**Rationale:** Layered architecture enforces compile-time separation. Epic structure enables parallel team work. Interview questions clarify UX tradeoffs before locking implementation.

---

## 2026-02-21T08:20:15Z: Telegram bot project isolation + solution wiring (Skiles)

**By:** Skiles (Implementation Lead)  
**Decision:** Created `SkyTeam.TelegramBot` console project and integrated into solution (`.slnx`).

**Operational Details:**
- Uses `Telegram.Bot` NuGet package (version managed via `Directory.Packages.props`).
- Bot token sourced from `TELEGRAM_BOT_TOKEN` environment variable.
- Implements long-polling via `StartReceiving` with minimal `/start` handler.
- Project references `SkyTeam.Domain` directly; future adapter/application/presentation layers will be added per Sully's architecture.

**Architecture Alignment:**
- Maintains clean separation: bot project does not leak Telegram SDK into domain.
- Ready for Sully's 5-layer model once foundation epics (A1–A3) are complete.

**Rationale:** Isolates Telegram concerns from domain; foundation for layered architecture. Minimal bootstrap code supports immediate testing.

---

## 2026-02-21T08:20:30Z: Telegram UX specification for Sky Team (Tenerife)

**By:** Tenerife (Rules Expert)  
**Decision:** Comprehensive UX specification for 2-player cooperative Sky Team gameplay in Telegram group chat.

**Core Principles:**
1. **Secret Assignments:** Dice placements private (via inline keyboards); bot reveals outcomes at resolution.
2. **Transparency on Non-Secrets:** Token counts, altitude, module values, turn status broadcast publicly.
3. **Visual Clarity:** Emoji, prefixes, ASCII tables for easy state scanning.
4. **Minimal Commands:** Most interactions via buttons; `/` commands only for setup/queries.
5. **Turn Discipline:** Alternating player turns; 60-second timeout (configurable); bot enforces ordering.

**Main Commands:**
- Setup: `/start_game`, `/join`, `/rules`, `/state`
- In-Game: `/undo_placement`, `/surrender`

**Game Flow (Round-by-Round):**
- Phase 1: Roll Dice (bot action)
- Phase 2: Assign Dice (player action — secret)
- Phase 3: Reveal & Resolve (bot action)
- Phase 4: Altitude Descent & Board Update (bot action)
- Phase 5: Win/Loss Check (bot action)

**Token Mechanics (Button-Based):**
- Display token-cost options: `[Axis]`, `[Axis] 💰2` (if pool > 0 and affordable)
- Spend declaration announced publicly (not secret)
- Gain: +1 token per Concentration placement (capped at 3)

**Board State Display:**
- Full `/state` output: altitude bar, modules, tokens, turn status, next action
- Inline round summary: round number, assignment status, resolution status

**7 Concrete Example Transcripts:**
1. Simple round, no tokens, both cooperate
2. Token spend (multi-token adjustment)
3. Reroll declaration
4. Landing & victory
5. Collision loss
6. Axis imbalance loss at landing
7. Concentration token spend + earn (net zero)

**Edge Cases & Timing:**
- Token pool 0 & spend attempt → buttons disable options
- No reroll available → prevent button click
- Concentration placed + die pre-adjusted → token spent → token earned → net 0
- Pilot bad roll (all 1s) → Copilot sees "Pilot thinking…" up to 120 sec
- Radio clears all planes → "Approach track cleared! ✅"
- Altitude at 6000 ft round 7 → no landing (only at 0)

**Implementation Hooks:**
- Bot: Ephemeral keyboard rendering, session state management, broadcast & reveal
- Domain: Accept `PlaceDieCommand`, module resolution order, landing check logic

**Rationale:** Comprehensive spec ensures clarity + secrecy. Concrete examples enable deterministic testing. Token UX is button-driven, not command-based (minimum friction). Rule compliance is validated in every decision branch.

---

## 2026-02-21T09:08:13Z: Group-chat-first UX for Telegram (Gianluigi Conti)

**By:** Gianluigi Conti  
**Decision:** Keep UX group-chat-first; start the game in the group chat, and use private messages only when needed for secret dice/placements (after onboarding).  
**Rationale:** User clarification — captured for team memory.

---

## 2026-02-21T09:09:14Z: Round interaction model — strict alternation (Gianluigi Conti)

**By:** Gianluigi Conti  
**Decision:** Strict alternation one die placement at a time (Pilot places 1 die, then Copilot, repeat).  
**Rationale:** User choice — captured for team memory.

---

## 2026-02-21T09:10:08Z: UX interaction preference — inline buttons primary (Gianluigi Conti)

**By:** Gianluigi Conti  
**Decision:** Primarily inline buttons/menus (typed commands optional, not required for parity).  
**Rationale:** User choice — captured for team memory.

---

## 2026-02-21T09:10:26Z: MVP persistence — in-memory session store (Gianluigi Conti)

**By:** Gianluigi Conti  
**Decision:** In-memory session store is acceptable for MVP (sessions reset on bot restart).  
**Rationale:** User choice — captured for team memory.

---

## 2026-02-21T09:11:20Z: Group spectators and 2-player enforcement (Gianluigi Conti)

**By:** Gianluigi Conti  
**Decision:** Group chats may have spectators, but the game enforces exactly 2 seated players (Pilot + Copilot).  
**Rationale:** User choice — captured for team memory.

---

## 2026-02-21T09:12:32Z: Undo placement — pre-other-player-action (Gianluigi Conti)

**By:** Gianluigi Conti  
**Decision:** Allow undo last placement, but only before the other player takes an action.  
**Rationale:** User choice — captured for team memory.

---

## 2026-02-21T13:00:05Z: Lobby slice review — MVP foundation acceptable (Sully)

**By:** Sully (Architect)  
**Decision:** Lobby slice (commit b704cbd) is acceptable MVP foundation.

**Key Assessments:**
- **Architecture:** Application-layer lobby store is clean — no Telegram SDK types leak into `SkyTeam.Application`.
- **In-Memory Store:** Concrete `InMemoryGroupLobbyStore` in `SkyTeam.Application.Lobby` acceptable for MVP (user accepted in-memory persistence).
- **API Hygiene:** Lobby state transport-agnostic; group chat IDs treated as primitives in application layer.

**Follow-ups (non-blocking, recommended before next slices):**
1. Introduce application port (`IGroupLobbyStore` / `IGroupLobbyRepository`) and move in-memory implementation to Host/Infrastructure for persistence swaps without application rewrites.
2. Rename `GroupChatId` → `GroupId` (and similar) to eliminate Telegram semantics from application public API.
3. Move `RenderLobby` / command parsing out of `Program.cs` once Presentation.Chat and Adapters.Telegram projects land (per 5-layer architecture).

**Rationale:** Lobby slice unblocks team; follow-up refactors keep codebase maintainable as scope expands.

---

## 2026-02-21T13:00:05Z: Application test layer — InMemoryGroupLobbyStore suite (Aloha)

**By:** Aloha (QA)  
**Decision:** Created `SkyTeam.Application.Tests` xUnit project (separate from `SkyTeam.Domain.Tests`) to isolate application-layer behavior tests.

**Rationale:** Lobby store is an application concern (in-memory multi-user coordination by group chat id), not domain logic. Dedicated test assembly improves signal and preserves layering.

**Test Suite Coverage:**
- **CreateNew:** Lobby created successfully; already-exists case reports current state without reset.
- **Join:** Pilot seated, Copilot seated, full-lobby rejection, already-seated no-op, no-lobby error.
- **GetSnapshot:** Returns null if no lobby; returns current state if lobby exists.

**Implementation:** xUnit v3 with FluentAssertions; follows AAA (Arrange, Act, Assert) pattern.

---

## 2026-02-21T13:00:05Z: Lobby slice — /sky new is non-destructive (Skiles)

**By:** Skiles (Implementation Lead)  
**Decision:** `/sky new` command creates a lobby **only if one does not already exist**.

**Behavior:**
- If no lobby exists → Create lobby, seat caller as Pilot, report success.
- If lobby already exists → Report current state (players seated, status) and prevent reset.

**Rationale:** Avoid surprising seat resets in active groups; explicit `/sky reset` (future feature) supports advanced scenarios. Non-destructive `/sky new` is MVP-safe.

---

## 2026-02-21T18:06:26Z: PR#37 Execute wiring — token pool + landing checks (Sully)

**By:** Sully (Architect)  
**Decision:** For PR #37 / issue #31, keep the **coffee token pool owned by `ConcentrationModule`** (as the authoritative source of token count).

- `Game.GetAvailableCommands()` now passes `ConcentrationModule.TokenPool` (fallback: 0) into module command generation.
- All token-adjusted `GameCommand.Execute(Game)` implementations spend tokens via `game.SpendCoffeeTokens(k)`, and `Game.SpendCoffeeTokens(k)` delegates to `ConcentrationModule.SpendCoffeeTokens(k)`.

Also, landing win/loss checks are evaluated as independent criteria (Engines ≥ 9, Brakes ≥ 6, Flaps ≥ 4, Landing Gear ≥ 3, Axis within [-2,2], Approach cleared), and `NextRound()` no longer enforces an additional "mandatory placements" loss gate beyond the existing command-availability rules.

**Rationale:** This keeps the PR37 command-execution wiring minimal and consistent, matches the current module/test contract, and avoids catching/rewrapping non-rule exceptions (only rule losses use `GameRuleLossException`).

---

## 2026-02-21T18:06:26Z: Loss Condition Semantics & Rule Validation Checklist (Tenerife)

**By:** Tenerife (Rules Expert)  
**Decision:** Codify explicit loss conditions (must throw `GameRuleLossException`) vs. invalid moves (normal rejection via command validation) based on current domain implementation and M1 rules spec.

**Win Condition (All 6 Must Pass at Landing):**
- Axis Position: [-2, 2] (balanced)
- Engines: Sum ≥ 9
- Brakes: Value ≥ 6
- Flaps: Value ≥ 4
- Landing Gear: All 3 switches activated (value = 3)
- Approach Track: All plane tokens cleared

**Explicit Loss Conditions (→ GameRuleLossException):**
1. Axis Out of Balance at Landing: `AxisPosition < -2 OR AxisPosition > 2`
2. Speed Too High at Landing: `BrakesValue < EnginesValue`
3. Approach Track Collision: ANY plane token remains on Approach track
4. Altitude Exhausted: No more segments to descend without having reached landing
5. Mid-Round Axis Invariant: After both Axis dice placed, result out of bounds (axis ≥ ±3)

**Invalid Moves (Normal Rejection, No Exception):**
- Brakes/Flaps sequence violations, duplicate placements, concentration exhaustion, radio orange limit, die availability issues — all preventable via command availability and UI validation.

**Bugs Noted:**
- Axis landing check currently too strict (checks == 0, should check ∈[-2,2]).
- Speed comparison uses > not ≥; verify if intended.
- Altitude exhaustion not explicitly implemented; needs implementation after altitude redesign.
- Reroll token mechanics not visible in current implementation.

**Rationale:** Comprehensive checklist unblocks rule validation and test coverage. Separates true loss conditions from validation errors, enabling proper exception handling and game-state management.

---
---

## 2026-02-21T18:14:17Z: Epic #26 triage — P0 path to playable Telegram MVP (Sully)

**Decision:** The smallest P0 sequence to reach a *playable* Sky Team MVP in a Telegram group chat is:

1. **Start game from ready lobby** (Issue #27)
2. **Authoritative application round/turn state + per-player secret dice hand** (Issue #28)
3. **DM secret dice roll/hand to each seated player** (Issue #29)
4. **Public placements in group + strict alternation enforcement** (Issue #30)
5. **Resolve round after 8 placements + broadcast updated cockpit/state** (Issue #32)
6. **Domain completion to base game + landing win/loss criteria** (Issue #31) — can progress in parallel, but required before calling the MVP "playable".

**Dependency note:** #27/#29/#30/#32 depend on #28 for a single source of truth for turn order and secret hands; #31 is the rule-completeness gate.

**Rationale:** This keeps Telegram wiring incremental while preserving DDD boundaries (Domain pure; Application owns multi-user orchestration; Telegram only adapts transport/UI).

---

## 2026-02-21T18:14:17Z: Issue #28 — Application round/turn state + secret hands (Skiles)

**Context:** We need application-layer orchestration state for Telegram UX where each player sees only their rolled dice hand, while the group chat enforces strict alternation and resolves after 8 placements. Domain types (`Player`, `BlueDie`, `OrangeDie`, `GameState`) are internal and must remain UI-agnostic; therefore Telegram and application orchestration cannot depend on them.

**Decision:** Implement a small, pure application state model in `SkyTeam.Application.Round`:

- `PlayerSeat` (`Pilot` / `Copilot`) + `Other()` helper for strict alternation.
- `DieValue` value object (1–6).
- `SecretDiceHand` (exactly 4 dice, per-player, tracks used/unused dice by index).
- `RoundTurnState`:
  - Holds `RoundNumber`, `StartingPlayer`, `CurrentPlayer`, hands, `Placements` list, and a `RoundPhase`.
  - Enforces strict alternation in `RegisterPlacement()`.
  - Transitions to `ReadyToResolve` automatically after 8 total placements.
  - Provides `UndoLastPlacement()` guard-railed by the existing UX policy: only the player who placed last can undo, and only before the other player plays.

**Rationale:**
- Keeps Telegram SDK types out of application and domain.
- Deterministic, immutable-ish state makes downstream testing trivial (Aloha can assert state transitions without any randomness).
- Provides explicit guardrails for upcoming issues: DM roll (#29) uses the hands; public placement (#30) uses the placement log; resolve-after-8 (#32) uses the phase transition.

**Follow-ups:**
- Wire this state into the session repository/use-cases once the application service layer is introduced.
- Aloha: add deterministic tests for placement alternation, max 8 placements, and undo gating.


---

## 2026-02-21T19:09:17Z: PR #38 sync with master

**By:** Gimli (Code Implementer)  
**Decision:** Merged `origin/master` into `squad/31-domain-tests` and resolved conflicts by taking master implementations for impacted domain modules/tests.  
**Source:** Merged from `.squad\decisions\inbox\gimli-pr38-sync.md`

<details>
<summary>Source (verbatim)</summary>

# Gimli: PR #38 sync with master

Date: 2026-02-21

Decision:
- To unblock PR #38 after PR #37, I merged `origin/master` into `squad/31-domain-tests` and resolved conflicts by taking the `origin/master` implementations for the affected Domain modules and related tests.

Rationale:
- PR #37 introduced a newer command execution pattern (commands own `Execute(Game)` and carry module references); keeping master’s versions minimizes risk and preserves the current architecture while retaining PR #38’s non-conflicting changes.

</details>

---

## 2026-02-21T19:09:18Z: Secret dice roll via DM (Issue #29)

**By:** Skiles (Implementation Lead)  
**Decision:** Add a `/sky roll` group command; on roll, generate dice for each seat and DM them to Pilot/Copilot; if DM fails, notify the group without revealing dice values.  
**Source:** Merged from `.squad\decisions\inbox\skiles-29-secret-dice.md`

<details>
<summary>Source (verbatim)</summary>

# Decision: Secret dice roll via DM (Issue #29)

## Context
We need to keep dice values secret between seated players while still rolling from the group chat.

## Decision
- Add a `/sky roll` group command.
- On roll, generate 4 dice values (1–6) for each seat and DM them to the seated Pilot/Copilot.
- If a DM fails (player has not `/start`ed the bot privately), notify the group without revealing any dice values.

## Rationale
This keeps the change minimal and testable (pure application dice-roll logic), while matching Telegram constraints around private messaging.

</details>

---

## 2026-02-21T19:09:19Z: Issue #31 specification — base game modules & landing win/loss (full text)

**By:** Tenerife (Rules Expert)  
**Decision:** Lock and document the full base-game module behaviors and landing win/loss criteria for Issue #31 (see verbatim spec below).  
**Source:** Merged from `.squad\decisions\inbox\tenerife-issue31-spec.md`

<details>
<summary>Source (verbatim)</summary>

# Issue #31 Spec: Base Game Modules & Landing Win/Loss

**Date:** 2026-02-21  
**Author:** Tenerife (Rules Expert)  
**Status:** Final Specification  
**Target Issue:** #31 (Domain: complete base modules + landing win/loss criteria)

---

## Overview

This specification codifies the **7 mandatory base-game modules** and **landing win/loss criteria** for Sky Team MVP. All modules have been implemented in the codebase; this spec validates correctness against official rules and documents behavior, invariants, and edge cases.

**Reference:** Official Sky Team rules at https://www.geekyhobbies.com/sky-team-rules/

---

## 1. Module Specifications

### 1.1 Axis Position Module

**Purpose:** Maintain plane balance along the roll axis. Imbalance triggers immediate loss.

**State:**
- `_axisPosition: int` — Current position on [-∞, +∞] axis (starts at 0).
- `_blueDie: BlueDie?` — Pilot die (if placed).
- `_orangeDie: OrangeDie?` — Copilot die (if placed).

**Placement Rules:**
- **Pilot (Blue):** Can place blue die if no blue die placed this round.
- **Copilot (Orange):** Can place orange die if no orange die placed this round.

**Resolution Timing:**
- **Trigger:** When both dice are placed (one per player).
- **Order:** Immediate (as soon as second die is placed).
- **Effect:** 
  - If `blueValue == orangeValue`: axis position unchanged.
  - If `blueValue > orangeValue`: axis shifts right by difference: `newPosition = currentPosition + (blueValue - orangeValue)`.
  - If `orangeValue > blueValue`: axis shifts left by difference: `newPosition = currentPosition - (orangeValue - blueValue)`.

**Loss Condition (Immediate):**
- If `newAxisPosition` exceeds bounds `[-2, +2]` (i.e., `< -2` or `> +2`), the **game is lost immediately**. Axis imbalance is a critical failure.
  - Bounds: `-3` or below = loss; `+3` or above = loss.
  - Valid range: `[-2, -1, 0, +1, +2]`.

**Round Reset:**
- Both `_blueDie` and `_orangeDie` are cleared at start of each round.

**No Wraparound:**
- Axis position accumulates continuously. No circular motion.

---

### 1.2 Engines Module

**Purpose:** Build thrust (speed) and advance on the Approach track to clear landing path.

**State:**
- `_blueDie: BlueDie?` — Pilot die (if placed).
- `_orangeDie: OrangeDie?` — Copilot die (if placed).
- `LastSpeed: int?` — Sum of both dice (used for landing check against Brakes).

**Placement Rules:**
- **Pilot (Blue):** Can place blue die if no blue die placed this round.
- **Copilot (Orange):** Can place orange die if no orange die placed this round.

**Resolution Timing:**
- **Trigger:** When both dice are placed.
- **Order:** Immediate.
- **Speed Calculation:** `speed = blueValue + orangeValue` (sum, range 2–12).
- **Approach Advance (non-final rounds only):**
  - If `speed < Airport.BlueAerodynamicsThreshold` (default 4): advance 0 segments.
  - If `4 ≤ speed ≤ Airport.OrangeAerodynamicsThreshold` (default 8): advance 1 segment.
  - If `speed > 8`: advance 2 segments.
- **Final Round Behavior:** During final round (altitude at 0), Engines does NOT advance Approach (already at final stage).

**Landing Criteria (checked at altitude 0):**
- **Engines Value:** Must achieve `LastSpeed ≥ 9` by landing to pass landing check.
- **Speed vs. Brakes:** Landing check validates `BrakesValue > LastSpeed` (brakes must exceed speed for safe deceleration).

**Cumulative:**
- `LastSpeed` is recalculated each round when both dice are placed.
- Approach position is cumulative across the game (never resets).

**Round Reset:**
- Both `_blueDie` and `_orangeDie` are cleared at start of each round.
- `LastSpeed` is cleared but then recalculated if both dice are placed in current round.

---

### 1.3 Brakes Module

**Purpose:** Activate descent control in sequential 3-step progression (2 → 4 → 6).

**State:**
- `_nextRequiredIndex: int` — Index of next switch to activate (0, 1, or 2).
- `BrakesValue: int` — Count of activated switches (0–3).

**Placement Rules:**
- **Pilot (Blue):** Can only place blue die.
- **Copilot:** Cannot place (module is blue-only).
- **Sequential Requirement:** Switches **must** be activated in order: 2 → 4 → 6.
- **Availability:** Can accept die **only if** current switch index < 3 (i.e., not all 3 switches activated).

**Resolution Timing:**
- **Trigger:** When die with exact required value is placed (2, then 4, then 6).
- **Order:** Immediate.
- **Effect:** Increment `_nextRequiredIndex` and `BrakesValue` by 1.

**Landing Criteria (checked at altitude 0):**
- **Brakes Value:** Must achieve `BrakesValue ≥ 6` by landing (all 3 switches activated: 3 points).
- Wait—official rules state ≥6 but the module tops out at 3 values. **Clarification:** The requirement is actually "all 3 switches activated" = BrakesValue 3, which unlocks speed check. The spec in decisions.md says ≥6 for Brakes landing criteria, but the module itself only has 3 switches. **Resolving:** The module value represents switch count (0–3). For landing, we check `BrakesValue >= 3` (all switches deployed). The ≥6 in decisions.md may refer to the combined Brakes+Engines value or is a transcription error. **Locked interpretation:** BrakesValue must be **exactly 3** (all switches activated) for landing pass.

**Cumulative:**
- BrakesValue accumulates throughout the game (never resets).
- Index never resets; once a switch is activated, it stays activated.

**Round Reset:**
- No reset—state persists across rounds.

---

### 1.4 Flaps Module

**Purpose:** Deploy flaps in 4-step progression with flexible die values per switch.

**State:**
- `_nextRequiredIndex: int` — Index of next switch (0–3).
- `FlapsValue: int` — Count of activated switches (0–4).
- Allowed values per switch: `[[1,2], [2,3], [4,5], [5,6]]`.

**Placement Rules:**
- **Pilot:** Cannot place (module is orange-only).
- **Copilot (Orange):** Can place die with allowed value for current switch.
- **Availability:** Can accept die **only if** current switch index < 4 (not all switches activated).

**Resolution Timing:**
- **Trigger:** When die with value in allowed set is placed.
- **Order:** Immediate.
- **Effect:** 
  - Increment `_nextRequiredIndex` and `FlapsValue` by 1.
  - Trigger `Airport.MoveOrangeAerodynamicsRight()` (shifts aerodynamics threshold).

**Landing Criteria (checked at altitude 0):**
- **Flaps Value:** Must achieve `FlapsValue == 4` (all switches deployed).

**Cumulative:**
- FlapsValue accumulates throughout game (never resets).

**Side Effect:**
- Each flap activation advances the orange aerodynamics threshold on the Airport track (affects speed thresholds for Engines module).

**Round Reset:**
- No reset—state persists.

---

### 1.5 Landing Gear Module

**Purpose:** Deploy landing gear in 3 stages via switches mapped to die ranges.

**State:**
- `_isSwitchActivated: bool[3]` — Tracks which of 3 switches are deployed (index → die range mapping).
- `LandingGearValue: int` — Count of activated switches (0–3).
- Mapping:
  - Switch 0: die values {1, 2}.
  - Switch 1: die values {3, 4}.
  - Switch 2: die values {5, 6}.

**Placement Rules:**
- **Pilot (Blue):** Can place blue die if switch for that die value is not yet activated.
- **Copilot:** Cannot place (module is blue-only).
- **Availability:** Can accept die **only if** at least one switch remains inactive.

**Resolution Timing:**
- **Trigger:** When die with valid unmapped switch is placed.
- **Order:** Immediate.
- **Effect:**
  - Mark the switch as activated.
  - Increment `LandingGearValue` by 1.
  - Trigger `Airport.MoveBlueAerodynamicsRight()` (shifts aerodynamics threshold).

**Edge Case:**
- If player places die whose switch is already activated, placement is **ignored** (no-op). Module allows this gracefully without error.

**Landing Criteria (checked at altitude 0):**
- **Landing Gear Value:** Must achieve `LandingGearValue == 3` (all switches deployed).

**Cumulative:**
- LandingGearValue accumulates throughout game (never resets).

**Side Effect:**
- Each gear activation advances the blue aerodynamics threshold on the Airport track (affects speed thresholds for Engines module).

**Round Reset:**
- No reset—switch state persists.

---

### 1.6 Radio Module

**Purpose:** Clear planes from the Approach track at specific positions determined by die values.

**State:**
- `_blueDie: BlueDie?` — Pilot die (if placed).
- `_orangeDice: List<OrangeDie>` — Copilot dice (up to 2 per round).

**Placement Rules:**
- **Pilot (Blue):** Can place one blue die if no blue die placed this round.
- **Copilot (Orange):** Can place up to 2 orange dice per round (different dice).
- **Availability:** No die can be placed twice in same round.

**Resolution Timing:**
- **Trigger:** As each die is placed.
- **Order:** Immediate per-die.
- **Effect:**
  - For each die, calculate offset: `dieValue - 1` (range 0–5).
  - Call `Airport.TryRemovePlaneTokenAtOffset(offset)` to remove one plane token at that segment.
  - If no plane token exists at offset, removal silently succeeds (no error).

**Cumulative:**
- Approach track position is cumulative; cleared planes stay cleared.

**Landing Criteria (checked at altitude 0):**
- **Approach Clear:** All planes must be cleared from Approach track (all segments have 0 plane tokens). If any planes remain, landing fails.

**Round Reset:**
- Both `_blueDie` and `_orangeDice` are cleared at start of each round.

**No Wraparound:**
- Die offsets map directly to segment indices (0–5). No circular motion on Approach track.

---

### 1.7 Concentration Module

**Purpose:** Invest dice as flexible wildcards to gain coffee token pool for re-rolling or die adjustments.

**State:**
- `_slotsUsed: int` — Count of dice placed this round (0–2).
- `_tokenPool: CoffeeTokenPool` — Shared pool of coffee tokens (capacity 0–3).

**Token Pool Value Object (`CoffeeTokenPool`):**
- `Count: int` — Number of tokens (0–3, capped).
- `CanSpend: bool` — True if Count > 0.
- `Spend(): CoffeeTokenPool` — Returns new pool with count − 1; throws if count ≤ 0.
- `Earn(): CoffeeTokenPool` — Returns new pool with min(count + 1, 3).

**Placement Rules:**
- **Both Players:** Pilot (blue) and Copilot (orange) can each place up to 1 die per round on Concentration (max 2 slots).
- **Availability:** Can place if `_slotsUsed < 2`.

**Resolution Timing:**
- **Trigger:** When die is placed on Concentration.
- **Order:** Immediate.
- **Effect:**
  - Increment `_slotsUsed` by 1.
  - Earn +1 token: `_tokenPool = _tokenPool.Earn()` (capped at 3).

**Token Spend (Optional, Pre-Placement):**
- **When:** Before placing die on any module (not just Concentration).
- **Cost:** `k = |adjustedValue - rolledValue|` tokens.
- **Adjustment:** Die value can be shifted to an adjacent value within [1, 6]:
  - Rolled 1 → can become {1, 2} (spending 0 or 1 token).
  - Rolled 2–5 → can become {die−1, die, die+1} (spending 0, 1, or 2 tokens depending on target).
  - Rolled 6 → can become {5, 6} (spending 0 or 1 token).
- **No Wraparound:** Cannot wrap 1→0 or 6→7.
- **Pool Enforcement:** Can only spend if pool has sufficient tokens.

**Special Case: Spend + Place on Concentration:**
- If a die is adjusted (tokens spent) and then placed on Concentration:
  - Tokens spent: pool decreases by k.
  - Die placed on Concentration: tokens earned: pool increases by 1.
  - **Net change:** If k=1, net change is 0. If k=2, net change is −1.
  - This is allowed and intended (players trade tokens for die flexibility).

**Landing Criteria:**
- **Tokens:** Unused tokens at landing have **no effect** on win/loss. Tokens are purely round-to-round resource.

**Cumulative:**
- Token pool is shared and persistent throughout the game.

**Round Reset:**
- `_slotsUsed` is reset to 0 at start of each round.
- `_tokenPool` persists across rounds.

**Edge Cases:**
- **Pool at 0:** Cannot spend (buttons/UI should gray out unavailable options).
- **Pool at 3:** Placement still succeeds; no token earned (cap enforced in `Earn()`).
- **Multiple spend options:** Player may choose to spend 0, 1, or 2 tokens (if available) on same die; each choice presented as distinct button.

---

## 2. Landing Win/Loss Criteria

**Trigger:** When altitude reaches 0 (final round completion).

**All 6 criteria must pass for WIN; failure of any one = LOSS.**

### 2.1 Landing Criteria (MUST ALL PASS)

1. **Axis Balance:** `AxisPosition ∈ [-2, +2]` (i.e., within bounds).
   - Loss condition: Out-of-bounds axis is checked **immediately** when Axis dice are both placed, not at landing. If out-of-bounds, game is lost before landing is even checked.
   - At landing, check that axis has stayed in bounds throughout (should be implicit if no immediate loss occurred).

2. **Engines Thrust:** `LastSpeed ≥ 9`.
   - Engines must sum to at least 9 by the time altitude reaches 0.

3. **Brakes Descent:** `BrakesValue == 3` (all 3 switches activated) **AND** `BrakesValue > LastSpeed`.
   - Brakes must exceed engines speed for safe deceleration.
   - Both conditions must be true.

4. **Flaps Deployment:** `FlapsValue == 4` (all 4 switches activated).

5. **Landing Gear Deployment:** `LandingGearValue == 3` (all 3 switches activated).

6. **Approach Clear:** All planes cleared from Approach track (all segments have 0 plane tokens).

---

## 3. Loss Conditions (Pre-Landing)

These conditions trigger **immediate loss** without waiting for landing check:

### 3.1 Axis Imbalance (Immediate)
- **Trigger:** When Axis resolution occurs and `newAxisPosition` is out of bounds (< -2 or > +2).
- **Effect:** Game status set to `GameStatus.Lost` immediately.
- **Timing:** Occurs during die placement phase, before any other module resolution.

### 3.2 Altitude Exhausted (Final Round Collision)
- **Trigger:** When altitude reaches 0 (enters final round) AND Approach track still has planes remaining.
- **Effect:** Game status set to `GameStatus.Lost` immediately.
- **Logic:** In `Game.NextRound()`, after `_altitude.Advance()` sets `IsLanded = true`, check `_airport.EnterFinalRound()`. If planes still exist at final index, loss occurs.
- **Timing:** Occurs during `NextRound()` call, not during placement.

### 3.3 Landing Failure (Deferred to Landing Check)
- **Trigger:** When altitude is 0 and player calls `NextRound()` again (landing check phase).
- **Effect:** Evaluate all 6 landing criteria; if any fail, game status set to `GameStatus.Lost`.

---

## 4. Win Condition

**Trigger:** When altitude reaches 0 (final round) AND all 6 landing criteria pass.

- **Engines Thrust:** LastSpeed ≥ 9.
- **Brakes Safe:** BrakesValue == 3 AND BrakesValue > LastSpeed.
- **Flaps Deployed:** FlapsValue == 4.
- **Landing Gear Deployed:** LandingGearValue == 3.
- **Axis Balanced:** AxisPosition ∈ [-2, +2].
- **Approach Clear:** All planes cleared.

**Effect:** Game status set to `GameStatus.Won`.

---

## 5. Module Resolution Order (Fixed)

During each round, after both players have placed all dice, modules are resolved in this order:

1. **Axis** — Check and enforce balance.
2. **Engines** — Calculate speed, advance Approach (unless final round).
3. **Brakes** — Activate switches (if conditions met).
4. **Flaps** — Deploy switches (if conditions met).
5. **Landing Gear** — Deploy switches (if conditions met).
6. **Radio** — Clear planes (Blue, then Orange, in order).
7. **Concentration** — Earn tokens (if die placed).

**Rationale:** Axis is first because imbalance triggers immediate loss. Engines advances Approach before Radio clears planes. Concentration is last because token earn is post-placement.

---

## 6. Key Invariants & Edge Cases

### 6.1 Axis
- **Immediate Loss on Imbalance:** No second chance. Loss threshold is exclusive: `< -2` or `> +2` triggers loss immediately.
- **No Reset:** Axis position is cumulative and never resets between rounds.
- **Two Dice Required:** Axis must have both blue and orange die placed to resolve. No resolution if only one die is placed.

### 6.2 Engines
- **LastSpeed Calculation:** Sum of both dice. If only one die is placed, `LastSpeed` is null and must be recalculated when second die is placed.
- **Final Round:** During final round, Approach advance is suppressed (already at final segment).
- **Landing Validation:** `LastSpeed >= 9` AND `BrakesValue > LastSpeed` are both checked at landing.

### 6.3 Brakes
- **Sequential:** Switches must be activated 2 → 4 → 6 in order. Out-of-order placement is not allowed (command will not be available).
- **Exact Match:** Die must match exact required value, not range. No flexibility (unlike Flaps and Gear).
- **Full Activation Required:** For landing pass, all 3 switches must be activated (BrakesValue == 3).

### 6.4 Flaps
- **Flexibility:** Each switch allows a range of die values. Player can choose which die value to place (within allowed range).
- **Cumulative Thresholds:** Each flap deployment shifts the orange aerodynamics threshold (affects Engines speed tiers).
- **Full Deployment Required:** For landing pass, all 4 switches must be activated (FlapsValue == 4).

### 6.5 Landing Gear
- **Flexible Switches:** Each switch maps to a range of die values. Multiple different die values can activate same switch (gracefully ignored on repeat).
- **Graceful Idempotence:** If die activates already-activated switch, placement is ignored (no error, no decrement).
- **Cumulative Thresholds:** Each gear deployment shifts the blue aerodynamics threshold (affects Engines speed tiers).
- **Full Deployment Required:** For landing pass, all 3 switches must be activated (LandingGearValue == 3).

### 6.6 Radio
- **Up to 2 Orange:** Can place max 2 orange dice per round; blue is max 1.
- **Silent Clearing:** If no plane exists at offset, clearing succeeds silently (no error).
- **Cumulative Track:** Cleared planes stay cleared. Position on Approach track is monotonic (only advance, never reset).

### 6.7 Concentration
- **Max 2 per Round:** Each player can place max 1 die per round (Pilot blue, Copilot orange); total max 2 per round.
- **Token Pool Capacity:** Max 3 tokens, enforced in `Earn()` via `min(count + 1, 3)`.
- **Token Spend Before Placement:** Spend phase is optional and occurs **before** die placement on any module.
- **Spend Limits:** Can only spend tokens if pool has sufficient count. `Spend()` throws if count ≤ 0.
- **Adjustment Bounds:** Adjusted die must stay in [1, 6]. No wraparound (1 − 1 = invalid, 6 + 1 = invalid).
- **Net Token Change (Spend + Concentration Placement):** Player can spend k tokens, then place on Concentration, gaining 1 token back. Net change = 1 − k. Allowed.

### 6.8 Airport & Approach Track
- **Cumulative Position:** Approach track position only moves forward (Engines module advances) or backward (Radio clears planes). Never resets.
- **Segment Count:** Montreal airport has 7 segments (indices 0–6). Final segment is index 6.
- **Plane Tokens:** Each segment has initial plane count. Radio removes one token per die placement. Engines advances position based on speed.
- **Final Round Entry:** When altitude reaches 0, `Airport.EnterFinalRound()` is called. This moves the plane position to the final segment. If planes remain, loss occurs.

---

## 7. Verification Checklist for Implementation

- [ ] **Axis:** Immediate loss on out-of-bounds (< -2 or > +2). Both dice required for resolution.
- [ ] **Engines:** Speed = sum of both dice. LastSpeed persists for landing check. Approach advance suppressed in final round.
- [ ] **Brakes:** Sequential 2 → 4 → 6. Exact match required. Full activation (3) required for landing.
- [ ] **Flaps:** Flexible per-switch die ranges. Cumulative threshold updates. Full activation (4) required for landing.
- [ ] **Landing Gear:** Flexible switch mapping. Graceful idempotence on re-activation. Cumulative threshold updates. Full activation (3) required for landing.
- [ ] **Radio:** Max 2 orange dice per round. Silent clearing. Cumulative track position.
- [ ] **Concentration:** Max 2 per round (1 per player). Token pool 0–3. Token spend (optional) before placement. Adjustment within [1, 6], no wraparound. Net token change on spend + placement allowed.
- [ ] **Module Resolution Order:** Axis → Engines → Brakes → Flaps → Gear → Radio → Concentration.
- [ ] **Landing Win:** All 6 criteria must pass (Engines ≥9, Brakes 3 and > speed, Flaps 4, Gear 3, Axis in [-2, 2], Approach clear).
- [ ] **Landing Loss:** Any criterion fails OR Axis out-of-bounds at any time OR Approach not clear when altitude reaches 0.
- [ ] **Altitude 0 Arrival:** Final round entry checks for plane collision. If planes exist, immediate loss.

---

## 8. Clarifications & Decisions

### 8.1 Brakes Landing Criterion Reconciliation
**Prior Decision:** `.squad/decisions.md` states landing criterion as "Brakes ≥ 6".

**Current Implementation:** BrakesModule has 3 sequential switches (2, 4, 6), yielding max BrakesValue = 3.

**Resolved:** The landing check is "BrakesValue == 3 AND BrakesValue > LastSpeed". The ≥6 in prior spec may have been a transcription artifact. Locked interpretation is:
- Brakes must have all 3 switches activated (BrakesValue == 3).
- Brakes value must exceed Engines LastSpeed.
- Both conditions required for landing pass.

### 8.2 Engines Final Round Advance Suppression
**Decision:** During final round (altitude 0), Engines does NOT advance Approach further. The check `!_airport.IsFinalRound` in `CalculateApproachAdvance()` enforces this.

**Rationale:** Final round is the landing moment; no further movement occurs. Approach position is fixed, and players must have cleared all planes before reaching final round.

### 8.3 Token Spend on Concentration Die (Net Zero Change)
**Decision:** If a die is adjusted with token spend, then placed on Concentration, the net token change is `1 - k` (where k = tokens spent). For k=1, net change is 0.

**Rationale:** The placement itself earns 1 token. The spend (if any) is a separate action cost. Both are allowed and result in the expected net change.

### 8.4 Landing Gear Idempotent Placement
**Decision:** Placing a die on Landing Gear whose switch is already activated does NOT error. The placement is silently ignored (no-op) and the die remains unused.

**Rationale:** Graceful handling. Player is not penalized for the redundant placement; the die simply isn't consumed and remains available for other modules.

### 8.5 Concentration Token Pool Scoping
**Decision:** The token pool is **shared** across both players. Not per-player. Pool starts at 0 and is capped at 3.

**Rationale:** Official Sky Team rules; shared pool encourages cooperation and strategic token allocation.

### 8.6 Reroll Token (Out of Scope for This Spec)
**Decision:** Reroll mechanic is not part of this module spec. It is handled separately as a global game mechanic (1 reroll token available per game, up to 2 dice per use). Documented in `.squad/decisions.md` separately.

---

## 9. References

- **Official Sky Team Rules:** https://www.geekyhobbies.com/sky-team-rules/
- **Prior Decisions:** `.squad/decisions.md` (Concentration token spec, module clarifications, Axis/Engines/Brakes landing criteria, Telegram UX specification).
- **Implementation:** `SkyTeam.Domain/` (all 7 modules + Game + GameState + Airport).
- **Codebase Audit:** Skiles' Phase 1 assessment in decisions.md (current implementation status).

---

## 10. Sign-Off

**Specification Complete:** All 7 modules specified with invariants, edge cases, and landing criteria locked.

**Validation Status:** ✅ All modules implemented in codebase. Spec is a formalization and audit of existing behavior.

**Next Steps:**
- Aloha: Prepare test harness per this spec (boundary conditions, edge cases, landing scenarios).
- Skiles: Validate implementation against this spec; no major code changes expected (audit + minor clarifications).
- User: Review and confirm no ambiguities remain.

---

**Appended to:** `D:\Repos\skyteam-bot\.squad\agents\tenerife\history.md` (Learnings section)


</details>


---


## 2026-02-21: Issue #36 tests PR base branch

**Decision:** Deliver issue #36 (application turn/undo invariant tests) as a stacked PR targeting `squad/28-round-turn-state-secret-hand` (PR #39), because the tests exercise `RoundTurnState` / `SecretDiceHand` introduced by #28.

**Rationale:** Keeps CI green and aligns with the declared dependency graph (#36 depends on #28). Once #39 merges to `master`, this PR can be retargeted or merged normally.

---


## 2026-02-21: PR #39 (Issue #28) — ready to undraft

**By:** Skiles (Domain Dev)  
**Decision:** PR #39 (Issue #28: application-layer round/turn state + secret dice hand) is coherent and can be moved out of Draft.

**Evidence:**
- `dotnet test -c Release` passes (145 tests, 0 failures).
- Changes are isolated to application-layer round/turn orchestration primitives (no Telegram SDK leakage), matching prior Issue #28 design decision.

**Rationale:** The PR provides the minimal state machine + invariants needed for strict alternation and private placement flows, and it is now green on the full test suite in Release configuration.

---


## 2026-02-21: PR39 resync (2)

**Context:** PR #39 (`squad/28-round-turn-state-secret-hand`) was conflicting after `master` advanced.

**Decision:** Re-synced by rebasing the PR branch onto latest `origin/master` using `git rebase --rebase-merges origin/master` and force-pushing with `--force-with-lease`.

**Conflict resolution stance (if future conflicts recur):**
- Keep **current `master`** domain logic for landing/brakes (Domain layer is source of truth).
- Preserve PR branch changes for **application-layer** Round/Turn state and secret-hand workflow.

**Result:** Rebase applied cleanly (no manual conflict resolution needed) and branch updated on origin.

---


## 2026-02-21T18:56:06Z: PR #39 sync with master (Skiles)

**By:** Skiles (Domain Developer)

**Decision:** Rebased PR #39 branch (`squad/28-round-turn-state-secret-hand`) onto `origin/master` to unblock merge conflicts; resolved Brakes-related conflicts by keeping the current master landing/braking-capability logic, and skipped the older coffee-token adjustment commit that was already upstream.

**Rationale:** Minimizes divergence from master, avoids re-introducing stale token-adjustment implementations, and keeps PR focused on Issue #28 changes.

---

# Issue #30 — Telegram public placements + alternation (Skiles)

**By:** Skiles  
**Date:** 2026-02-21

## Decision
- Implement per-placement flow via **private chat** commands: `/sky place <dieIndex> <module/slot>` (no inline keyboard yet).
- Store the user-selected **placement target** (module/slot string) alongside application `RoundTurnState` placements so the bot can broadcast each reveal in the group.
- Enforce strict alternation by treating `RoundTurnState.CurrentPlayer` as the single source of truth.

## Rationale
This is the smallest change consistent with the current `/sky` command handler patterns while keeping dice hands secret and ensuring every placement is announced publicly in the group chat.
# Decision: Issue #30 — Telegram public placements + alternation enforcement (MVP)

**Date:** 2026-02-21  
**Author:** Tenerife (Rules Expert)  
**Requested by:** Gianluigi Conti  
**Target:** Issue #30 “Telegram: public placements in group + alternation enforcement”

## Context
Telegram play uses **secret hands** (DM dice) but **public placements** in the group chat. MVP must enforce **strict alternation** (one placement at a time), prevent spectators from acting, and be robust to invalid actions and Telegram retries.

---

## MVP rules to enforce

### 1) Alternation / turn ownership
1. **Strict alternation:** exactly **one die placement per turn**.
2. **After a successful placement**, `CurrentPlayer` toggles to the other seat.
3. **Round start player alternates each round** (per existing decision: “player alternates after each descent”). Recommended mapping for MVP:
   - Round 1 starts with **Pilot**.
   - Each next round starts with the **other** seat.
4. **Round end:** a round accepts **exactly 8 placements** (4 dice from Pilot hand + 4 dice from Copilot hand). After placement #8 the round is **ReadyToResolve**; no further placements are accepted.

### 2) What is a “placement”
A placement is **atomic** and consists of:
- **Actor seat** (Pilot/Copilot)
- **Die selection** from that actor’s **secret hand** (MVP: select by **die index** 0–3, not by value)
- **Target** (module + slot/position) and any declared adjustments (e.g., token spend) if supported

**Important MVP invariant:** If the placement is rejected (any validation failure), **nothing changes**: no die is consumed, no placement is logged, and the turn does not advance.

### 3) Public vs private visibility
- **Public (group):** every successful placement is posted immediately: **seat + placed value + module (+ slot)**, and the bot states **who plays next**.
- **Private (DM):** hands, choice UI, and all rejection/error feedback should be sent **only to the acting user** (to avoid group spam).

---

## Edge cases (required behavior)

### A) Wrong player tries to place out-of-turn
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **DM:** “⛔ Not your turn. Waiting for <Pilot/Copilot>.”
  - **Group:** no message (optionally: keep/refresh the “Current turn” status in `/state`).

### B) Player tries to place a die they don’t have
Covers: invalid die index, die already used, die belongs to the other seat.
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **DM:** “⛔ You can’t use that die (already used or not in your hand).”
  - **Group:** no message.

### C) Placement targets an invalid module/slot
Covers: module doesn’t exist, slot out of range, slot already occupied, module forbids that seat/color this round, any domain rule violation.
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **DM:** “⛔ Invalid placement. That slot isn’t available.”
  - **Group:** no message.

### D) Double-submission / retries
Telegram may deliver duplicate updates/callbacks; players may double-click.
- **Rule:** a placement request must be handled **idempotently**.
  - If the exact same placement is received again **after it was already accepted**, treat it as a **no-op** (do not post a second group message).
  - If the retry occurs and the state has advanced (die now used / turn switched), the request should be rejected with a clear reason.
- **Recommended responses:**
  - **DM (no-op):** “ℹ️ Already received.”
  - **DM (rejected due to new state):** reuse the relevant rejection message above.

### E) Spectators / non-seated users attempt actions
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **Group (reply to user):** “⛔ Only the seated Pilot/Copilot can place dice. Use /sky join to sit.”

---

## Recommended short user-facing wording

### Successful placement (group)
- “✅ **Pilot** placed **4** on **Engines** (slot 2). **Copilot** to play. (1/8)”

### Out of turn (DM)
- “⛔ Not your turn. Waiting for **Copilot**.”

### Die not available (DM)
- “⛔ You can’t use that die.”

### Invalid target (DM)
- “⛔ Invalid placement. Pick another slot.”

### Round complete (group)
- “✅ Round complete (8/8). Resolving…”

### Duplicate click (DM)
- “ℹ️ Already received.”
# Aloha: Issue #30 test boundary

Date: 2026-02-21

## Decision
Add coverage for public placements + strict alternation at the **application layer** (`SkyTeam.Application.Tests`) by exercising `SkyTeam.Application.Round.RoundTurnState`.

## Rationale
`RoundTurnState` is the pure, deterministic source of truth for alternation and the public placement log, and testing it avoids coupling the suite to Telegram transport/UI details.

---

## 2026-02-21T20:37:20Z: Issue #30 — Telegram public placements + alternation enforcement (Tenerife)

# Decision: Issue #30 — Telegram public placements + alternation enforcement (MVP)

**Date:** 2026-02-21  
**Author:** Tenerife (Rules Expert)  
**Requested by:** Gianluigi Conti  
**Target:** Issue #30 “Telegram: public placements in group + alternation enforcement”

## Context
Telegram play uses **secret hands** (DM dice) but **public placements** in the group chat. MVP must enforce **strict alternation** (one placement at a time), prevent spectators from acting, and be robust to invalid actions and Telegram retries.

---

## MVP rules to enforce

### 1) Alternation / turn ownership
1. **Strict alternation:** exactly **one die placement per turn**.
2. **After a successful placement**, `CurrentPlayer` toggles to the other seat.
3. **Round start player alternates each round** (per existing decision: “player alternates after each descent”). Recommended mapping for MVP:
   - Round 1 starts with **Pilot**.
   - Each next round starts with the **other** seat.
4. **Round end:** a round accepts **exactly 8 placements** (4 dice from Pilot hand + 4 dice from Copilot hand). After placement #8 the round is **ReadyToResolve**; no further placements are accepted.

### 2) What is a “placement”
A placement is **atomic** and consists of:
- **Actor seat** (Pilot/Copilot)
- **Die selection** from that actor’s **secret hand** (MVP: select by **die index** 0–3, not by value)
- **Target** (module + slot/position) and any declared adjustments (e.g., token spend) if supported

**Important MVP invariant:** If the placement is rejected (any validation failure), **nothing changes**: no die is consumed, no placement is logged, and the turn does not advance.

### 3) Public vs private visibility
- **Public (group):** every successful placement is posted immediately: **seat + placed value + module (+ slot)**, and the bot states **who plays next**.
- **Private (DM):** hands, choice UI, and all rejection/error feedback should be sent **only to the acting user** (to avoid group spam).

---

## Edge cases (required behavior)

### A) Wrong player tries to place out-of-turn
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **DM:** “⛔ Not your turn. Waiting for <Pilot/Copilot>.”
  - **Group:** no message (optionally: keep/refresh the “Current turn” status in `/state`).

### B) Player tries to place a die they don’t have
Covers: invalid die index, die already used, die belongs to the other seat.
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **DM:** “⛔ You can’t use that die (already used or not in your hand).”
  - **Group:** no message.

### C) Placement targets an invalid module/slot
Covers: module doesn’t exist, slot out of range, slot already occupied, module forbids that seat/color this round, any domain rule violation.
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **DM:** “⛔ Invalid placement. That slot isn’t available.”
  - **Group:** no message.

### D) Double-submission / retries
Telegram may deliver duplicate updates/callbacks; players may double-click.
- **Rule:** a placement request must be handled **idempotently**.
  - If the exact same placement is received again **after it was already accepted**, treat it as a **no-op** (do not post a second group message).
  - If the retry occurs and the state has advanced (die now used / turn switched), the request should be rejected with a clear reason.
- **Recommended responses:**
  - **DM (no-op):** “ℹ️ Already received.”
  - **DM (rejected due to new state):** reuse the relevant rejection message above.

### E) Spectators / non-seated users attempt actions
- **Outcome:** reject.
- **State:** unchanged.
- **Recommended responses:**
  - **Group (reply to user):** “⛔ Only the seated Pilot/Copilot can place dice. Use /sky join to sit.”

---

## Recommended short user-facing wording

### Successful placement (group)
- “✅ **Pilot** placed **4** on **Engines** (slot 2). **Copilot** to play. (1/8)”

### Out of turn (DM)
- “⛔ Not your turn. Waiting for **Copilot**.”

### Die not available (DM)
- “⛔ You can’t use that die.”

### Invalid target (DM)
- “⛔ Invalid placement. Pick another slot.”

### Round complete (group)
- “✅ Round complete (8/8). Resolving…”

### Duplicate click (DM)
- “ℹ️ Already received.”

---

## 2026-02-21T20:37:20Z: Issue #30 — Telegram placements implementation (Skiles)

# Issue #30 — Telegram public placements + alternation (Skiles)

**By:** Skiles  
**Date:** 2026-02-21

## Decision
- Implement per-placement flow via **private chat** commands: `/sky place <dieIndex> <module/slot>` (no inline keyboard yet).
- Store the user-selected **placement target** (module/slot string) alongside application `RoundTurnState` placements so the bot can broadcast each reveal in the group.
- Enforce strict alternation by treating `RoundTurnState.CurrentPlayer` as the single source of truth.

## Rationale
This is the smallest change consistent with the current `/sky` command handler patterns while keeping dice hands secret and ensuring every placement is announced publicly in the group chat.

---

## 2026-02-21T20:37:20Z: Issue #30 — Test strategy (Aloha)

# Aloha: Issue #30 test boundary

Date: 2026-02-21

## Decision
Add coverage for public placements + strict alternation at the **application layer** (`SkyTeam.Application.Tests`) by exercising `SkyTeam.Application.Round.RoundTurnState`.

## Rationale
`RoundTurnState` is the pure, deterministic source of truth for alternation and the public placement log, and testing it avoids coupling the suite to Telegram transport/UI details.


---

# Decision: Issue #32 — Resolve round after 8 public placements + broadcast state (MVP)

**Date:** 2026-02-21  
**Author:** Tenerife (Rules Expert)  
**Requested by:** Gianluigi Conti  
**Target:** Issue #32 “Application/Telegram: resolve round after 8 public placements + broadcast state”

## Context
In Telegram play, dice hands are **private** (DM) but placements are **public** (group) and strictly alternate (Issue #30). After the **8th accepted placement** (4 dice per seat), the bot must (a) apply the round to the Domain, (b) broadcast the **module outcomes + updated cockpit state**, and (c) advance altitude/round as needed (Issue #32).

This decision defines **MVP semantics** for what “resolve a round” means and what must be **broadcast publicly**, including required edge-case wording.

---

## MVP semantics — when and how a round resolves

### 1) Trigger
A round becomes resolvable when `RoundTurnState` reaches `ReadyToResolve` (exactly **8** successful placements recorded).

**MVP rule:** resolution is **automatic** immediately after placement **(8/8)** is accepted.

### 2) Domain application model (MVP)
The safest MVP is:
- **On each successful placement:** execute the corresponding Domain `GameCommand` immediately (so rule losses happen at the correct time and available-command validation stays authoritative).
- **At resolve-after-8:** assert the Domain is also at “end of round” (i.e., no dice remain, and `NextRound` is available), then execute `NextRound`.

> Note: If an implementation instead queues placements and applies them only at the end, it must still apply them **in acceptance order** and treat the whole batch as **atomic** (see “partial resolution”).

### 3) Resolution steps (observable behavior)
After the 8th placement:
1. **Freeze** the round (no undo, no more placements).
2. **Snapshot** the “end of round” cockpit values **before** calling `NextRound` (because `NextRound` resets some per-round fields like `Engines.LastSpeed`).
3. Execute Domain `NextRound`.
   - If altitude is already landed (0), `NextRound` performs the **landing outcome check** and ends the game (Won/Lost).
   - Otherwise it descends altitude, switches starting player, resets per-round module slots, and rolls the next dice.
4. **Broadcast** a single group message summarizing:
   - What the round produced (module outcomes),
   - The updated shared cockpit state,
   - Whether the game continues or has ended,
   - The next required action (who plays / dice DM status).

---

## What must be broadcast publicly (MVP)

### A) Always public (non-secrets)
These are always safe to show in the **group**:
- Round number + phase (“resolved”, “next round started”, “landing check”).
- **Altitude** (before → after), including whether the new segment grants reroll (if surfaced).
- **Axis position** (current numeric value; highlight bounds [-2,+2] for landing and the crash limit ±3).
- **Approach track** state: current position index + remaining plane tokens per segment ahead (at least totals; ideally a compact per-segment display).
- **Engines last speed** for the round (the summed speed that was just flown).
- **Brakes**: braking capability total, whether fully deployed (3 switches), and (optionally) last activated switch value.
- **Flaps**: flaps value (0–4).
- **Landing gear**: deployed count (0–3).
- **Coffee token pool**: current count (0–3) after spends/earns.
- **Game status**: In progress / Won / Lost.

### B) Placement recap (optional, but recommended)
Placements are already announced publicly per Issue #30; the round-resolution message **may** include a short recap by module (e.g., “Engines: blue 4 + orange 5 = speed 9”).

**Do not broadcast:** any player’s **new** secret dice hand values for the next round.

---

## Edge cases and required handling

### 1) Loss triggered before 8 placements (mid-round)
Some rule losses can occur the moment a placement is executed (e.g., Axis goes out of bounds after the second Axis die; approach overshoot / cannot advance with planes at current segment).

**Semantics:** the game ends **immediately**, remaining placements are ignored/rejected, and the bot must broadcast the loss reason.

**Recommended group wording:**
- “💥 **Game over** — Axis position out of bounds (±3).”
- “💥 **Game over** — Approach overshoot.”
- “💥 **Game over** — Cannot advance approach with airplanes at current position.”

### 2) Landing check on altitude 0
When `Altitude.IsLanded` and the players execute `NextRound`, the game performs the landing outcome check.

**Semantics:** broadcast either a victory or a failure with **explicit criteria**.

**Recommended group wording:**
- Win: “🎉 **Landing successful!** All landing criteria met. You win.”
- Loss: “🛬 **Landing failed.** Missing: <list failed criteria>.”

### 3) Invalid placements / invalid resolution request
- Invalid placement attempts (wrong player, wrong die, invalid module/slot, command not available) remain **private DM rejections** with no state change (Issue #30).
- If a resolve is attempted when not ready (e.g., fewer than 8 placements), reply in group (or DM) with current progress only.

**Recommended wording:**
- “⏳ Waiting for placements: (6/8). **<Seat>** to play.”

### 4) Partial resolution / non-atomic failure
If end-of-round application fails after some side effects (should be rare if commands execute at placement-time):
- Treat as an **internal error** (not a rule loss) and stop further actions.
- Broadcast a short “paused” message, and require an operator to inspect logs.

**Recommended wording:**
- “⚠️ Internal error while resolving the round. Game paused; please retry or contact an admin.”

### 5) Telegram retries / duplicate updates
Resolution must be **idempotent**:
- If the bot already resolved the round for the same state, do not broadcast again.

**Recommended DM wording (no-op):**
- “ℹ️ Already resolved.”

### 6) DM failure for next-round dice
If new dice cannot be delivered via DM (player has not `/start`ed the bot privately):
- Do **not** leak dice values.
- Broadcast that the DM failed and what the player must do.

**Recommended group wording:**
- “⚠️ I can’t DM **Pilot** the new dice. Pilot must open a private chat with me and send /start.”

---

## Recommended public round-resolution message (template)

1) **Completion marker** (already defined in Issue #30):
- “✅ Round complete (8/8). Resolving…”

2) **Resolution + cockpit snapshot + next action** (single message preferred):
- “🧩 **Round {N} resolved**\nAxis: {axis}\nSpeed: {speed}\nApproach: {position}/{segments}, planes remaining: {planesSummary}\nBrakes: {brakeCapability} ({switches}/3)\nFlaps: {flaps}/4\nGear: {gear}/3\n☕ Tokens: {tokens}/3\nAltitude: {altBefore} → {altAfter}\n➡️ **Round {N+1}**: {startingSeat} to play (dice sent via DM).”

3) **Game end** (replace “next action” section):
- “🏁 **Game over** — {Won/Lost}. {Reason}”


---

# Aloha — Issue #32 Test Boundary Decisions (2026-02-21)

## Decision: What “round resolution” means in application tests

Because the current application layer (`InMemoryGroupGameSessionStore` + `RoundTurnState`) does **not yet** integrate with the domain `Game` (no mapping from placement `Target` strings to domain commands/modules, and no cockpit state snapshot type), the Issue #32 tests treat *application-layer round resolution* as:

- After the **8th public placement**, the application session advances to the **next round**.
- Observable effects are limited to application state:
  - `GameRoundSnapshot.RoundNumber` increments (e.g., 1 → 2)
  - `GameRoundSnapshot.Status` returns to `AwaitingRoll`
  - `TurnState` is cleared, so `/sky hand` becomes `RoundNotRolled` until the next roll.

## Non-goals (deferred to implementation integration)

- Validating actual **domain module resolution** (Axis/Engines/etc.)
- Validating **broadcast formatting/content** beyond “state advanced”

These will require a domain snapshot/broadcast model and/or an application service that applies placements to the domain aggregate.


---

## 2026-02-21T21:07:28Z: User directive

**By:** Gianluigi Conti (via Copilot)
**Decision:** Follow Squad (Coordinator) v0.5.2 governance: Team Mode behavior, mandatory task-based subagent spawning, ceremony triggers, reviewer lockout, directive capture, and Ralph continuous work loop.
**Rationale:** User request — captured for team memory

---

## 2026-02-21T21:49:12Z: Telegram /sky undo + application replay log (Skiles)

**By:** Skiles (Infrastructure)
**Decision:** Implemented Telegram /sky undo command for private chats, wired to InMemoryGroupGameSessionStore.UndoLastPlacement(userId).
**Implementation:** 
- Added placement logging/replay utilities inside InMemoryGroupGameSessionStore.GameSession to rebuild DomainGame deterministically from per-round roll + placement logs.
- On roll: store round dice values.
- On placement: store executed commandId + display name.
- On undo: drop last placement log, reset game (new airport/modules/game), and replay all rounds (including NextRound between completed rounds).

**Rationale:** Domain has no built-in command undo; application-level undo is safest by replaying commands. Keeps domain pure and ensures cockpit state matches visible placement history after undo.

---

## 2026-02-21T21:46:44Z: PR #47–48 architecture review (Sully)

**By:** Sully (Architect)

**Decision:**
- Undo stays out of the Domain; implement application-level undo via deterministic replay (roll values + executed command IDs).
- Transport-agnostic cockpit rendering lives in `SkyTeam.Application.Presentation` for adapter reuse.
- Telegram adapter must be concurrency-safe: serialize per group chat and dedupe `update_id`.

**Notes (non-blocking):**
- Consider removing or using the `startingPlayer` input in `RegisterRoll(...)` to keep a single source of truth.
- Keep the “one active group session per user” invariant explicit until persistence/multi-session support exists.

