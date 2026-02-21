# Tenerife: Rule Guardrails for Button-First Vertical Slices

**Date:** 2026-02-22  
**Author:** Tenerife (Rules Expert)  
**Context:** Sanity-check of proposed button-first UX (Issues #49–#57) for game rules compliance and fairness.

---

## Executive Summary

The button-first design is **sound** from a rules perspective **if and only if** the following guardrails are enforced:

1. **Server-side turn/seat validation** (not UI-enforced)
2. **DM-only secret dice rendering** (no group cockpit leaks)
3. **Command availability as truth** (prevents illegal placements)
4. **Idempotent callback handlers** (safe to Telegram retries)

All 4 are explicitly called out in existing decisions (#30, #32, #50, #53) but need **explicit acceptance criteria** in each vertical-slice issue.

---

## Rule Guardrails & Acceptance Criteria

### Guard 1: Server-Side Turn Ownership Validation

**Risk:** Any user (spectators, wrong seat) can press "Place (DM)" button → leaks action to non-seated user or allows out-of-turn placement.

**Safeguard:**

- ✅ Server **revalidates seat + turn** on every callback.
- ✅ If callback user ≠ current player's Telegram ID, **reject silently** (DM: "Not your turn") or toast + no-op.
- ✅ Callback routing must include `(userId, groupChatId)` binding; no action is valid without seat lookup.

**Affected Issues:**
- **#50** (Callback handler): Must validate `userId` against `RoundTurnState.CurrentPlayer` **before** executing any domain command.
- **#53** (Callback data design): callback_data versioning must include user ID binding or callback handler must re-validate.
- **#57** (E2E tests): Include tests for "wrong seat presses button" → rejected + no-op.

---

### Guard 2: DM-Only Secret Dice & Command IDs

**Risk:** Group cockpit button menu renders full placement options (e.g., "Axis.BluePosition:1>3") or callback_data leaks die values → reveals secret dice or token-spend strategy before placement is locked in.

**Safeguard:**

- ✅ **Group cockpit buttons are generic** (e.g., "Place (DM)", "Undo", "View Hand" → all delegate to DM).
- ✅ **Command IDs, token-spend tokens, die values never appear in group cockpit**.
- ✅ Secret placement menu lives **in DM only**; group sees only the **result** after placement is accepted (seat + value + module, per Issue #30).
- ✅ DM menu refreshes on state change (via "Place (DM)" button or explicit `/sky hand` fallback).

**Affected Issues:**
- **#51** (Cockpit renderer): Must render buttons generically; no module details, no die selectors, no token spend indicators in group message.
- **#52** (DM menu): Must include all secret state (die hand, available modules, token spend UI) in DM only. Each die must be selectable by index (0–3), not by value.
- **#53** (Callback data design): No die values, token counts, or command IDs in callback_data. Versioned action tokens only; full state on server.
- **#56** (Button lifecycle): Button "Place (DM)" always triggers DM menu refresh; menu version increments on state change to invalidate old buttons.

---

### Guard 3: Domain Command Availability is the Single Source of Truth for Legality

**Risk:** Callback handler accepts a placement (e.g., "place die on Concentration") but domain rejects it as illegal (e.g., Concentration already has a die this round, or insufficient tokens) → state desync or rule violation.

**Safeguard:**

- ✅ **On every successful placement**, execute the domain command **immediately** (not queued for end-of-round).
- ✅ If domain command fails (throws), **reject the placement** (no die consumed, no log entry, turn does not advance).
- ✅ DM menu is rendered **after each successful placement** with **only the currently available commands** (as returned by domain `Game.AvailableCommands()`).
- ✅ Never render a "Place" button for a module/slot if the domain hasn't listed it as available.

**Affected Issues:**
- **#50** (Callback handler): Wrap domain command execution in try/catch; on failure, reply with DM error (e.g., "That slot is full" or "Insufficient tokens"). Do **not** log, do **not** advance turn.
- **#52** (DM menu): Button labels and availability are driven by `Game.AvailableCommands()` at render time. If the domain says Axis is not available, no Axis button appears.
- **#54** (State store): Ensure every state query (to render menu or validate callback) pulls fresh `Game` state from domain, not stale copies.
- **#57** (E2E tests): Test "player tries to place on full slot" → rejected + DM error + no-op + turn does not advance.

---

### Guard 4: Idempotent Callback Handlers for Telegram Retries

**Risk:** Telegram delivers duplicate callback_query updates (known behavior); handler applies the same placement twice → die is consumed twice, turn advances twice, or phantom placements appear in log.

**Safeguard:**

- ✅ **All callback handlers are idempotent**: if the same callback (same user, same action, same timestamp/nonce) arrives twice, the second is a no-op.
- ✅ **Placement deduplication** is tracked by `(userId, groupChatId, roundNumber, actionId, nonce)` or similar unique key.
- ✅ Response to duplicate: DM toast "Already received" or silent no-op; do **not** re-broadcast to group, do **not** re-execute domain command.

**Affected Issues:**
- **#50** (Callback handler): Implement idempotency check **before** executing domain command. Track executed callbacks in session store; lookup before execute.
- **#54** (State store): Store `LastProcessedCallbackId` (or similar) per group session. Callback handler checks this before execution.
- **#57** (E2E tests): Include "duplicate callback delivered" → no-op + same DM response as first.

---

### Guard 5: Mid-Round Loss Ends Game Immediately

**Risk:** Player places a die that triggers a rule loss (e.g., Axis goes out of bounds); game should end immediately, but button flow might continue accepting placements.

**Safeguard:**

- ✅ Domain `ExecuteCommand` returns `GameRuleLossException` (or similar) **immediately** when placement triggers loss (e.g., Axis ± 3).
- ✅ Callback handler **catches loss exceptions** and broadcasts the loss reason to group (e.g., "💥 Game over — Axis imbalance") without freezing the session.
- ✅ After loss, cockpit message is updated to end-of-game state; all buttons become disabled or replaced with "New game" buttons.
- ✅ Remaining placements are **rejected** (DM: "Game over" or similar).

**Affected Issues:**
- **#50** (Callback handler): Catch `GameRuleLossException`, broadcast to group, disable cockpit buttons, reply in DM "Game has ended".
- **#51** (Cockpit renderer): After loss, render end-of-game cockpit (no action buttons, show loss reason clearly).
- **#57** (E2E tests): Test "Axis placement triggers loss" → domain throws, callback catches, group message posted, remaining placements rejected.

---

### Guard 6: No Undo After Game Loss or Round Resolve

**Risk:** Player undoes a placement that was the losing move; game is unlosn → breaks fairness.

**Safeguard:**

- ✅ Undo is **only available**:
  - During active gameplay (before round 8/8 placements).
  - For the player who placed the die.
  - Before the opponent has played.
- ✅ Once `RoundTurnState` reaches `ReadyToResolve` (8/8), **undo is disabled**.
- ✅ Once a game-loss exception is thrown, **undo is disabled**.

**Affected Issues:**
- **#52** (DM menu): "Undo" button is conditionally rendered only if `RoundTurnState.CanUndo(userId) == true`.
- **#56** (Button lifecycle): Button state is version-stamped; old undo buttons (from before round resolved) are expired and rejected with toast "Round already resolved".
- **#57** (E2E tests): Test "undo after 8/8 placements" → rejected. Test "undo after loss" → rejected.

---

## Acceptance Criteria by Issue

### #49 (Epic: Button-First Cockpit UX)

- [ ] All child issues (#50–#57) include **explicit guardrails** from this checklist in their acceptance criteria.
- [ ] Definition of Done includes: "All rule-guard test cases passing (Issues #50, #57)".

---

### #50 (Callback Handler)

- [ ] Callback validates `userId` against `CurrentPlayer` before executing domain command.
- [ ] Domain command execution is wrapped in try/catch; `GameRuleLossException` is caught and broadcast to group.
- [ ] Invalid moves (domain rejects command) are rejected with DM error; no die consumed, no turn advance.
- [ ] Duplicate callbacks (same user, same action, within same round) are deduplicated; second is no-op + DM "Already received".
- [ ] Mid-round loss disables all future placements in that round.
- [ ] Test: "Wrong seat presses button" → rejected DM "Not your turn", group state unchanged.
- [ ] Test: "Duplicate callback" → first placement succeeds, second is no-op + toast.
- [ ] Test: "Axis loss triggered by placement" → domain throws, game ends, group broadcast posted.

---

### #51 (Cockpit Renderer)

- [ ] Group cockpit renders **no secret information**: no die values, no command IDs, no token spend details.
- [ ] All action buttons (except "View Hand" / "New Game") delegate to DM (e.g., "Place (DM)" → triggers DM menu refresh).
- [ ] Cockpit shows **only public state**: round number, current player seat, placement count (x/8), altitude, axis position, approach/modules summaries.
- [ ] After game loss or round resolve, buttons are disabled or hidden; cockpit shows end-of-game reason.
- [ ] Test: "Cockpit renderer on active round" → no die values, no command IDs visible.
- [ ] Test: "Cockpit after game loss" → loss reason prominent, buttons disabled.

---

### #52 (DM Menu)

- [ ] DM menu displays secret dice hand (indexed 0–3, values visible to user only in DM).
- [ ] Available placements (modules/slots) are driven by `Game.AvailableCommands()` at render time.
- [ ] Buttons that are not available are not rendered (or rendered disabled).
- [ ] Die-selector buttons (0–3) do **not** appear in group cockpit, only in DM.
- [ ] "Undo" button is **only rendered** if `RoundTurnState.CanUndo(currentUserId) == true` and round < 8/8.
- [ ] Menu versioning invalidates buttons from previous menu versions.
- [ ] Test: "Render DM after Concentration placed" → Concentration "Place" button unavailable (not rendered).
- [ ] Test: "Render DM after 8/8 placements" → no "Place" buttons, only "View cockpit" / "New game".

---

### #53 (Callback Data Design)

- [ ] Callback_data is versioned (e.g., `v1:place:a2`, `v1:undo:x3`) and **never contains** die values, full command IDs, or token counts.
- [ ] All state mapping (action token → actual command/module/slot) is **server-side only** in MenuState store.
- [ ] Callback validation re-queries current game state before execution (not cached in callback_data).
- [ ] Versioning supports **button expiration**: old menu versions (before state change) are rejected with toast "Menu expired, refresh".
- [ ] Test: Verify callback_data stays under 64 bytes per Telegram spec.
- [ ] Test: "Callback with expired menu version" → rejected + toast "Menu expired".

---

### #54 (State Store / Session Persistence)

- [ ] Session store tracks `CurrentPlayer` (from `RoundTurnState.CurrentPlayer`) as the single source of truth for turn ownership.
- [ ] Every state query (for menu rendering, callback validation) pulls **fresh** `Game` state from domain, not stale copies.
- [ ] Deduplication key `(userId, groupChatId, roundNumber, actionId)` is stored; duplicate callbacks are no-op'd.
- [ ] Session timeout or game end clears menu state and revokes all callbacks.
- [ ] Test: "Simulate Telegram retry (duplicate callback)" → second is no-op, no state change.

---

### #55 (Deep-Link Onboarding)

- [ ] Deep-link `/start?game=<groupChatId>` or `/start` → if user is already seated, confirm and refresh DM hand menu; if not seated, offer "Join as Pilot" / "Join as Copilot" (if seat available).
- [ ] After user joins, DM hand menu is shown **only if** a round is active (RoundTurnState exists and is not pre-roll).
- [ ] User cannot join a game that is already over (lost or won).
- [ ] Test: "Deep-link to active game, user joins" → DM menu shows current hand + available placements.

---

### #56 (Button Lifecycle & Versioning)

- [ ] Menu state is versioned; each state change (placement, undo, round end) increments menu version.
- [ ] Old menu buttons (with stale menu version) are rejected with toast "Menu expired, refresh".
- [ ] "Place (DM)" button always refreshes DM menu to latest version.
- [ ] Cockpit "Undo" button is **only rendered** during active turn and before opponent plays (guarded by `RoundTurnState.CanUndo()`).
- [ ] After 8/8 placements, all action buttons are replaced with status message (e.g., "Resolving…") and then "New game" / "View stats".
- [ ] Test: "Place die → menu version increments; old button rejected".
- [ ] Test: "After 8/8 placements, 'Place' buttons are gone".

---

### #57 (E2E Tests)

**Core Rule Guard Tests:**

- [ ] **Turn ownership:** Wrong seat presses "Place (DM)" → DM "Not your turn", group state unchanged, turn does not advance.
- [ ] **Secret dice:** Group cockpit never shows die values, command IDs, or token spend. DM shows dice. After placement, only result (seat + value + module) is in group.
- [ ] **Command availability:** Placement on full slot / insufficient tokens → rejected DM error, no die consumed, no turn advance.
- [ ] **Idempotency:** Duplicate callback (same user, same action, same round) → second is no-op, DM "Already received", group message not re-posted.
- [ ] **Mid-round loss:** Axis placement triggers loss (± 3) → game ends immediately, group loss broadcast, remaining placements rejected.
- [ ] **Undo gating:** Undo after 8/8 → rejected. Undo after loss → rejected. Undo by wrong player → rejected.

**Scenario Tests:**

- [ ] **Full game flow:** Two players alternate 8 placements → round resolves → cockpit updates → next round starts → dice DM'd privately.
- [ ] **Loss mid-round:** Player 1 places Axis die → loss triggered → game ends → buttons disabled.
- [ ] **Landing check:** After round at altitude 0 → win or loss broadcast with criteria.

---

## Summary: Rule Violations Prevented

By enforcing these guardrails, the button-first UX **prevents**:

1. ✅ Spectators or wrong-seat players placing dice.
2. ✅ Secret dice values leaking to group chat.
3. ✅ Illegal placements (full slots, insufficient tokens) being accepted.
4. ✅ Telegram retries causing phantom duplicate placements.
5. ✅ Mid-round losses being undone or ignored.
6. ✅ Turn order desync (server state disagrees with visible cockpit).
7. ✅ Stale buttons accepting actions after round ends.

**Trust Model:** Buttons are UI only; **server is the referee**. Every callback must revalidate turn ownership, command legality, and idempotency before executing any domain change.

---

## Cross-Issue Notes

- **#30** (Public placements) already defines turn ownership; callback handler (#50) must implement its enforcement.
- **#32** (Round resolution) already defines when round ends; button lifecycle (#56) must implement menu version expiration.
- **#31** (Domain modules) already enforces rule invariants; callback handler (#50) must catch and broadcast losses.
- **#33** (Undo) already gates undo by seat + round state; DM menu (#52) must conditionally render undo button.

**No new rules are needed.** These guardrails are **restatements** of existing decisions (#30, #31, #32, #33) in terms of button-first callback validation.

