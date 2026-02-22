# Skiles — Issue #56 PR

- PR: https://github.com/glconti/sky-team-bot/pull/74
- Branch: `skiles/issue-56-callback-hardening`
- Draft: `false` (ready for review)
- Mergeability: `clean`

## Acceptance Summary
- Introduced `CallbackDataCodec` with canonical versioned callback format `v1:grp:<action>` and validation.
- Added `CallbackMenuStateStore` with 1-hour TTL cleanup, callback allow-list binding, and duplicate-delivery dedup handling.
- Hardened `TelegramBotService` callback processing to reject malformed/expired callbacks and return clear toasts instead of throwing.
- Added issue #56 tests covering codec round-trip/rejection and menu-state chat-binding, dedup, and TTL expiry.

Closes #56