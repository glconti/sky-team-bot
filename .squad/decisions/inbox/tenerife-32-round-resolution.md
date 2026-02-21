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
