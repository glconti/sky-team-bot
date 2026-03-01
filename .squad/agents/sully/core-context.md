# Sully — Core Context (Summarized from Sessions 1–12)

## Identity
**Role:** Lead Architect  
**Focus:** Domain architecture, API design, Epic leadership, PR review gates  
**Responsibilities:** Architecture review, issue triage, decision records, critical path coordination

## Current Status (2026-03-02)
- ✅ Issue closure round 8: Closed #76 (BotFather config), #85 (WebApp tests), #86 (QA matrix)
- ✅ Epic #75 progress: 4/11 child issues complete; critical path gates established
- ⏳ Next: Begin #81 security-context-binding design review
- ⏳ Pending: Review #82 versioning API contract before #78–#79 UI integration

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
2. **#77** ✅ Open App Launchpad hardening (merged in PR #87)
3. **#80** ⏳ Game Persistence (in progress; architecture contract delivered)
4. **#81** ⏳ Security-context-binding (design pending)
5. **#82** ⏳ Versioning/Concurrency APIs (design pending)
6. **#78–#79** ⏳ Mini App UI (blocked until #80–#82 complete)

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

## Decision Artifacts
- `.squad/decisions.md` — Merged inbox decisions from all team agents
- `.squad/orchestration-log/` — Per-agent timestamped logs
- GitHub issues #75–#86 — Epic + child issues with architecture review gates

## Next Actions
1. Begin #81 security-context-binding contract (before Skiles implements)
2. Review #82 versioning API shape (expectedVersion parameter, ConcurrencyConflict response)
3. Validate Skiles #80 persistence implementation (Version field, TTL, repository interface)
4. Approve #82 test suite design (6 required tests: concurrency, persistence, version, conflicts, TTL)
