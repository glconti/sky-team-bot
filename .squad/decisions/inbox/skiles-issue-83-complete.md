# Skiles — Issue #83 Practical Completion

**Date:** 2026-03-02  
**Issue:** #83  
**PR:** #87 (https://github.com/glconti/sky-team-bot/pull/87)

## Decision
Keep the transport-driven async turn notification slice and harden it for real gameplay by:
1. Resetting group-scoped notification dedup history when a new game starts in that group.
2. Making DM→group fallback notification delivery best-effort (log on failure, never break gameplay flow).
3. Locking behavior with focused regression checks and updating operator-facing policy notes in `readme.md`.

## Why
- Existing dedup keys can repeat across separate games in the same group, which suppresses valid notifications after restart/new game.
- Notification transport errors should not fail successful roll/place/undo transitions.
- This closes practical reliability gaps without introducing a larger event-infrastructure refactor mid-branch.

## Completed Scope
- `TelegramBotService` now clears stale turn-notification dedup entries for a group when `GameSessionStartStatus.Started`.
- Group fallback send path now uses guarded helper with warning logging.
- Added `Issue83AsyncTurnNotificationTests` and policy updates in `readme.md`.

## Remaining Scope (deferred)
- Formal `TurnTransitioned` application/domain event wiring.
- Reminder timeout behavior and analytics/read-receipt policy.
- Live Telegram client latency QA for notification delivery.
