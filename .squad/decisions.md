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
