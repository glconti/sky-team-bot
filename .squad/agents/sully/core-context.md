# Sully — Core Context (Summarized from Sessions 1–12)

## Identity
**Role:** Lead Architect  
**Focus:** Domain architecture, API design, Epic leadership, PR review gates  
**Responsibilities:** Architecture review, issue triage, decision records, critical path coordination

## Current Status (2026-03-02)
- ✅ Issue #80: Game Persistence (schema migration + TTL confirmed)
- ✅ Issue #81: Security-context-binding (InvalidGameContext invariants + regression coverage)
- ✅ Issue #82: Versioning/Concurrency APIs (expectedVersion + 409 ConcurrencyConflict)
- ✅ Epic #75: 7/11 child issues complete (#76, #80, #81, #82, #83, #85, #86)
- ✅ Critical Path Gate #81: CLOSED
- **Next Priority:** #77 (UI Slice — Place/Undo); #84 (Abuse Protection expansion)

## Architectural Decisions (Locked)

### Mini App Strategy
- **Primary UI:** Telegram Mini App (WebApp) as main interface
- **Secondary Channel:** Group chat is launch surface only (Open app button with startapp deep link)
- **Secrets:** All sensitive data (dice hand, placements) stay inside Mini App; no DM leaks
- **Architecture:** 5-layer separation (Domain → Application → Presentation → Adapter → Host)

### Persistence + Concurrency (Co-Design Mandate)
- **Foundation:** Version field (int, starts at 1) on GameSessions aggregate
- **Serialization:** Persist round logs, not snapshots; replay existing RebuildDomainGameFromLogs pattern
- **Concurrency:** Optimistic locking with compare-and-swap semantics; HTTP 409 ConcurrencyConflict response
- **Repository Interface:** CreateAsync, UpdateAsync (with expectedVersion), GetByIdAsync, GetByGroupChatIdAsync
- **TTL:** Active games no expiry; completed/abandoned 30-day retention

### WebApp Configuration Validation
- **Runtime Check:** HTTPS-only URLs; no query/fragment allowed (per BotFather spec)
- **Early Failure:** DI ValidateOnStart() prevents misconfiguration at startup
- **Operator Docs:** Checklist in readme.md with BotFather commands + environment setup

## Critical Path (Locked)
1. **#76** ✅ BotFather config validation (complete)
2. **#80** ✅ Game Persistence (schema + TTL + migration complete)
3. **#81** ✅ Security-context-binding (InvalidGameContext invariants enforced; gate closed)
4. **#82** ✅ Versioning/Concurrency APIs (complete)
5. **#77** ⏳ Open App Launchpad hardening (awaiting UI implementation; persistence/security stack unblocked)
6. **#78–#79** ⏳ Mini App UI (ready after #77 completion)

## Team Coordination Model
- **Skiles:** Implementation (carries critical path work)
- **Aloha:** QA/Testing (deterministic integration tests + manual matrices)
- **Tenerife:** Rules validation (consulted on game-state exposure)
- **Ralph:** Work monitor (validates closures, gates PR merges)

## Learnings
1. **Configuration Validation as Gating:** Early DI validation prevents silent production failures
2. **Deterministic Integration Tests:** WebApplicationFactory + in-memory removes flakiness
3. **QA Matrix as Living Doc:** Tester-focused matrix in readme.md beats static spreadsheets
4. **Persistence ≠ Concurrency:** Must design together as atomic unit; cannot separate
5. **Epic Chaining:** Requires upfront design of foundational layers to prevent rework
6. **Issue Comments with PR Cross-Links:** Improves team context; enables automatic closure on merge
7. **Schema Migration Closure:** Idempotent startup migration satisfies DB artifacts without runtime rewrite
8. **Security Outcome Granularity:** Explicit InvalidGameContext prevents ambiguity vs. collapsed authorization
9. **Audit Cadence:** Regular scope audits validate critical path alignment and catch acceptance gaps early
10. **Gate Closure Impact:** Closing #81 security gate unblocks UI (#77–#79) and abuse protection (#84) for parallel execution

## Decision Artifacts
- `.squad/decisions.md` — Merged inbox decisions for #76–#84 + #81 closure
- `.squad/orchestration-log/` — Per-agent timestamped logs
- GitHub issues #75–#86 — Epic + child issues with architecture review gates

## Next Actions (Updated 2026-03-02 Round 16)
1. ✅ Epic #75 critical path gate #81 closed; #77–#79 (UI) unblocked
2. **Next Priority:** #77 Open App launchpad UI implementation; #84 abuse protection expansion
3. Coordinate team prioritization for #77 UI slice (Place/Undo buttons + turn summary display)
