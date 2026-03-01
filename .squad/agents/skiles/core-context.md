# Skiles — Core Context (Summarized from Sessions 1–22)

## Identity
**Role:** Implementation Lead / Domain Developer  
**Focus:** Feature vertical slices, WebApp endpoints, Telegram transport, test harness  
**Responsibilities:** Issue implementation, sprint execution, transport adapters, DDD boundary enforcement

## Current Status (2026-03-02)
- ✅ Issue #80: Durable persistence slice (Game state survives restart + version field tracking)
- ✅ Issue #77: Open App Launchpad hardening (startapp deep links + safe fallback)
- ✅ Issue #83: Async turn notifications (DM-first + group fallback, idempotency guard)
- ✅ Issue #84: Abuse protection slice 1 (rate limiting + input validation; more work deferred)
- ✅ PR #87: Consolidated deliverables for #76, #85, #86 (259 tests passing)
- ⏳ Next: Full #80 implementation with Version field + TTL cleanup + database backend

## Implementation Patterns (Locked)

### Vertical Slice Architecture
- **Pattern:** Self-contained features from domain through Telegram/WebApp transport
- **Example #80:** Persistence port + in-memory + JSON repos + rehydration tests
- **Example #84:** Endpoint filters + rate limiting + logging; preserve domain purity
- **Benefit:** Parallel team work; low blast radius; reviewable changesets

### Persistence Design (Issue #80)
- **Unit:** GameSession with round logs (not snapshots)
- **Rehydration:** Replay round logs through existing RebuildDomainGameFromLogs pattern
- **State:** GameSessionPersistence port (Application layer) + JsonGameSessionPersistence impl (TelegramBot layer)
- **Cockpit Lifecycle:** Persist message IDs per game for edit-in-place refresh after restart
- **Version Field:** Added in full implementation (not in initial slice scope)

### WebApp Transport Layer
- **Pattern:** Thin adapters over in-memory stores; no side effects
- **Endpoints:** POST /api/webapp/lobby/{new|join}, GET /api/webapp/game-state
- **Auth:** TelegramInitDataValidator + constant-time HMAC comparison
- **Filtering:** Abuse protection (rate limiting) + input validation (oversized headers, invalid commands)
- **Logging:** Throttled requests + rejected initData attempts

### Telegram UX Patterns
- **Cockpit:** Single edited group message (refresh via callback or /sky command)
- **Callbacks:** Versioned action tokens (v1:action:index) for 64-byte constraint
- **Notifications:** DM-first for active turns; group fallback for delivery
- **Idempotency:** In-memory dedup cache (groupChatId + transitionKey + recipientUserId + seat)

## Abuse Protection Strategy (Issue #84, Slice 1)
- **Rate Limits:** Per-user 10 req/sec, per-IP 100 req/min, lobby creation 1 req/user/5min
- **Input Validation:** Oversized headers (>4096), invalid commandId (max 128, no whitespace), invalid names (max 64)
- **Logging:** Throttled requests with scope/key/path/retry-after; rejected initData with validation status
- **Deferred:** Expand to /sky commands + callback flood, per-game idempotency-key policy, distributed limiters

## Test Patterns
- **Framework:** xUnit + FluentAssertions
- **Integration:** WebApplicationFactory with in-memory stores (no Telegram polling flakiness)
- **Round-Trip:** Serialize/deserialize game state; validate deterministic rehydration
- **Concurrency:** (Deferred to #82) Parallel placement rejection + version conflict handling

## Critical Path Deliverables
- **#76** ✅ WebAppOptionsValidator + DI registration + readme docs
- **#77** ✅ startapp deep links + safe fallback behavior
- **#80** ⏳ Full persistence (Version field, TTL, database) — Sully architecture review pending
- **#81** ⏳ Security-context-binding (Sully design review)
- **#82** ⏳ Versioning APIs + concurrency test harness (Sully contract first)

## Design Principles (DDD-Aligned)
1. **Domain Purity:** No I/O, no framework types, no logging inside aggregates/value objects
2. **Port Pattern:** Infrastructure concerns abstracted as ports (GameSessionPersistence, IEndpointFilter)
3. **Transport Adaptation:** Telegram/WebApp specifics confined to Host layer
4. **Immutability:** Value objects use record/readonly; state transitions via new aggregate instances
5. **Early Returns:** Guard clauses prevent deep nesting; fail fast on invalid input
6. **Composition:** Filters chain; stores nest in DI; no hidden side effects

## Learnings
1. **Replay-Based Persistence:** Round logs superior to snapshots (smaller, auditable, existing pattern reuse)
2. **Callback Versioning:** Short token codec (v1:action:index) fits 64-byte Telegram constraint cleanly
3. **Idempotency Guards:** In-memory dedup + dedup cache prevents duplicate notifications on retry
4. **Deterministic Tests:** WebApplicationFactory removes flakiness; integration tests fast + reliable
5. **Abuse Protection Layers:** Filter-based guardrails preserve DDD boundaries while delivering practical safety
6. **Edit-in-Place Cockpit:** Persisting message IDs enables edit-first refresh after restart; robust UX

## Decision Artifacts
- `.squad/decisions.md` — Merged decisions for #80, #77, #83, #84
- GitHub issues #76–#84 — Implementation slices with acceptance criteria
- `.squad/orchestration-log/` — Per-round logs

## Next Actions
1. Await Sully #80 architecture review (Version field + TTL + database shape)
2. Implement full #80 (add Version field, TTL cleanup job, database backend)
3. Begin #81 security-context-binding (after Sully design)
4. Expand #84 abuse protection (reach #81 completion, then expand to /sky + callbacks)
5. Prepare #82 versioning API implementation (after Sully contract)
