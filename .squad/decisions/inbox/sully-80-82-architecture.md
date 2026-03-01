# Architecture Decision: Durable Persistence + Concurrency (#80 + #82)

**Author:** Sully (Lead/Architect)  
**Date:** 2026-03-02  
**Status:** Approved for Implementation  
**Epic:** #75 (Mini App-first Async Play)

---

## Problem Statement

Issues #80 (durable persistence) and #82 (concurrent action handling) are **architecturally inseparable**. You cannot persist game state without versioning; you cannot version without persistence. This decision treats them as a single design unit.

**Current State:**
- `InMemoryGroupGameSessionStore` holds all game state in-memory
- No durability across restarts
- No optimistic locking; race conditions possible in async play
- `GameSession` is a private nested class with complex reconstruction logic (`RebuildDomainGameFromLogs`)

---

## Architecture Contract

### 1. Game Aggregate Persistence Schema

```
GameSessions table:
┌─────────────────────────────────────────────────────────────────────────┐
│ Column          │ Type       │ Notes                                   │
├─────────────────────────────────────────────────────────────────────────┤
│ GameId          │ UUID       │ PK; immutable; generated on create      │
│ GroupChatId     │ long       │ Telegram group; indexed; unique active  │
│ PilotUserId     │ long       │ Telegram user ID                        │
│ CopilotUserId   │ long       │ Telegram user ID                        │
│ State           │ JSON/BLOB  │ Serialized GameSession (see below)      │
│ Status          │ string     │ InProgress | Won | Lost | Abandoned     │
│ Version         │ int        │ Monotonic; starts at 1; CAS field       │
│ CreatedAt       │ DateTime   │ UTC                                     │
│ UpdatedAt       │ DateTime   │ UTC; updated on every mutation          │
│ ExpiresAt       │ DateTime?  │ TTL for cleanup; null = no expiry       │
└─────────────────────────────────────────────────────────────────────────┘
```

**Invariants:**
- `Version` increments atomically on every state mutation
- Only one active session per `GroupChatId` (enforced by unique index + status filter)
- `State` contains full reconstructable game state (round logs, turn state, domain snapshots)

### 2. Optimistic Locking (Compare-and-Swap)

Every mutation operation follows this pattern:

```csharp
public interface IGameSessionRepository
{
    Task<GameSession?> GetByGroupChatIdAsync(long groupChatId, CancellationToken ct);
    Task<GameSession?> GetByIdAsync(Guid gameId, CancellationToken ct);
    
    /// <summary>
    /// Creates a new session. Fails if active session already exists for GroupChatId.
    /// </summary>
    Task<CreateResult> CreateAsync(GameSession session, CancellationToken ct);
    
    /// <summary>
    /// Updates session state. Fails with ConcurrencyConflict if Version mismatch.
    /// Caller must reload and retry on conflict.
    /// </summary>
    Task<UpdateResult> UpdateAsync(GameSession session, int expectedVersion, CancellationToken ct);
}

public enum CreateResult { Created, AlreadyExists }
public enum UpdateResult { Updated, ConcurrencyConflict, NotFound }
```

**Concurrency Contract:**
- `UpdateAsync` performs `WHERE GameId = @id AND Version = @expectedVersion`
- If 0 rows affected → return `ConcurrencyConflict`
- Caller receives current Version in error response for retry
- No deadlocks; optimistic locking only

### 3. State Serialization

Serialize the following (NOT the domain Game object directly):

```csharp
public sealed record PersistedGameState(
    Guid GameId,
    long GroupChatId,
    LobbyPlayer Pilot,
    LobbyPlayer Copilot,
    int CurrentRoundNumber,
    GameRoundStatus RoundStatus,
    RoundTurnState? TurnState,
    List<PersistedRoundLog> RoundLogs,
    int? CockpitMessageId,
    string DomainGameStatus);

public sealed record PersistedRoundLog(
    int RoundNumber,
    int[] PilotDice,
    int[] CopilotDice,
    List<PersistedPlacement> Placements,
    bool IsCompleted);

public sealed record PersistedPlacement(string CommandId, string CommandDisplayName);
```

**Why logs, not snapshots:**
- Domain `Game` is reconstructed from logs (existing pattern in `RebuildDomainGameFromLogs`)
- Logs are append-only, immutable, auditable
- Smaller serialization footprint than full domain state

### 4. Concurrency Error Response

Return to client:

```csharp
public sealed record ConcurrencyConflictResponse(
    string Error = "ConcurrencyConflict",
    int CurrentVersion,
    string Message = "Game state changed. Please refresh and retry.");
```

**Client contract:**
- On `409 Conflict`: fetch latest state, rebuild UI, allow user to retry
- WebApp: show toast "Someone else made a move. Refreshing..."

### 5. TTL Cleanup Policy

- **Active games:** No TTL (ExpiresAt = null)
- **Abandoned games:** Set `ExpiresAt = UpdatedAt + 30 days` when `Status = Abandoned`
- **Completed games (Won/Lost):** Set `ExpiresAt = UpdatedAt + 30 days`
- **Cleanup job:** Background service; runs daily; deletes `WHERE ExpiresAt < NOW()`

---

## Required Invariants (Review Gates)

### For Skiles (Implementation)

1. **Version field is non-nullable int**, starts at 1, increments on every `UpdateAsync` call
2. **Serialization uses System.Text.Json** with `JsonSerializerOptions` for camelCase
3. **No direct writes to storage** — all mutations go through repository
4. **Transaction boundary:** Single `UpdateAsync` call = atomic; no multi-row transactions needed
5. **Index:** Unique index on `(GroupChatId) WHERE Status = 'InProgress'`

### For Aloha (Testing)

1. **Concurrency test:** Two parallel `PlaceDie` calls → exactly one succeeds, one returns `ConcurrencyConflict`
2. **Persistence test:** Create game → restart application → load game → verify state matches
3. **Version test:** Load session (v1) → mutate → save (v2) → load again → confirm v2
4. **TTL test:** Set `ExpiresAt` to past → run cleanup → verify deleted
5. **Edge case:** Simultaneous `Start` calls → only one creates; other returns `AlreadyStarted`

---

## Migration Path (Phase 1 → Phase 2)

### Phase 1: In-Memory Repository with Versioning
- Add `Version` field to `GameSession`
- Implement `InMemoryGameSessionRepository` with CAS semantics
- Update all mutation methods to check/increment version
- **No database yet** — validates concurrency logic in isolation

### Phase 2: Durable Repository
- Introduce SQLite/PostgreSQL storage (decision deferred)
- Implement `DatabaseGameSessionRepository` with same interface
- Add JSON serialization/deserialization
- Add cleanup background service

---

## Handoff Checklist

### Skiles: Implementation Ready

- [ ] Add `Version` property to `GameSession` (or new `PersistedGameSession` type)
- [ ] Create `IGameSessionRepository` interface per contract above
- [ ] Implement `InMemoryGameSessionRepository` with optimistic locking
- [ ] Refactor `InMemoryGroupGameSessionStore` to use repository pattern
- [ ] Add serialization helpers for `PersistedGameState`
- [ ] Update `PlaceDie`, `UndoLastPlacement`, `RegisterRoll` to use version check
- [ ] Return `ConcurrencyConflict` from API when version mismatch detected

### Aloha: Test Coverage Required

- [ ] `ConcurrentPlacement_ShouldRejectSecond_WhenVersionStale`
- [ ] `GameSession_ShouldPersistAndRestore_AfterApplicationRestart` (mock restart)
- [ ] `Version_ShouldIncrement_OnEveryMutation`
- [ ] `CreateSession_ShouldFail_WhenActiveSessionExists`
- [ ] `UpdateSession_ShouldFail_WhenSessionNotFound`
- [ ] `TTLCleanup_ShouldRemoveExpiredSessions`

---

## Rejected Alternatives

1. **Pessimistic locking (row locks):** Adds latency; unnecessary for turn-based game with low contention
2. **Event sourcing:** Over-engineered for current scope; logs-as-events is sufficient
3. **Distributed locks (Redis):** Single-host deployment; not needed
4. **Snapshot-only persistence:** Loses audit trail; harder to debug state corruption

---

## Summary

Persistence and concurrency form a single architectural unit. Version field is the foundation of both. Implementation follows repository pattern with optimistic locking. No database decision required for Phase 1 — validate design with in-memory CAS first.

**Approval:** ✅ Architecture approved for implementation.
