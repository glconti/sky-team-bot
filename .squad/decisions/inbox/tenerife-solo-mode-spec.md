# Decision: Solo Testing Mode Specification

**Agent:** Tenerife (Rules Expert)  
**Date:** 2026-03-02  
**Status:** Ready for Team Review  

---

## Decision Summary

Solo mode is a **testing and exploration convenience**, not a game variant. It allows a single player to control both Pilot (blue dice) and Copilot (orange dice) without requiring a second human. 

**Recommended approach:** Player rolls both hands, places all 8 dice simultaneously with full visibility. All rules remain identical to 2-player mode.

---

## Key Architectural Decisions

### 1. Domain Model: No Changes Required
- `Game` aggregate is **mode-agnostic**
- All module rules apply identically
- Landing/loss conditions unchanged
- Game aggregate does not need to know about solo mode

### 2. Session Layer: Add Mode Flag (Recommended)
```csharp
enum GameMode { TwoPlayer, Solo }

record GameSession {
    public required GameMode Mode { get; init; }
    public required string PilotId { get; init; }
    public required string? CopilotId { get; init; }  // null in solo
}
```

**Why:** Session creation, lobby routing, and UI adaptation require mode awareness at the application layer. Domain layer remains pure.

### 3. Placement Mechanics: Visible & Simultaneous
- **Why this choice:** Supports testing scenarios (developer can set up exact dice combinations), preserves game integrity (all rules apply), minimizes code complexity (no special "secret" state).
- **Alternative rejected:** Sequential placement (introduces asymmetric information not present in 2-player) or hidden placement (adds friction to testing).

### 4. Module Behavior: Identical
- **Concentration:** Token pool works the same; solo player "implicitly controls both" for agreement purposes.
- **Radio:** Blue + orange dice sum clears planes identically.
- All other modules: No special solo logic.

### 5. Win/Loss Conditions: Unchanged
- **Landing success:** All 6 criteria (axis, engines, brakes, flaps, gear, approach clear)
- **Loss:** Axis imbalance, altitude exhaustion, landing failure, collision

---

## Artifacts Produced

- `.squad/agents/tenerife/solo-mode-spec.md` — Full specification (10 sections, verification checklist, implementation roadmap)

---

## Cross-Team Coordination

### Sully (Architecture)
- Confirm: Game aggregate is mode-agnostic
- Implement: Session layer `GameMode` flag if desired
- Update: Telegram/WebApp routing to display both hands (solo) vs. current player (2-player)

### Skiles (Domain/Implementation)
- **Action:** No domain changes required
- **Validation:** Confirm module implementations work identically in solo mode
- **Test:** Run domain tests with solo game instances

### Aloha (Testing)
- Create: Solo test harness (single player executes all moves)
- Verify: Win/loss parity with 2-player rules
- Coverage: Concentration tokens, Radio clearing, landing outcomes in solo mode

---

## Questions for Team

1. **Session mode tracking:** Should `GameSession` add `GameMode` property, or is solo mode implicit (e.g., single seat claimed)?
2. **Reroll tokens:** Should reroll token reuse be tracked per human player or per seat? (Likely per seat; doesn't change in solo mode.)
3. **Mini App display:** Show all 8 dice in solo, or maintain current player view (4 dice)?

---

## Verification Checklist

- [x] All module rules specified as identical
- [x] Win/loss conditions unchanged
- [x] Domain aggregate requires no changes
- [x] Session layer mode flag optional but recommended
- [x] Placement mechanics (visible + simultaneous) justified
- [x] Concentration/Radio behavior clarified
- [x] Implementation roadmap provided (domain validation → session layer → UI → tests)

---

## Next Steps

1. **Sully:** Review architectural recommendations (session mode flag, UI adaptation)
2. **Skiles:** Validate domain implementations are mode-agnostic
3. **Aloha:** Begin solo test harness preparation
4. **User:** Confirm solo mode approval; clarify session mode tracking (if needed)

---

## Links

- Full specification: `.squad/agents/tenerife/solo-mode-spec.md`
- Team history: `.squad/agents/tenerife/history.md` (update pending)
