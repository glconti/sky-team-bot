# Issue #30 — Telegram public placements + alternation (Skiles)

**By:** Skiles  
**Date:** 2026-02-21

## Decision
- Implement per-placement flow via **private chat** commands: `/sky place <dieIndex> <module/slot>` (no inline keyboard yet).
- Store the user-selected **placement target** (module/slot string) alongside application `RoundTurnState` placements so the bot can broadcast each reveal in the group.
- Enforce strict alternation by treating `RoundTurnState.CurrentPlayer` as the single source of truth.

## Rationale
This is the smallest change consistent with the current `/sky` command handler patterns while keeping dice hands secret and ensuring every placement is announced publicly in the group chat.
