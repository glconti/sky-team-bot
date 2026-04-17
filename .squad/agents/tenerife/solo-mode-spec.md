# Solo Testing Mode Specification

**Tenerife, Rules Expert**  
**Status:** Initial Specification  
**Date:** 2026-03-02  

---

## 1. Solo Mode Definition

### Purpose
Solo mode is a **development and testing convenience**, not a game variant. It allows a single person to control both the Pilot (blue dice) and Copilot (orange dice) seats to play through complete game scenarios without needing a second player.

### Use Cases
- **Automated testing:** Test harnesses verify game flow, module interactions, landing outcomes
- **Manual playtesting:** Single developer explores edge cases, verifies rule implementations, validates game balance
- **Demonstration:** Show game mechanics without requiring two participants
- **Debugging:** Isolate module behavior by controlling exact dice assignments

### Key Constraint
Solo mode is **NOT** a new game variant with relaxed rules. It is purely a **"who controls what"** switching mechanism. All game rules, win/loss conditions, module interactions, and turn flow remain **identical** to 2-player mode.

---

## 2. Rules Adaptations: Secret/Simultaneous Placement

### 2-Player Mode (Baseline)
- **Pilot rolls 4 blue dice** (secret from Copilot)
- **Copilot rolls 4 orange dice** (secret from Pilot)
- Both players place their dice on modules **simultaneously** and **secretly**
- Neither player sees the other's assignments until both have committed
- **Concentration module:** Requires both players to agree on placement (post-placement interaction)
- **Radio module:** Both players must coordinate on frequency (post-placement interaction)

### Solo Mode: Recommended Approach (Option B)
**Player rolls both hands, places simultaneously with full visibility.**

#### Rationale
- Preserves game integrity: all module logic, interactions, and landing criteria remain unchanged
- **Practically testable:** Developer can reason about both hands at once, set up specific scenarios
- **Minimizes aggregate complexity:** No need for separate "secret" vs. "revealed" game states
- **Maintains concentration/radio mechanics:** Both modules still work because the solo player controls both seats

#### Flow
1. **Dice Phase:** Player rolls 4 blue dice + 4 orange dice (all visible)
2. **Placement Phase:** Player assigns blue dice to modules, then assigns orange dice to modules
   - Player sees all 8 dice simultaneously
   - Player considers Concentration and Radio needs during placement
3. **Resolution:** Modules resolve in canonical order (Axis → Engines → Brakes → Flaps → Gear → Radio → Concentration)
4. **Concentration/Radio:** Treated as "both players have already agreed" (since the solo player controls both)

#### Behavioral Equivalence
- From the game's perspective, solo mode **IS** 2-player with both decisions made by one actor
- No special rules, no shortcuts, no "skip resolution" options
- Win/loss conditions identical

---

### Alternative (Not Recommended): Option A - Sequential Placement
**Player places Pilot dice first, then Copilot dice (sequential, each visible when placed).**

**Drawback:** Introduces **asymmetric information** that doesn't exist in real 2-player Sky Team. Player would know Pilot's full commitment before deciding Copilot, potentially breaking game balance through privileged knowledge. **Not recommended.**

---

### Alternative (Not Recommended): Option C - Hidden Then Revealed
**Player places both hands secretly (drawing from shuffled pool), then reveals.**

**Drawback:** Adds unnecessary friction to testing. Testing a specific scenario (e.g., "Axis module fails at value X") becomes harder because placements are randomized. **Not recommended.**

---

## 3. Game Flow Differences

### Turn Structure (Solo)
1. **Roll Phase:** 4 blue + 4 orange dice rolled
2. **Placement Phase:** Single player assigns all 8 dice to modules
   - No turn switching between Pilot and Copilot (both controlled by same actor)
   - Player may place blue dice first, then orange (convention), or any order they choose
3. **Resolution Phase:** Identical to 2-player
   - Modules resolve in canonical order
   - Landing checks, speed tracking, altitude advancement, etc.
4. **Round Advance:** Same as 2-player

### Concentration Module (Solo)
**No behavior change.**

In 2-player mode:
- Pilot/Copilot place dice on Concentration to gain coffee tokens
- Spending tokens: both players must agree on which die to adjust

In solo mode:
- Solo player places dice on Concentration (or not)
- Spending tokens: solo player decides (controlling both seats implicitly means consensus)
- Token pool mechanics, multi-token spend, and bounds remain identical

**Implication:** From the aggregate's perspective, there is no special "solo" token logic. The `Game` class simply sees one player executing all commands.

### Radio Module (Solo)
**No behavior change.**

In 2-player mode:
- Pilot + Copilot each place dice on Radio
- Combined value clears planes on the airport track
- Both players see the final sum and outcome

In solo mode:
- Solo player places blue dice on Radio
- Solo player places orange dice on Radio
- Combined sum resolves identically
- No negotiation needed (solo player controls both)

### Win/Loss Conditions
**Identical to 2-player.**

- **Win:** Altitude 0, axis [-2, +2], engines ≥9, brakes deployed + sufficient, flaps ≥4, gear ≥3, all planes cleared
- **Loss:** Axis imbalance, altitude exhausted, landing criteria fail, collision on approach, engine failure

### No "Assisted" Modes
Solo mode does **NOT** include:
- Reroll tokens (already in 2-player rules; no special solo mechanic)
- "Undo" (already available in application layer if needed; not a rule mechanic)
- Relaxed landing criteria (rules unchanged)
- Skipped modules or phases

---

## 4. Domain Model Impact

### Game Aggregate State
The `Game` class **does not need to know about solo mode**. It operates on two players (Pilot/Copilot) regardless.

**Why:** Solo mode is a **session/presentation concern**, not a game rule concern.

#### Current State (No Changes Required)
```csharp
class Game
{
    private readonly Airport _airport;
    private readonly Altitude _altitude;
    private readonly GameModule[] _modules;
    private readonly GameState _state = new();
    
    internal Player CurrentPlayer => _state.CurrentPlayer;
    internal IReadOnlyList<BlueDie> UnusedBlueDice => _state.UnusedBlueDice;
    internal IReadOnlyList<OrangeDie> UnusedOrangeDice => _state.UnusedOrangeDice;
}
```

**Invariant:** The `Game` class alternates `CurrentPlayer` (Pilot ↔ Copilot) based on turn flow and altitude track, **regardless of how many humans are playing**.

### GameSession Aggregate (Application Layer)
The `GameSession` (or equivalent application-layer entity managing a game instance) may need to track solo mode for **UI/UX purposes only**.

#### Proposed Additions
```csharp
record GameSession
{
    public required GameMode Mode { get; init; } // TwoPlayer, Solo
    public required string PilotId { get; init; }
    public required string? CopilotId { get; init; }  // null in solo mode
    
    // ... other fields ...
}

enum GameMode
{
    TwoPlayer,  // Normal 2-player cooperative
    Solo        // Single player controls both Pilot and Copilot
}
```

#### Implications
- **Session creation:** Lobby/init flow chooses mode; in solo mode, only one seat is claimed
- **Seat eligibility:** In solo mode, only Pilot seat (or convention) is claimed; Copilot seat is controlled by the same user
- **Placement UI:** Application layer may show both hands at once in solo mode (all 8 dice visible)
- **No aggregate rule change:** Game logic remains identical; mode is metadata

---

## 5. API/UX Impact (Application Layer)

### Telegram Bot Endpoints (Minor Changes)

#### Lobby Flow
- `/sky new` → Ask: "Two-player or solo?"
  - Two-player: Normal flow (wait for second player)
  - Solo: Create game immediately with one seat, proceed to game
- `/sky state` → Show current game mode in status message (e.g., "Solo Game" vs. "2-Player Game")

#### In-Game Commands
- `/sky place <module> <die-value>` → Accept placements from solo player (no change needed; solo player just issues commands as normal)
- No new commands needed; existing placement interface works as-is

### WebApp / Mini App (UI Convenience)
- **Button layout:** In solo mode, display all 8 dice (4 blue + 4 orange) on one screen
- **2-player:** Show only the current player's dice (4 blue or 4 orange, depending on whose turn)
- **Confirmation flow:** Same as 2-player (place → resolve → advance)
- **No UI state machine change:** Core game loop works identically

### Implementation Note for Sully (Architecture)
- `GameSession.Mode` property (enum or discriminator)
- `GameSession.CopilotId` nullable (null if solo)
- `GameSession.PilotId` required (always set, even in solo)
- No changes to command execution, module resolution, or landing logic
- Application layer only: UI/UX layer adapts display based on mode

---

## 6. What Does NOT Change

### Module Rules
All module rules remain **identical** in solo mode:

- **Axis:** Position tracking, balance checking at landing
- **Engines:** Speed accumulation, final round suppression, landing requirement
- **Brakes:** Deployment tracking, braking capability vs. landing speed
- **Flaps:** Accumulation, landing requirement
- **Landing Gear:** Deployment tracking, landing requirement
- **Radio:** Plane clearing via combined blue + orange sum
- **Concentration:** Coffee token pool (max 3), multi-token spend on die adjustment

### Placement Mechanics
- Dice assignment to modules (one die per assignment)
- Invalid moves caught identically
- Token adjustment (if applicable) works the same

### Turn Flow
- Pilot and Copilot alternate turns (same as 2-player)
- Altitude advances each round (same as 2-player)
- Round resolution order is fixed (same as 2-player)

### Landing Win/Loss
- **Landing success:** All 6 criteria must pass (axis, engines, brakes, flaps, gear, approach)
- **Landing failure:** Any criterion fails
- **Altitude exhaustion:** Lose if altitude reaches 0 with no valid landing
- **Loss during play:** Axis imbalance, collision on approach, engine failure

### Reroll Tokens
Reroll tokens are an **official Sky Team mechanic**, not a solo-mode convenience. They are available in both 2-player and solo modes, subject to the same rules (1 token per game, max 2 dice per use, found on altitude track).

---

## 7. Design Rationale & Decisions

### Why Not a Separate "Solo Variant"?
Solo mode is **not** a game variant (like "hard mode" or "story mode"). It is purely a **control mechanism**. The rules engine (`Game` class) is entirely unaware of solo mode; from its perspective, both Pilot and Copilot are being controlled (by the same human in solo mode, or two humans in 2-player mode).

### Why Visible/Simultaneous Placement?
Testing and single-player exploration benefit from seeing all available resources at once. The alternative (sequential placement) introduces artificial information asymmetry that could hide bugs or break game balance in ways 2-player mode wouldn't expose.

### Why No Aggregate Changes?
The `Game` aggregate is pure domain logic. Solo mode is a session/presentation concern. Mixing testing concerns into the core domain model adds complexity and drag; a simple mode flag at the session level is cleaner.

### Why Concentration/Radio Still Require "Agreement"?
In solo mode, the solo player **implicitly controls both seats**. When the player places dice on Concentration and spends tokens, they are making a decision that affects both colors of dice. No special handling is needed; the player simply chooses to place or not place, and to spend tokens or not, as if they were both players.

---

## 8. Verification Checklist

### Rule Preservation
- [ ] All 7 module rules apply identically in solo mode
- [ ] Landing criteria identical (6 conditions, all must pass)
- [ ] Loss conditions identical (axis imbalance, altitude exhaustion, landing failure, collision)
- [ ] Altitude track advancement identical
- [ ] Approach track plane clearing identical
- [ ] Coffee token pool mechanics identical
- [ ] Reroll token mechanics identical

### Game Aggregate (Domain)
- [ ] `Game` class requires no code changes
- [ ] `GameState` class requires no code changes
- [ ] Module classes require no code changes
- [ ] `NextRoundCommand` works identically

### Application Layer (Session)
- [ ] `GameSession` adds optional `GameMode` discriminator (recommended)
- [ ] Solo mode creates session with one seat claimed
- [ ] 2-player mode creates session with two seats unclaimed
- [ ] Placement validation identical (no special solo checks)

### Telegram/WebApp Integration
- [ ] Lobby flow offers "solo" option
- [ ] `/sky state` displays mode in status
- [ ] Mini App displays all 8 dice in solo, current player's dice in 2-player
- [ ] Placement buttons/UX unchanged

### Testing
- [ ] Test harness can run complete game in solo mode
- [ ] Solo game wins with same landing criteria as 2-player
- [ ] Solo game loses with same loss conditions as 2-player
- [ ] Concentration tokens work in solo mode
- [ ] Radio module clears planes correctly in solo mode

---

## 9. Implementation Roadmap (For Sully/Skiles/Aloha)

### Phase 1: Domain Validation (No Changes)
- Confirm: `Game` class works as-is
- Confirm: Module implementations are mode-agnostic
- Confirm: Landing/loss validation is mode-agnostic

### Phase 2: Application Layer (Session Mode Flag)
- Add `GameMode` enum (TwoPlayer, Solo)
- Add `GameSession.Mode` property
- Add `GameSession.CopilotId` (nullable)
- Lobby logic branches on mode (immediate start vs. wait for second player)

### Phase 3: Telegram/WebApp Integration
- Update `/sky new` to offer mode choice
- Update `/sky state` to display mode
- Update Mini App to show all dice (solo) vs. current player dice (2-player)

### Phase 4: Test Harness
- Aloha creates solo test suite
- Run full game scenarios in solo mode
- Verify win/loss parity with 2-player rules

---

## 10. Open Questions & Escalations

### Deferred (Out of Scope for M1)
None. Solo mode is a pure **presentation/session concern** with no domain rule implications.

---

## Summary

Solo testing mode is a **non-rule change** that lets one person control both Pilot and Copilot seats. By adopting visible, simultaneous placement for both hands:
- All 7 module rules remain identical
- All win/loss conditions remain identical
- Game aggregate (`Game` class) requires no changes
- Application layer adds a simple `GameMode` flag for session management
- Testing and exploration become easier without breaking game integrity

**Recommendation:** Proceed with Option B (visible simultaneous placement), minimal application-layer changes, no domain model changes.
