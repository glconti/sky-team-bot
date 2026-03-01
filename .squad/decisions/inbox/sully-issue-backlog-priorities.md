# Sully: Mini App Backlog → Issue Prioritization

**Date:** 2025  
**Author:** Sully (Lead/Architect)  
**Status:** Approved

## Executive Summary

Refined 11 backlog items into GitHub issues with **P0 / P1 / P2** priority and squad ownership. Dependency chain respects "Mini App-first UX" product goal: Telegram infrastructure → WebApp UI → persistence/resilience → testing/QA.

## Prioritization Rationale

### P0 (Blocking Infrastructure)
- **botfather-main-miniapp**: Root blocker. Cannot launch Mini App without BotFather setup. Unblocks 4 other items. Assign: **squad:skiles** (Telegram Bot integrations).
- **group-launchpad-ux**: Hardened in-group "Open app" entry point eliminates DM-first friction. Depends on botfather config. Assign: **squad:skiles**.

### P1 (Core Domain + Feature Work)
**Architecture & Security (squad:sully):**
- **game-session-persistence**: Aggregate root-level concern. Drives schema, TTL, migration strategy. Durable state is prerequisite for async play. Unblocks 3 items (turn-notifications, concurrency-conflicts, security-binding).
- **security-context-binding**: Enforce chat/game association at domain boundary. Prevents cross-chat tampering. DDD architectural decision: game aggregate must validate context consistency.
- **concurrency-conflicts**: Add versioning/ETag semantics. Prevents double-moves in concurrent scenarios. Critical for trustless async.
- **abuse-limits**: Rate limiting + input validation. Protect endpoints from abuse. Infra-level concern but architectural surface.

**WebApp & Bot Integration (squad:skiles):**
- **webapp-lobby-ui**: Create/join UX. Depends on botfather + persistence layer. Core user journey.
- **webapp-game-ui**: In-game UX. Depends on lobby. Game state rendering + action dispatch.
- **turn-notifications**: Async notification flow (group update policy TBD). Depends on persistence.

**Testing & Quality (squad:aloha):**
- **api-integration-tests**: End-to-end verification (initData, context, create/join, authorization). Depends on botfather config.

### P2 (Documentation)
- **qa-matrix**: Manual test checklist across Telegram clients. Documentation artifact. Lower priority initially (can follow after P0/P1 core work).

## Squad Assignments

| Squad | Responsibility | Items |
|-------|---|---|
| **squad:skiles** | Telegram Bot integrations, WebApp frontend, notifications | botfather-main-miniapp, group-launchpad-ux, webapp-lobby-ui, webapp-game-ui, turn-notifications |
| **squad:sully** | Domain architecture, security, concurrency, persistence | game-session-persistence, security-context-binding, concurrency-conflicts, abuse-limits |
| **squad:aloha** | Testing, QA matrices | api-integration-tests, qa-matrix |
| **squad:tenerife** | Game rules validation | *(not in this backlog)* |

## Dependency Chain

```
botfather-main-miniapp [P0]
├── group-launchpad-ux [P0]
├── webapp-lobby-ui [P1]
│   └── webapp-game-ui [P1]
├── api-integration-tests [P1]
└── qa-matrix [P2]

game-session-persistence [P1] (foundational; can start in parallel)
├── turn-notifications [P1]
├── concurrency-conflicts [P1]
└── security-context-binding [P1]
```

## Key Architectural Decisions

1. **Aggregate Root: Game persistence first.** Before UI, we need durable game state. Drives schema, TTL, conflict resolution.
2. **Context binding at domain layer.** Security is not a feature; it's an invariant. Game aggregate must reject invalid chat contexts.
3. **Versioning/ETag pattern for concurrency.** Simple, deterministic, fits async turn-based model. Avoids pessimistic locks.
4. **WebApp launchpad depends on BotFather config.** Cannot test Mini App launch UX until startapp is live. Serialization enforces correct order.

## Next Steps

1. Create all 11 issues with labels + acceptance criteria.
2. Backlog P1 items; start P0 blockers immediately.
3. Organize sprint planning by squad ownership.
4. Track dependency edges in GitHub (e.g., issue links or zenhub).

---

**Approved by:** Sully
