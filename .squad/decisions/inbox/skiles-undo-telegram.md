### 2026-02-21: Telegram `/sky undo` + application replay log

**By:** Skiles

**What:**
- Added a private-chat `/sky undo` command to the Telegram bot, wired to `InMemoryGroupGameSessionStore.UndoLastPlacement(userId)`.
- Implemented missing placement logging/replay utilities inside `InMemoryGroupGameSessionStore.GameSession` to make undo work by rebuilding the `DomainGame` deterministically from per-round roll + placement logs.

**Why:**
- The domain has no built-in command undo; application-level undo is safest by replaying commands.
- This keeps the domain pure and ensures the cockpit state matches the visible placement history after an undo.

**How (high level):**
- On roll: store round dice values.
- On placement: store executed `commandId` + display name.
- On undo: drop last placement log, reset game (new airport/modules/game), and replay all rounds (including `NextRound` between completed rounds).