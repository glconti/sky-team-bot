# 2026-02-22: Issue #52 Slice 3 — Lobby cockpit button semantics

**By:** Skiles (Domain Dev)  
**Context:** Implementing lobby cockpit buttons (`New`, `Join`, `Start`, `Refresh`) in group chat while preserving `/sky` command fallback behavior.

## Decision
- Group cockpit always renders all four lobby controls: `New`, `Join`, `Start`, `Refresh`.
- Buttons are pressable by any group member; legality is enforced server-side in callback handlers via existing `InMemoryGroupLobbyStore` and `InMemoryGroupGameSessionStore` operations.
- Invalid callback actions are handled as no-op + toast via `AnswerCallbackQuery` text (no group message spam, no cockpit mutation).
- Successful callback actions refresh cockpit through the existing edit-first lifecycle (`RefreshGroupCockpitAsync`), preserving single-cockpit-message behavior.
- `/sky new|join|start` fallback commands remain supported and continue to refresh cockpit state.

## Rationale
- Aligns with Epic #49 constraints: visible/pressable group controls, server-side authorization, graceful callback failure, and text-command regression safety.
- Keeps implementation minimal by reusing current lobby/session command paths and cockpit refresh pipeline.

## Implications
- Callback toasts now carry user-facing legality feedback for lobby actions.
- Cockpit button surface is stable while future slices can add in-game controls without changing this contract.
