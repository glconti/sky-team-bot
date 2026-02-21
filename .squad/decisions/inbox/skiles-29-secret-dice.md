# Decision: Secret dice roll via DM (Issue #29)

## Context
We need to keep dice values secret between seated players while still rolling from the group chat.

## Decision
- Add a `/sky roll` group command.
- On roll, generate 4 dice values (1–6) for each seat and DM them to the seated Pilot/Copilot.
- If a DM fails (player has not `/start`ed the bot privately), notify the group without revealing any dice values.

## Rationale
This keeps the change minimal and testable (pure application dice-roll logic), while matching Telegram constraints around private messaging.
