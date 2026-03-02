# Sully — Core Context (Summarized from Sessions 1–12)

## Identity
**Role:** Lead Architect  
**Focus:** Domain architecture, API design, Epic leadership, PR review gates  
**Responsibilities:** Architecture review, issue triage, decision records, critical path coordination

## Current Status (2026-03-02)
- ✅ Issue closure round 8: Closed #76 (BotFather config), #85 (WebApp tests), #86 (QA matrix)
- ✅ Epic #75 progress: 7/11 child issues complete (#76, #80, #81, #82, #83, #85, #86); #77 still pending while #84 (abuse protection) and the UI slices wait for the remaining gates
- ✅ Issue #80 closure audit: GameSessions schema migration + TTL confirmed; JSON persistence contract validated
- ✅ Issue #81 security-context-binding closure: explicit InvalidGameContext invariants surface at the aggregate and WebApp layers, backed by regression coverage
- ⏳ Immediate gate: #77 Open App launchpad hardening and QA now that the core persistence/security stack is steady
- ⏳ Next: Ship #84 abuse protection plus #78–#79 Mini App UI once the #77 gate passes

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
2. **#77** ⏳ Open App Launchpad hardening (awaiting merge + QA rerun now that #80–#83 compliance is validated)
3. **#80** ✅ Game Persistence (schema + TTL audit closed; contract satisfied)
4. **#81** ✅ Security-context-binding (cross-chat InvalidGameContext invariants enforced)
5. **#82** ✅ Versioning/Concurrency APIs (expectedVersion + ConcurrencyConflict semantics shipped)
6. **#78–#79** ⏳ Mini App UI (blocked until #77 UI refinements and #84 abuse protection land)

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
1. Drive #77 Open App launchpad sign-off (UI hardening + QA rerun) now that the core persistence/security stack is steady
2. Coordinate #84 abuse protection (rate limits and tamper detection) so the UI slices can ship with guardrails
3. Approve #78–#79 Mini App UI release plan once #77/#84 completion criteria are met
