# Aloha: UX Epic #49 Vertical Slices Review

**Reviewer:** Aloha (Tester)  
**Requested by:** Gianluigi Conti  
**Date:** 2026-02-21  
**Epic:** #49 Button-first Telegram UX via single Cockpit message

---

## Summary

The UX epic (#49) and its 8 child stories (#50–#57) are **largely well-designed verticals**, but they require **reordering** and **acceptance criteria tightening** for true end-to-end shippability.

**Key Finding:** Stories cluster into **3 delivery tiers**:
1. **Foundation (must ship first):** Callback routing (#50) + cockpit message storage (#51)
2. **Core UX (vertical slices, each shippable):** Group buttons (#52), DM hand (#53), placement flow (#54)
3. **Hardening / Polish:** Undo (#55), callback encoding (#56), stretch WebApp (#57)

Current order (#50–#57) is **correct top-level**, but stories need **tighter E2E acceptance criteria** and a **regression safety contract**.

---

## Recommended Slice Ordering

### **Slice 1: Callback Infrastructure**
**Stories:** #50, #51, #56 (in parallel, or #50 + #51 first, #56 parallel)

**Why first:** No buttons work without callback routing and cockpit message ID storage. Callback encoding (#56) is a cross-cutting concern that should be defined **before** button rendering, not after.

**What's shippable:** Bot accepts callback queries; cockpit message persists per chat. No user-visible buttons yet, but infrastructure works.

**Suggested E2E test:** Mock callback on a cockpit, verify `AnswerCallbackQuery` fires and message edits (or recreates if missing).

**Suggested acceptance criteria additions (per issue):**
- **#50:** Add test: "Callback on expired/unknown token → toast + no crash"
- **#51:** Add test: "Cockpit deletion during gameplay → auto-recreate + continue"
- **#56:** Callback encoding spec **must be in place** before #52–#55 ship; versioning allows future changes without breaking old clients.

---

### **Slice 2a: Group Cockpit Buttons (Create/Join/Start/Roll/State)**
**Story:** #52

**Why after slice 1:** Depends on callback routing (#50) + cockpit storage (#51) + encoding spec (#56).

**What's shippable:** Players can create/join/start/roll/state via buttons in a single Cockpit message. `/sky ...` fallback still works. Group chat is **quiet** (no spam, just edits).

**User experience:** Group chat shows one Cockpit message, updates in place. Buttons are visible to everyone; invalid presses (e.g., "start" before enough players) are no-op + toast.

**Suggested E2E test scenario:**
1. User A presses "Create" button → cockpit renders with "Join" button.
2. User B presses "Join" button → cockpit updates (B listed as Player 2).
3. User A presses "Start" button → cockpit transitions to game state + shows dice.
4. User A presses "Roll" button → cockpit shows rolled dice + hand.
5. Verify all updates are **edits** (no new messages).

**Suggested acceptance criteria additions:**
- **#52:** Explicitly test: "Spectator (not seated) presses 'Start' → no-op + toast 'Not a player.'"
- **#52:** Test undo: "Start game, user A presses Roll → state changes. Does Roll affect other users' visible cockpit or only group state?" (Clarify: is Roll per-player or global?)

---

### **Slice 2b: DM Hand Message + Die Selection**
**Story:** #53

**Why parallel to #52 (or after, but independent):** Depends on callback routing (#50) + encoding (#56). Does **not** depend on group buttons (#52).

**What's shippable:** Seated players receive a DM with their hand (unseen dice), rendered with inline keyboard buttons (one per unused die + refresh). Selecting a die does **not** execute anything yet; it preps for placement flow (#54).

**User experience:** When player is seated (e.g., after pressing group "Join"), bot sends/updates a private DM with hand keyboard.

**Suggested E2E test scenario:**
1. User A joins game → bot sends DM with "Hand" keyboard (3 unused dice: 1, 2, 3).
2. User A presses die "1" button → (no change yet; waiting for command selection in #54).
3. Verify keyboard stays in sync if A's dice are modified by another player (does hand refresh? Scope for #54 or broader E2E?)

**Suggested acceptance criteria additions:**
- **#53:** "When a player undoes a placement (#55), DM hand is updated to reflect unused dice again."
- **#53:** "If 3 dice are placed, only 1 die button remains; pressing it triggers next step (command selection, #54)."
- **#53:** Test idempotency: "Pressing 'Refresh' multiple times does not corrupt state or spam AnswerCallbackQuery."

---

### **Slice 2c: Callback-Driven Placement Flow**
**Story:** #54

**Why after die selection:** Depends on #53 (die selection UI) + callback routing (#50) + encoding (#56).

**What's shippable:** Multi-step placement workflow:
1. User selects die (from #53) → hand DM shows command buttons.
2. User selects command → placement is executed.
3. DM hand + group cockpit are both updated.
4. "Cancel" button allows recovery from mis-taps.

**User experience:** Placement is now **discoverable and low-friction** (no typing command IDs).

**Suggested E2E test scenario:**
1. User A (in game, hand: dice 1,2,3) presses die "1" button in DM.
2. Bot responds with "Available commands for die 1" (e.g., Axis.AssignBlue, Engine.Thrust, ...).
3. User A presses "Axis.AssignBlue" button → placement executes.
4. DM hand updates: "Remaining dice: 2, 3."
5. Group cockpit updates: "User A placed die 1 on Axis."
6. User B's available hand/commands refresh (turn alternates).

**Suggested acceptance criteria additions:**
- **#54:** "Placement is **atomic**: either full success (hand + cockpit update) or full rollback (with clear error toast)."
- **#54:** "If placement is invalid (e.g., die already placed), callback returns error toast without corrupting hand state."
- **#54:** "Command list is paged if > 5 commands; pagination buttons stay within 64-byte limit." (Tie to #56 encoding spec.)
- **#54:** Test: "User A selects die 1, then selects die 2 (changed mind); hand shows only die 2 context, not stale state."

---

### **Slice 3: Button-Driven Undo**
**Story:** #55

**Why after placement:** Depends on #54 (placement flow works) + existing undo rules (#36).

**What's shippable:** Undo is integrated into button UX. DM hand shows "Undo" button when allowed. Pressing it undoes last placement, updates both DM hand + group cockpit, turns back to previous player.

**User experience:** Mis-taps are recoverable without typing commands.

**Suggested E2E test scenario:**
1. User A places die 1 on Axis.
2. User B places die 2 on Engine.
3. User B presses "Undo" button → die 2 returns to B's hand, turn reverts to A.
4. Group cockpit + DM hand both update.

**Suggested acceptance criteria additions:**
- **#55:** "Undo is only available to the last placer; pressing from another user's DM returns toast 'Not your turn.'"
- **#55:** "Undo is disabled when round is ReadyToResolve; pressing returns toast 'Cannot undo in resolve phase.'" (Ties to #36 rules.)
- **#55:** Test idempotency: "Pressing Undo twice in quick succession does not undo twice or crash."

---

### **Slice 4: Callback Encoding & Validation**
**Story:** #56

**When:** In parallel with #50–#51 (define spec), then validate in #52–#55 (use spec).

**What's shippable:** Documented `CallbackData` format (e.g., `v1:action:token`), encoder/decoder, central validation middleware. Invalid/expired callbacks are caught gracefully (toast, no crash).

**User experience:** No visible change, but system is **hardened** against invalid callbacks.

**Suggested acceptance criteria additions:**
- **#56:** Specify max payload size strategy (1-64 bytes → prefer server-side ephemeral store for large state).
- **#56:** Document versioning (e.g., `v1:`, `v2:` in future) to allow safe migrations.
- **#56:** Test: "Malformed callback_data (e.g., truncated, wrong version) → AnswerCallbackQuery with error toast, no unhandled exception."
- **#56:** Test: "Callback token expired (e.g., > 30 min old) → AnswerCallbackQuery with 'Offer expired, refresh hand,' no state mutation."

---

### **Slice 5: Stretch (Optional, Do Not Block)**
**Story:** #57

**When:** After main inline keyboard epic (#50–#56) ships and is stable.

**Scope:** Research + PoC for Telegram Menu Button + WebApp cockpit. Do **not** add hard dependencies.

**Suggested acceptance criteria:** Remove "Do NOT block the main inline keyboard epic" from notes and elevate it to AC: "Completion of #57 must not require changes to #50–#56."

---

## Acceptance Criteria Gaps & Regression Safety

### **Gap 1: E2E State Mutation Contracts**
Most stories lack **explicit end-to-end state contracts**. Example:
- **#52 (Group buttons):** "Pressing Roll does what? Does it immediately roll for a player, or trigger a DM interaction?" 
- **Current spec:** Vague. Interview decision mentions "group button triggers/refreshes pressing user's private DM placement UI" for placement, but what about Roll?

**Recommendation:** Before #52 ships, clarify:
- Does "Roll" execute game logic and mutate state, or does it signal "ready to roll, check DM"?
- **Proposed:** Roll should mutate game state (roll dice), update cockpit, and trigger DM hand refresh for seated players. This is one cohesive vertical.

---

### **Gap 2: Regression Test Suite Coverage**
No story explicitly commits to **regression safety** (e.g., "all existing `/sky` commands still work; all test results are identical").

**Recommendation:**
- Add acceptance criterion to **#50–#52**: "All pre-existing `/sky ...` commands continue to work without modification; behavior is identical to button path."
- Add test suite: Compare `/sky place <cmd>` result against "button → die → command → place" result; they must match (same state mutation, same messaging).

---

### **Gap 3: Chat/User Binding Validation**
Stories #50, #56 mention "validation" but don't specify what happens when callbacks cross chat boundaries or have permission mismatches.

**Recommendation (add to #56):**
- "Callback initiated in chat A, but token references chat B → AnswerCallbackQuery with error, no state mutation."
- "Callback from non-seated user in a private round → AnswerCallbackQuery with error toast, no state mutation."

---

### **Gap 4: Concurrency / Race Conditions**
Interview decision says "same per-chat lock model," but no story explicitly tests concurrent callbacks in one chat.

**Recommendation (add to #50):**
- "Two users rapidly press buttons in same chat (e.g., Roll + Join simultaneously) → one is queued/delayed, state is consistent afterward."

---

### **Gap 5: Hand/Cockpit Sync**
Stories #53 and #52 each render state independently. What if they diverge?

**Recommendation (add to #54):**
- "Placement is atomic: hand DM + cockpit are updated in one transactional operation (or at least DM is sent before cockpit, so user sees consistency). If either fails, placement is rolled back."

---

### **Gap 6: Message Deletion / Edit Failures**
#51 says "handle gracefully" but doesn't define it.

**Recommendation (tighten #51):**
- "If cockpit edit fails (message deleted / insufficient rights), bot: (1) logs error, (2) AnswerCallbackQuery with toast 'State update failed, refresh', (3) recreates cockpit next request."

---

## Suggested Per-Issue Edits

Below are minimal, concrete suggested changes to issue bodies to close gaps:

### **#50: Telegram: Handle callback_query updates**

**Current AC 3:**
> Callback handling is executed under the same per-chat lock model (no race conditions between message + callback paths).

**Suggested edit:**
Add to Acceptance Criteria:
- Every handled callback validates chat/user binding (callback from correct chat; user is seated if required). Invalid bindings return error toast, no state mutation.
- Concurrent callbacks in one chat are serialized via per-chat lock; no race conditions.
- Unknown/expired callbacks return error toast; no unhandled exceptions.

**Suggested scope addition:**
- Define a callback validation middleware (e.g., `ValidateCallbackAsync(callbackData, userId, chatId) → bool`).

---

### **#51: Telegram: Single edited group Cockpit message**

**Current AC:**
> If the cockpit message is missing/deleted, the bot recreates it and updates the stored id.

**Suggested edit:**
Tighten to:
- If cockpit edit fails (message deleted, no permission), log error and AnswerCallbackQuery with toast 'Cockpit out of sync, refresh'. Recreate cockpit on next state change.
- Cockpit message ID is stored per chat (using existing chat context, e.g., `IChatSessionStore` or similar). If ID is null/invalid, treat as "needs recreation."

**Suggested test scenario:**
1. Cockpit message created (ID stored).
2. User deletes cockpit message via Telegram client.
3. User presses a button → bot detects missing message, AnswerCallbackQuery, recreates cockpit, updates stored ID.

---

### **#52: Telegram: Inline keyboard for group cockpit actions**

**Current AC (buttons: Create/Join/Start/Roll/State):**

**Suggested edit:**
Add acceptance criteria:
- "Create" button: Creates lobby, stores message ID, shows updated cockpit with "Join" button.
- "Join" button: Adds pressing user as Player 2 (or later), updates cockpit. If user is already seated, no-op + toast.
- "Start" button: Starts game if min 2 players seated. If <2 players, no-op + toast 'Need 2 players.'
- "Roll" button: Rolls dice for current player, updates cockpit with roll result, triggers DM hand refresh for that player.
- "State" button: Edits cockpit to show current state (useful if message was out-of-view). No state mutation.
- All buttons only **attempt** action; server-side rules enforce seat/turn; invalid presses are no-op + toast (e.g., 'Not your turn').
- **Regression:** All button behaviors match equivalent `/sky create`, `/sky join`, etc. commands (compare state, messaging, behavior).

**Suggested scope addition:**
- Document button visibility rules: Which buttons show in lobby vs. game state? (E.g., "Roll" only during turn phases.)
- Test: Non-seated spectator presses "Roll" → no-op + toast 'You are not seated.'

---

### **#53: Telegram: DM "Hand" message with inline keyboard**

**Current AC:**
> • DM Hand view is rendered with an inline keyboard: Buttons to choose a die (only unused dice enabled). Undo button (when allowed). Refresh hand button.

**Suggested edit:**
Add acceptance criteria:
- Die buttons are ordered consistently (die 1, 2, 3, etc.); disabled dice are greyed out (or not shown).
- "Undo" button appears only when undo is allowed (current player has placed, and undo rules permit). Otherwise, button is missing or disabled.
- "Refresh" button re-renders hand without state mutation.
- When a die is placed (by this user or opponent), hand is automatically refreshed (or user sees stale state until next manual refresh?). **Clarify expectation.**
- **Regression:** `/sky hand` behavior is identical to DM hand button UX.

**Suggested scope addition:**
- Define: Does hand auto-refresh when an opponent places a die, or does user have to press "Refresh"? (Affects #54 and #55 integration.)

---

### **#54: Telegram: Callback-driven placement flow**

**Current AC:**
> • Placement can be initiated from the group cockpit via a "Place (DM)" button; it triggers/refreshes the pressing user's private DM placement UI (no group-visible secret dice or command IDs).

**Suggested edit:**
Add acceptance criteria:
- "Place (DM)" group button (or die selection in DM) triggers step 1: "Which die?"
- After die selection, DM shows "Available commands" buttons for that die (paged if >5).
- Selecting a command executes placement (atomic: hand + cockpit update, or rollback on error).
- On success, DM updates to show remaining dice; cockpit shows public state (e.g., "User A placed die on Axis").
- "Cancel" button at any step reverts to main hand view.
- **Atomicity:** Placement either fully succeeds or fully rolls back. If cockpit update fails, hand is reverted.
- **Regression:** Button path produces identical state/messaging as `/sky place <commandId>`.

**Suggested scope addition:**
- Ephemeral state machine: How is multi-step state stored? (E.g., "User A selected die 1, waiting for command selection" → store in `Dictionary<userId, PlacementContext>`.)
- Timeout: If user abandons placement (doesn't select command for 5 min), does state auto-clear? (Recommend: yes, with cleanup.)

---

### **#55: Telegram: Button-driven Undo in DM**

**Current AC:**
> • Undo button is available when undo is allowed; otherwise it is disabled or returns a clear toast.
> • Undo uses existing `GameSessionStore.UndoLastPlacement(...)` behavior.

**Suggested edit:**
Add acceptance criteria:
- "Undo" button only appears in DM when:
  - Current player is the one who just placed a die (matches `GameSessionStore.UndoLastPlacement` rule from #36).
  - Round is not in ReadyToResolve state.
  - Otherwise, button is hidden or disabled.
- Pressing Undo (when allowed):
  - Calls `GameSessionStore.UndoLastPlacement(...)`.
  - Updates DM hand (shows undone die as unused again).
  - Updates cockpit (shows turn reverted to previous player).
  - AnswerCallbackQuery with success toast (e.g., 'Placement undone, your turn').
- If Undo is not allowed (not last placer, round is resolving, etc.), AnswerCallbackQuery with error toast (no state mutation).
- **Regression:** Undo button behavior matches `/sky undo` command.

---

### **#56: Telegram: callback_data encoding scheme + validation**

**Current AC:**
> • A documented `CallbackData` format exists (e.g., `v1:<action>:<token>`).
> • All callbacks are validated (seat/turn checks where applicable, token expiry, chat/user binding).

**Suggested edit:**
Add acceptance criteria:
- Documented format: `v1:<action>:<encoded_state>` (max 64 bytes total).
  - `<action>`: Action ID (e.g., "roll", "place", "join", "undo") — short string.
  - `<encoded_state>`: Compressed/hashed state token. If state is large, store server-side (ephemeral map) and use short token in payload.
- Versioning: Future callbacks may use `v2:`, etc. Decoder gracefully rejects unknown versions (error toast, no crash).
- Validation middleware: Centralized `ValidateAndParseCallbackAsync(callbackData, userId, chatId) → (action, state) or error`.
  - Checks: Chat binding, user seating (if applicable), token expiry (if time-bound), action availability (e.g., can't undo in resolve state).
  - On validation failure: returns error message (for toast), no state mutation.
- Test: Malformed/expired/forged callbacks are rejected with error toast; no unhandled exceptions, no state leaks.

**Suggested scope addition:**
- Ephemeral token store: Where are server-side state tokens stored? (E.g., `Dictionary<string, EphemeralCallbackState>`, cleared on bot restart, or with TTL?)
- Token collision/uniqueness: How to avoid token collision? (E.g., use short random UUID, or include chat ID + user ID in hash.)

---

### **#57: Stretch: Telegram Menu Button / WebApp cockpit**

**Current AC:**
> • Investigate and document Telegram Menu Button + WebApp options and constraints.
> • If implemented: users can open a cockpit UI via menu button (without disrupting inline keyboard MVP).

**Suggested edit:**
Clarify priority:
- **Acceptance Criterion (research phase):** Write a markdown doc (`.squad/decisions/telegram-menu-button-research.md` or similar) summarizing:
  - Telegram Menu Button API constraints (can it be set per-user? per-chat?).
  - WebApp hosting options (serverless function, embedded web server, third-party).
  - Trade-offs (complexity, maintenance, cost, user experience).
- **Acceptance Criterion (implementation, if chosen):** Implementation must not require changes to #50–#56. It's an **additive feature only**.
- **Priority note:** Should not block epic #49 ship. Can be deferred to v2.

---

## Summary of Recommended Slice Ordering

1. **Slice 1 (Foundation):** #50, #51, #56 in parallel (callback routing, cockpit storage, encoding spec)
2. **Slice 2a (Group UX):** #52 (group buttons)
3. **Slice 2b (DM UX):** #53 (hand + die selection, can be parallel with #52)
4. **Slice 2c (Placement):** #54 (multi-step flow, depends on #52 + #53)
5. **Slice 3 (Hardening):** #55 (undo integration)
6. **Slice 4 (Polish):** #57 (stretch, do not block)

Each slice is **independently shippable** and represents a working UX improvement.

---

## Key Recommendations

1. **Define vertical slices as working end-to-end features, not just infrastructure.**
   - Slice 1 (foundation) is **not shippable to users** — it's just infrastructure.
   - Slice 2a + 2b + 2c are **true vertical slices** (users can place dice via buttons, fully end-to-end).

2. **Add regression test contracts to every story.**
   - Each slice must include tests comparing button path vs. `/sky` command path.
   - If they diverge, that's a bug.

3. **Tighten acceptance criteria around atomicity, validation, and error handling.**
   - Placement must be atomic (hand + cockpit update).
   - Invalid callbacks must not crash or leak state.
   - Concurrent callbacks must be serialized per chat.

4. **Define ephemeral state storage strategy (in #54 + #56).**
   - Multi-step placement state (user selected die X, waiting for command) needs storage.
   - Document TTL, cleanup, and collision avoidance.

5. **Clarify interview gaps (from epic #49).**
   - Questions 1–8 in epic were partially answered (cockpit is edited, buttons are visible to all, invalid presses are no-op + toast).
   - **Outstanding:** Does "Roll" mutate state or just trigger DM interaction? (Affects #52 scope.)

---

## Test Strategy (Aloha's Recommendation)

**Per slice, add:**
- **Unit tests:** Callback validation, encoding/decoding, state mutation (existing domain tests, no new domain logic).
- **Integration tests:** Message updates (edit cockpit, send hand DM), callback routing, per-chat lock serialization.
- **E2E tests:** End-to-end user flows (create → join → start → roll → place → undo) using deterministic test transcripts. Compare button path vs. `/sky` command path; results must match.
- **Regression tests:** All existing `/sky` commands continue to work; no behavior change.

---

## Conclusion

The epic is **well-structured** and stories are **mostly aligned**. With the suggested reordering (foundation → group UX → DM UX → placement → undo → polish) and tightened acceptance criteria, each slice is **incremental, independently testable, and shippable**.

**Ready for Sully/Skiles to implement.**
