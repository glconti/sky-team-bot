# PR #74 Cycle — Issue #56 Callback Hardening

**Branch:** `skiles/issue-56-callback-hardening`
**Issue:** #56 — Harden callback_data + menu state store
**Commits:** `7ffaec0` (feat), `75946f3` (docs)
**Timeline:**
- Sully review approved ✅
- Coordinator merged to master
- Issue #56 closed

## Changes
- **CallbackDataCodec:** Versioned codec with v1:grp:<action> format, 64-byte max enforcement
- **CallbackMenuStateStore:** State dedup with 1-hour TTL, cross-chat isolation, GC on operation
- **TelegramBotService:** Graceful unknown/expired handling; no-throw on invalid callbacks
- **Tests:** Issue52LobbyButtonFlowTests updated; Issue56CallbackHardeningTests added

## Acceptance Criteria (All Met ✅)
1. Documented callback_data format (versioned, ≤64 bytes) → codec enforces via TryDecodeGroupAction
2. Central encoder/decoder used by all callbacks → EncodeGroupAction / TryDecodeGroupAction in HandleCallbackQueryAsync
3. Menu state store (user/chat/menuVersion keyed, TTL + GC, cross-user safe) → CallbackMenuStateStore with 1-hour default, GC on every op
4. Duplicate callback delivery safe (dedup/idempotent) → ValidateAndMarkProcessed returns Duplicate, service returns toast
5. Unknown/expired never throw → toast returned, no exceptions
6. No domain rule changes → zero changes to SkyTeam.Domain or SkyTeam.Application

## Test Results
- Domain: 145 passed, 0 failed
- Application: 99 passed, 16 skipped, 0 failed
- **Total: 244 passed, 16 skipped, 0 failed ✅**

## Quality Notes
- Clean separation: codec vs state store
- Thread-safe lock discipline
- Time abstraction enables deterministic TTL tests
- Guard clauses on all public methods
- Non-blocking observations: switch pattern trade-off acceptable; RegisterGroupMenu eviction documented; EncodeGroupAction validation deferred to future pass

## Merge Status
✅ Ready to merge. All criteria met, tests green, clean mergeable state.

---
*Scribe: Orchestration logged by session.*
