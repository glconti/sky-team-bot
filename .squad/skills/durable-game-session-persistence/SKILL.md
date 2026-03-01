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
