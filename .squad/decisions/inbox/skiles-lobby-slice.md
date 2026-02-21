# Skiles — Lobby slice (group commands)

## 2026-02-21

### Decision: `/sky new` is non-destructive

- `/sky new` creates a lobby for the group **only if one does not already exist**.
- If a lobby already exists, the bot reports that fact and shows the current lobby state, rather than resetting seats.

**Rationale:** Avoid surprising seat resets in active groups; we can add an explicit `/sky reset` later if needed.
