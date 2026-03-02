# Skiles Decision — Issue #80 Remediation (PR #87)

## Context
Aloha QA marked #80 as not close-ready due to missing explicit repository contract artifacts, missing lifecycle cleanup policy clarity, and lack of concrete file-backed restart evidence.

## Decision
Keep JSON replay persistence architecture for #80 slice, but close the contract gaps by formalizing repository-style operations on the existing application persistence port:
- `Create`
- `Update(expectedVersion)`
- `GetById`
- `List`
- `CleanupExpired(utcNow)`

Add persisted lifecycle metadata (`CreatedAtUtc`, `UpdatedAtUtc`, `ExpiresAtUtc`) per session and apply retention cleanup during persistence load/save cycles, with runtime-configurable retention days:
- `Persistence:CompletedSessionRetentionDays` (default 30)
- `Persistence:AbandonedSessionRetentionDays` (default 30)

## Why
This is the smallest safe implementation set that materially advances close-readiness without re-architecting to a DB migration in the same round.

## Evidence Added
- Contract behavior tests for JSON repository operations and lifecycle cleanup.
- Host-level file-backed restart integration test proving game state rehydration across factory restart.

## Remaining Scope (if strict original issue text is enforced)
- Database schema + migrations are still not introduced in this remediation slice.
- If ownership insists on DB-only acceptance wording, issue text should be updated to align with the approved JSON replay persistence pattern or split DB migration into follow-up work.
