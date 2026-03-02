# Decision: Issue #80 schema/migration closure slice

## Context
- Issue #80 required an explicit `GameSessions` schema + migration artifact.
- PR #87 already delivered durable JSON replay persistence, repository contract, versioning, and lifecycle retention.
- Remaining blocker was the missing database schema/migration criterion.

## Decision
- Add an idempotent SQLite schema migration (`0001_game_sessions_schema`) as a production startup concern in `JsonGameSessionPersistence`.
- Keep JSON replay persistence as the active runtime source of truth for session state (no storage-engine rewrite in this slice).

## Implemented Artifacts
- SQL migration artifact: `SkyTeam.TelegramBot/Persistence/Migrations/0001_game_sessions_schema.sql`
- Migration runner: `SkyTeam.TelegramBot/Persistence/GameSessionsSchemaMigrator.cs`
- Runtime trigger + config: `JsonGameSessionPersistence` + `Persistence:GameSessionsDatabasePath`
- Evidence test: `Load_ShouldApplyGameSessionsSchemaMigration_WhenPersistenceIsInitialized`

## Consequences
- #80 now has an explicit, runtime-applied `GameSessions` schema migration with `Version`, lifecycle timestamps, and active-session uniqueness on `GroupChatId`.
- The change is minimal-risk and additive; existing JSON persistence behavior remains unchanged.
- A future DB-backed repository migration can reuse the same `GameSessions` schema artifact.
