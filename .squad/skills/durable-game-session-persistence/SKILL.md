# Skill: Durable Game Session Persistence (Replay + Atomic Snapshot)

## Context
Use this pattern when an in-memory game/session store must survive process restarts without leaking persistence concerns into the domain model.

## Pattern
1. **Define an application-layer persistence port** (e.g., `IGameSessionPersistence`) with explicit snapshot DTOs.
2. **Persist replayable round logs** (rolled dice + executed command ids + die indexes) instead of trying to serialize domain internals directly.
3. **Rehydrate by replay**:
   - Recreate a fresh domain aggregate.
   - Replay round logs in order.
   - Rebuild current turn state from the active round log.
4. **Persist metadata needed by adapters** (e.g., cockpit message ids) alongside session state.
5. **Write snapshots atomically** (`temp file` + `move/replace`) to avoid torn files on crashes.
6. **Persist a Version field** on snapshots so optimistic locking can be layered later (CAS in #82-style work).

## Why it works
- Keeps domain pure and deterministic.
- Minimizes serialization coupling to rich aggregate internals.
- Makes restart behavior reliable with small, explicit contracts.
- Future-proofs concurrency work by carrying version in the persistence schema early.

## QA close-ready gate
Before marking a durability issue close-ready, validate both:
1. **Behavior evidence:** rehydration round-trip + stale-write/version-conflict tests are green.
2. **Contract evidence:** implementation artifacts match issue scope (storage contract, lifecycle policy like TTL, and documented operational behavior).

If behavior tests pass but contract artifacts are missing, issue a **not close-ready** verdict with an explicit remaining checklist.

## Contract closure add-on (Issue #80 remediation)
When QA requires explicit repository completeness without a storage-engine rewrite:
1. Extend the persistence port with repository-style operations (`Create`, `Update(expectedVersion)`, `GetById`, `List`, `CleanupExpired`).
2. Add per-session lifecycle timestamps (`CreatedAtUtc`, `UpdatedAtUtc`) plus computed `ExpiresAtUtc`.
3. Enforce cleanup on load/save so restart and steady-state paths apply the same retention policy.
4. Add one host-level file-backed restart integration test (same persistence file, new host instance) to prove real rehydration behavior.

## Schema migration gate add-on (Issue #80 closure)
When acceptance criteria explicitly require a database schema/migration but runtime persistence is already stable:
1. Add a concrete SQL migration artifact with required columns and indexes (for SkyTeam: `Version`, lifecycle timestamps, active-session uniqueness).
2. Embed the SQL artifact and run it idempotently at startup through a tiny migration runner.
3. Keep the existing runtime persistence path unchanged to minimize production risk.
4. Add a runtime evidence test that verifies migration row + table shape + required indexes; clear SQLite pools in fixture teardown to avoid locked-file cleanup failures.
