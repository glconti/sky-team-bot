# Skiles — Issue #81 first implementation slice (2026-03-02T01:25:00Z)

## Context
Issue #81 requires enforcing chat/game binding so a user cannot mutate a game outside the chat context that originated the action.

## Decision
For this first slice, enforce binding at the application boundary by requiring explicit `(groupChatId, userId)` on game mutation methods and use signed WebApp chat context to call those methods.

## Done in this slice
- Added context-bound mutation overloads in `InMemoryGroupGameSessionStore`:
  - `PlaceDie(long groupChatId, long requestingUserId, ...)`
  - `UndoLastPlacement(long groupChatId, long requestingUserId)`
- Kept legacy user-only overloads as compatibility wrappers.
- Updated WebApp mutation endpoints to pass resolved `groupChatId`:
  - `POST /api/webapp/game/place`
  - `POST /api/webapp/game/undo`
- Documented security invariant in store class XML summary.
- Added tests for:
  - multi-session same-user routing bound to requested chat
  - cross-chat mutation rejection when user is not seated in requested session
  - WebApp integration path for context-bound placement

## Remaining scope
- Propagate explicit context-bound mutation calls to any future non-WebApp mutation surfaces.
- Consider replacing single `userId -> groupChatId` index with multi-chat membership mapping.
- Decide whether product/API should expose a dedicated `InvalidGameContext` status instead of current `NotSeated` conflict semantics.
