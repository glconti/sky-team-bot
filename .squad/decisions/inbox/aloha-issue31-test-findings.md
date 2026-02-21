# Aloha — Issue #31 test findings

**Date:** 2026-02-21

## What was added (tests)
- Axis: out-of-bounds resolution throws immediately when axis would go `< -2` or `> 2`, plus explicit coverage for boundary positions `-2` and `+2`.
- Landing outcome matrix: one passing WIN case and one focused LOSS case per landing criterion (axis out-of-bounds at landing, engines thrust below 9, brakes not fully deployed, flaps not fully deployed, landing gear not fully deployed, approach not fully cleared).
- Concentration / coffee tokens boundaries: token pool ctor bounds, Earn/Spend bounds (including k=1 and k=2), and die value bounds (cannot create dice outside 1..6).

## Spec mismatches / ambiguities noticed
1. **Brakes landing criterion is inconsistent in the locked spec.**
   - Spec states `Engines LastSpeed >= 9` *and* `BrakesValue == 3` *and* `BrakesValue > LastSpeed`.
   - If `BrakesValue` is “switch count” (0–3), then `BrakesValue > LastSpeed` is impossible when `LastSpeed >= 9`.
   - Current code treats `BrakesValue` as the last activated required value (2/4/6) and landing checks `BrakesValue >= 6` without any `BrakesValue > LastSpeed` comparison.
   - **Recommendation:** clarify whether the intended brakes landing check is “all switches deployed” only, or a different brakes magnitude (e.g., sum of deployed switch values) meant to be compared to speed.

2. **Coffee-token die adjustment is implemented via token-adjusted command ids.**
   - `Game.GetAvailableCommands()` surfaces token-adjusted commands like `Axis.AssignBlue:1>3` when tokens are available (cost = `|effective - rolled|`).
   - `Game.ExecuteCommand()` spends the required tokens, consumes the rolled die, and assigns the effective value (still bounded to 1..6).
   - Tests cover command surfacing/spend behavior plus pool and die-value boundaries.
