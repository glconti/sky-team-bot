# Skiles — Core Context (Summarized from Sessions 1–22)

## Identity
**Role:** Implementation Lead / Domain Developer  
**Focus:** Feature vertical slices, WebApp endpoints, Telegram transport, test harness  
**Responsibilities:** Issue implementation, sprint execution, transport adapters, DDD boundary enforcement

## Current Status (2026-03-02)
- ✅ Issue #80: Durable persistence slice (Game state survives restart + version field + migration)
- ✅ Issue #77: Open App Launchpad hardening + residual closure (startapp deep links + safe fallback + QA sign-off)
- ✅ Issue #81: Security-context-binding completion (InvalidGameContext explicit + tests)
- ✅ Issue #83: Async turn notifications (DM-first + group fallback, idempotency guard)
- ✅ Issue #84: Abuse protection slice 1 (rate limiting + input validation)
- ✅ PR #87: Consolidated deliverables; all tests passing
- ✅ Epic #75: 8/11 closed; all pre-UI gates CLOSED (#77, #80, #81)
- **Next Priority:** #78–#79 (UI Slices) — unblocked; #84 expansion deferred

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
- **#77** ✅ startapp deep links + safe fallback behavior; awaiting UI implementation
- **#80** ✅ Full persistence (Version field, TTL, database migration) — complete
- **#81** ✅ Security-context-binding complete (InvalidGameContext explicit, gate closed)
- **#82** ✅ Versioning APIs + concurrency — delivered in #81 round
- **#83** ✅ Async turn notifications — complete
- **#84** ✅ Abuse protection slice 1 — complete; more work deferred
- **#77–#79** Ready for UI implementation after #81 gate closed

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
7. **Schema Migration Closure:** Idempotent startup schema migration can satisfy DB artifact requirements without runtime persistence rewrite
8. **Security Outcome Granularity:** Explicit `InvalidGameContext` outcome prevents ambiguity at client/ops levels
9. **User-Chat Context Binding:** Cross-chat mutation detection best enforced at application boundary before domain mutations
10. **Distinct Failure Codes:** Enable deterministic tamper telemetry and proper corrective flows

## Decision Artifacts
- `.squad/decisions.md` — Merged decisions for #76–#84 + #81 closure
- GitHub issues #76–#84 — Implementation slices with acceptance criteria
- `.squad/orchestration-log/` — Per-round logs

## Next Actions (Updated 2026-03-02 Round 16)
1. ✅ Closed #80 persistence (Version field + TTL + database backend + migration)
2. ✅ Completed #81 security-context-binding (InvalidGameContext explicit + tests)
3. ✅ Epic #75 gate closed; #77–#79 (UI) unblocked
4. **Next Priority:** #77 (UI Slice — Place/Undo) ready for team prioritization
