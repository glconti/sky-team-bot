# Skiles — Issue #84 Abuse Protection Slice 1 (2026-03-02)

## Context
Issue #84 requires abuse/rate limits and input validation for player-facing surfaces.  
This first slice is intentionally in-process and minimal to fit the current architecture on PR #87.

## Decision
Implement lightweight WebApp transport guardrails via endpoint filters + in-memory sliding windows, without introducing external infrastructure packages yet.

### Implemented
1. **Rate limiting (`429` + `Retry-After`)**
   - Per-user: **10 requests / second**
   - Per-IP: **100 requests / minute**
   - Lobby creation (`POST /api/webapp/lobby/new`): **1 request / user / 5 minutes**
2. **Input validation (`400`)**
   - Reject oversized `X-Telegram-Init-Data` headers (`> 4096` chars)
   - Validate `commandId` for placement: required, trimmed, max 128 chars, no whitespace
   - Validate join display name: non-empty after trim, max 64 chars
3. **Abuse logging**
   - Log throttled requests (scope/key/path/retry-after)
   - Log rejected `initData` requests with validation status

## Why this slice
- Delivers practical guardrails immediately on active traffic paths.
- Preserves current DDD boundaries (domain untouched; transport/presentation hardening only).
- Keeps blast radius low while providing concrete abuse controls now.

## Remaining for full issue #84
- Expand throttling to additional channels (`/sky` command spam and callback flood patterns).
- Add per-game action cadence/idempotency-key policy for write endpoints.
- External/distributed limiter backend for multi-instance deployments.
- Broader abuse telemetry/alerting policy beyond warning logs.
