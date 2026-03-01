# Skiles Decision Inbox — Issue #83

## Decision
Implement a first async turn-notification slice in `SkyTeam.TelegramBot` with **DM-first + group fallback** policy and transition-key deduplication.

## Why
- Fastest path to unblock async-play feedback without introducing new domain event infrastructure mid-branch.
- Reuses existing Telegram integration seams (`TelegramBotService`, WebApp endpoint callbacks, in-memory session state).
- Keeps secret information safe by sending only public turn summary + action-required prompt.

## Implemented Scope
1. Added turn notification orchestration in `TelegramBotService`:
   - Triggered from successful round roll (group command/callback path).
   - Triggered from successful WebApp roll/place/undo flows via `NotifyCurrentTurnFromWebAppAsync`.
2. Added idempotency guard:
   - In-memory dedup key: `groupChatId + transitionKey + recipientUserId + seat`.
   - Bounded key cache to avoid unbounded growth.
3. Documented policy in `readme.md` under **Async turn notification policy (Issue #83, slice 1)**.

## Remaining for Full Issue #83
- Emit and consume a formal application/domain turn-transition event (current slice is transport-driven).
- Add reminder timeout flow and analytics/read-receipt behavior if product confirms.
- Add integration tests for notification delivery and no-secret-content assertions.
