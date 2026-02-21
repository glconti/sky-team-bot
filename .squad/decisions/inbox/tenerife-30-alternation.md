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
