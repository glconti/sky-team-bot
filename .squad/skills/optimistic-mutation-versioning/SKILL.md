# Skill: Optimistic mutation versioning (CAS)

## Context
Use this when mutation APIs must reject stale client writes safely in multi-actor game flows.

## Pattern
1. Include `Version` in read models used by mutating clients.
2. Accept `expectedVersion` on mutation operations (`Roll`, `Place`, `Undo`).
3. Before any mutation side effect, compare `expectedVersion` with current session version.
4. On mismatch, return explicit conflict status and include `CurrentVersion`.
5. Map conflict status to transport `409 Conflict` with a stable payload (`ConcurrencyConflict` + `CurrentVersion` + retry message).
6. Increment version only after successful state mutation and persistence.

## Why it works
- Prevents stale overwrites deterministically without locks.
- Gives clients a safe retry protocol (refresh state, then retry with fresh version).
- Keeps domain pure by enforcing concurrency at application/transport boundaries.
