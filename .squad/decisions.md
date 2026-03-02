# Decisions

> Append-only ledger of team decisions. Never retroactively edit entries.

## 2026-03-02T01:35:00Z — Issue #80 Closure Audit (Sully)

**Issue:** https://github.com/glconti/sky-team-bot/issues/80  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Commit:** `8bd9d1d` (branch `feat/issue-76-85-botfather-config-webapp-tests`)

**Finding:** Outstanding blocker — Game aggregate schema + migration not yet implemented.

### Deliverables Verified (Skiles Remediation)
✅ **Repository contract:** `IGameSessionPersistence` now exposes CRUD primitives (Create, Update, GetById, List, CleanupExpired)  
✅ **TTL/cleanup policy:** `Persistence:CompletedSessionRetentionDays` and `Persistence:AbandonedSessionRetentionDays` (defaults 30 days); documented in appsettings.json and readme.md  
✅ **Restart evidence:** `Issue80FileBackedRestartPersistenceTests` proves file-backed game state survives host restart  
✅ **Versioning:** Version field, optimistic locking (expectedVersion), conflict detection functional  

### Outstanding Criteria
❌ **Game aggregate schema + migration:** Schema/migration for GameSessions table not created; JSON persistence does not persist to database tables  

### Path to Closure
1. **Option A (DB):** Add GameSessions schema + migration (with version field + TTL metadata) to satisfy original issue wording.
2. **Option B (Scope revision):** Formally update issue #80 scope to embrace JSON-backed file persistence pattern as permanent solution.

**Current Status:** Issue #80 remains open; awaiting schema/migration decision or scope clarification.

---

## 2026-03-02T01:26:00Z — Issue #80 QA Validation Verdict (Aloha)

**Issue:** https://github.com/glconti/sky-team-bot/issues/80  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  

**Verdict:** Not close-ready (at time of audit).

### Test Coverage
- Focused #80 tests: 3/3 passed
- Full suite: 270/286 passed (16 skipped)

### Acceptance Criteria Status (Pre-remediation)
✅ Game state survives restart/reload path (store rehydration tested)  
✅ Version field supports optimistic locking (expectedVersion + VersionConflict verified)  
❌ Game aggregate schema defined and migrated to database  
❌ GameRepository CRUD/List contract implemented as written  
❌ TTL/cleanup policy implemented/documented  
❌ Integration persistence test verifies file-backed restart path end-to-end  

### Key Learning
Separate **behavior validation** (tests confirming rehydration + conflict mechanics work) from **contract validation** (issue-specified architecture artifacts). Passing tests alone insufficient for close-ready if acceptance criteria require missing deliverables.
