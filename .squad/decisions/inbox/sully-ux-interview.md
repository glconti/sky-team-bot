# Sully — UX interview decisions (Telegram)

## Context
Confirmed interview answers impacting the Telegram UX epic (#49) and its child issues.

## Decisions
1. **Group chat UX is a single Cockpit message**
   - The bot maintains one cockpit message per group chat and **edits** it on state changes (no chat spam).

2. **Group cockpit buttons are pressable by anyone**
   - Buttons are visible to all group members and **anyone may press**.
   - The server enforces seating/turn/permission rules; invalid presses are **no-op + toast**.

3. **“Placement from group cockpit” is a group button that drives a private DM UI**
   - The group cockpit includes a **Place (DM)** action that **triggers/refreshes the pressing user’s DM placement UI**.
   - The group chat must not reveal private placement info: **no secret dice** and **no command IDs**.

## Implications
- Callback routing/validation must bind actions to the pressing user and current game state.
- Any payload/state that would reveal private information must remain server-side or within DM-only messages.
