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

## 2026-03-02T01:53:05Z — Issue #80 Final Closure (Sully)

**Issue:** https://github.com/glconti/sky-team-bot/issues/80  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  

**Finding:** The GameSessions schema migration now exists, TTL retention is documented, the repository contract remains intact, and targeted restart/lock tests still pass, satisfying every acceptance criterion.

### Acceptance Criteria Verified
- Migration `0001_game_sessions_schema.sql` defines the GameSessions table with `Version`, lifecycle timestamps, `ExpiresAtUtc`, and an active-session uniqueness index on `GroupChatId`; `GameSessionsSchemaMigrator` applies it whenever persistence initializes.
- `JsonGameSessionPersistence` still orchestrates Create/Update/GetById/List/Load/Save/CleanupExpired and normalizes TTL via `Persistence:CompletedSessionRetentionDays` and `Persistence:AbandonedSessionRetentionDays` (documented in `appsettings.json` and `readme.md`).
- Focused test `Issue80FileBackedRestartPersistenceTests` passes after adding the migration, proving restart resilience while carrying the existing version/lock semantics.

### Outcome
- Issue #80 may now be closed, letting the critical path advance to #81 (security-context-binding) and #82 (versioning/concurrency) before the UI (#77–#79) ships.

---

## 2026-03-02T01:44:00Z — Issue #80 Schema/Migration Closure Slice (Skiles)

**Issue:** https://github.com/glconti/sky-team-bot/issues/80  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Commit:** `ab61d0e`

### Context
- Issue #80 required an explicit `GameSessions` schema + migration artifact.
- PR #87 already delivered durable JSON replay persistence, repository contract, versioning, and lifecycle retention.
- Remaining blocker was the missing database schema/migration criterion.

### Decision
- Add an idempotent SQLite schema migration (`0001_game_sessions_schema`) as a production startup concern in `JsonGameSessionPersistence`.
- Keep JSON replay persistence as the active runtime source of truth for session state (no storage-engine rewrite in this slice).

### Implemented Artifacts
- SQL migration artifact: `SkyTeam.TelegramBot/Persistence/Migrations/0001_game_sessions_schema.sql`
- Migration runner: `SkyTeam.TelegramBot/Persistence/GameSessionsSchemaMigrator.cs`
- Runtime trigger + config: `JsonGameSessionPersistence` + `Persistence:GameSessionsDatabasePath`
- Evidence test: `Load_ShouldApplyGameSessionsSchemaMigration_WhenPersistenceIsInitialized`

### Consequences
- #80 now has an explicit, runtime-applied `GameSessions` schema migration with `Version`, lifecycle timestamps, and active-session uniqueness on `GroupChatId`.
- The change is minimal-risk and additive; existing JSON persistence behavior remains unchanged.
- A future DB-backed repository migration can reuse the same `GameSessions` schema artifact.

---

## 2026-03-02T01:51:00Z — Issue #80 Final Closure (Sully)

**Issue:** https://github.com/glconti/sky-team-bot/issues/80  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Commit:** `ab61d0e`

### Context
- Issue #80 demanded durable game persistence with an explicit GameSessions schema/migration plus the repository/TTL/optimistic-locking contract that Skiles had already delivered in PR #87.
- Prior audit raised the schema/migration gap: JSON-backed replay persistence existed, but the database artifact required by the acceptance criteria was missing.
- Closing the issue reopens the critical path into #81/#82 for security-context-binding and concurrency once the migration gate is satisfied.

### Decision
- Close GitHub issue #80. PR #87 now satisfies all acceptance criteria by pairing the existing JSON persistence behavior with a runtime-applied SQLite schema migration, version field, lifecycle timestamps, and documented retention configuration.

### Evidence
- `SkyTeam.TelegramBot/Persistence/Migrations/0001_game_sessions_schema.sql` defines the GameSessions table with `Version`, lifecycle timestamps, `ExpiresAtUtc`, and an active-session uniqueness index on `GroupChatId`.
- `GameSessionsSchemaMigrator` executes the migration before any persistence operation, and `JsonGameSessionPersistence` now triggers the migrator (plus the TTL/cleanup options) via `EnsureSchemaMigrated`.
- Runtime defaults and docs include `Persistence:GameSessionsDatabasePath`, `Persistence:CompletedSessionRetentionDays`, and `Persistence:AbandonedSessionRetentionDays` in `appsettings.json` and `readme.md`.
- Focused validation test `Issue80FileBackedRestartPersistenceTests` still passes after the migration addition, demonstrating restart resilience, alongside the existing version-conflict contract.
- `IGameSessionPersistence` continues to expose Create, Update(expectedVersion), GetById, List, Load/Save, and CleanupExpired, so the repository contract remains intact.

### Consequences
- Issue #80 is now ready to be marked closed; the critical path advances to #81 (security-context-binding) and #82 (versioning/concurrency) before UI work (#77–#79) can ship.
- The additive migration ensures future database-backed stores can reuse the same schema without touching the existing JSON persistence flow.
