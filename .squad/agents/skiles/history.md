# Skiles — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Cross-Team Status (2026-03-02T01:40:00Z) — Round 13 Scribe Sync (Remediation + Audit)
- **Skiles (You):** Issue #80 REMEDIATION COMPLETE (Round 13). Delivered repository contract (CRUD + CleanupExpired), lifecycle policy (TTL metadata + config), restart integration test. Commit 8bd9d1d (PR #87). Outstanding: Game aggregate schema + migration.
- **Sully:** Issue #80 CLOSURE AUDIT COMPLETE (Round 13). Verified deliverables: repository contract ✅, TTL policy ✅, restart evidence ✅, versioning ✅. Outstanding blocker: Schema/migration criterion. Path forward: DB implementation or scope revision.
- **Aloha:** QA verdict (Round 12) identified gaps. Skiles + Sully remediation/audit completed. Issue #80 remains open pending schema/migration decision.
- **Tenerife:** Standby. Awaiting #80 closure decision before #81 expansion.
- **Critical Path:** Issue #80 schema/migration decision → close → #81 full scope → #82 expansion.
- **Blockers:** Game aggregate schema + migration (design + implementation decision required).
- **Next:** Schema/migration owner to commit. PR #87 merge contingent on #80 closure path clarity.

## Core Context (Summarized from Sessions 1–23)

### Foundational Work (Sessions 1–2, 2026-02-21)
Audited base game logic: 40% in place, all 19 unit tests passing. Designed 4-phase Milestone 1 plan (GameState + ExecuteCommand + Modules + Win/Loss). Modeled CoffeeTokenPool value object for Concentration module. GitHub backlog structured with label taxonomy. Telegram architecture finalized as 5-layer design (Domain → App → Presentation → Adapter → Host). Created `SkyTeam.TelegramBot` project.

**Key Achievements:**
- ✅ Phase 1 foundation: GameState + ExecuteCommand dispatcher (Phase 1 blocker met)
- ✅ All 7 domain modules complete + landing validation + multi-token mechanics
- ✅ Immutable value object patterns (CoffeeTokenPool, Die, Altitude, Airport)
- ✅ Guard clauses + early returns (high code quality baseline)

### Telegram Infrastructure (Sessions 4–8, 2026-02-21 to 2026-02-22)
Converted to ASP.NET Core Web SDK; moved polling to `BackgroundService`. Implemented callback plumbing (`v1:grp:refresh` versioning for 64-byte constraint). Added cockpit lifecycle management (edit-first, recreate-on-fail, best-effort pinning). Wired lobby buttons (New, Join, Start). All with in-memory state store + per-group locking.

**Key Patterns:**
- Single edited cockpit message (group) + DM hand menus (private)
- Callback versioning: `v1:action:index` for 64-byte compliance
- Menu state store: Per-group in-memory with GC
- Edit-first with recreate fallback for robust lifecycle
- Soft-fail callback UX (toasts on invalid presses)

### WebApp Foundation (Session 14, 2026-02-22)
Added Telegram Mini App (`initData` validation, HMAC-SHA256 with constant-time compare, 5-minute freshness). Single `GET /api/webapp/game-state` endpoint returning public state. TelegramInitDataValidator + TelegramInitDataFilter. Tests reflecting on stable class instead of Program.cs.

**Key Learnings:**
- `initData` validation sensitive to HMAC key/data order (`secret_key = HMAC("WebAppData", bot_token)`)
- Use constant-time compare + auth_date max age validation
- Convert host in-place (Web SDK) to keep in-memory stores shared via DI

### Epic #75 — Mini App-first Async Play (2026-03-01 to 2026-03-02)
**Session 15 (#76):** BotFather Main Mini App guardrails — `WebAppOptionsValidator` enforces absolute HTTPS URLs, no query/fragment. Fails fast at startup via `ValidateOnStart()`. readme docs + operator checklist.

**Session 16 (#80):** Durable persistence vertical slice — JSON persistence for active game sessions. Per-round log replay for deterministic state reconstruction. Cockpit message ids persisted. IGameSessionRepository port in Application layer, JsonGameSessionPersistence in TelegramBot layer. All acceptance criteria met; deferred Version field + TTL to #82.

**Session 18 (#77):** Launchpad hardening slice — Robust Open app deeplinks via `startapp=<groupChatId>`. Safe validation of bot username + chat id. Fallback to Refresh + `/sky state` when launch unavailable. Preserves Mini App-first flow without DM drift.

**Session 20 (#83):** Async turn notifications — DM-first + group fallback. Transition-key deduplication (groupChatId + transitionKey + recipientUserId + seat) prevents duplicates. Public turn summaries protect secrets. Transport-driven, minimal (full domain event infrastructure deferred).

**Session 22 (#84):** Abuse protection slice 1 — Per-user 10 req/sec, per-IP 100 req/min, lobby creation 1 req/user/5min. Input validation (oversized headers, invalid commandId, invalid display names). Endpoint filters in DI preserve DDD. Logging for throttled/rejected requests. Deferred: /sky command expansion, per-game idempotency, distributed limiter, telemetry.

**Session 23 (#81):** Chat/game binding first slice — Context-bound mutation overloads in `InMemoryGroupGameSessionStore` (`PlaceDie(groupChatId, userId)`, `UndoLastPlacement(groupChatId, userId)`). WebApp endpoints wired to signed request chat context. Legacy overloads as compatibility wrappers. Tests verify multi-session same-user routing + cross-chat rejection. Remaining scope: propagate to non-WebApp surfaces, multi-chat membership mapping, `InvalidGameContext` vs `NotSeated` semantics decision.

### PR #87 Consolidation (2026-03-02)
All work consolidated on `feat/issue-76-85-botfather-config-webapp-tests` branch:
- #76 (BotFather config validation) ✅
- #80 (persistence vertical slice) ✅
- #77 (launchpad hardening) ✅
- #83 (turn notifications) ✅
- #84 (abuse protection slice 1) ✅
- #81 (chat/game binding slice 1) ✅
- #85 (Aloha: WebApp integration tests) ✅
- #86 (Aloha: QA matrix) ✅

**Test Status:** 273 total tests passing (145 Domain + 128 Application/Tests)

### Current Blockers & Path Forward
- **#80 Complete:** Vertical slice merged into PR #87; deferred Version field + TTL to #82
- **#81 First Slice Done:** Context binding at app boundary complete; full scope pending (propagate to all surfaces, finalize semantics)
- **#82 Blocked on #81:** Versioning API (expectedVersion, ConcurrencyConflict) awaiting #81 scope closure
- **#77 Slice Done:** Launchpad hardening merged; full UI rendering awaits #82 completion
- **Next:** Merge PR #87 → finalize #81 design → implement #81 full + #82 versioning/concurrency APIs

### Key Architectural Decisions
1. **Persistence as Vertical Slice:** JSON-backed session replay using existing RebuildDomainGameFromLogs pattern. No domain-level serialization concerns. Deferred DB migration to future. Clean DDD boundary via IGameSessionRepository port.
2. **Chat/Game Binding at App Boundary:** User-only lookups unsafe once player is active in multiple group chats. Explicit `(groupChatId, userId)` context required at mutation boundaries. WebApp mutation endpoints wired to signed request context.
3. **Abuse Protection Without Infrastructure:** Singleton in-memory sliding-window filters sufficient for production. DDD boundary preserved (domain untouched, filters in transport layer). Endpoint-level input validation with 400 responses.
4. **Async Notifications Transport-Driven:** No domain event refactor in initial slice. Transition-key dedup + public summaries provide minimal but sufficient abstraction. DM-first + group fallback maintains responsiveness.
5. **Telegram Mini App as Primary UI:** All secrets stay inside WebApp. Group chat is launch surface only. Deeplink strategy consistent (`startapp=<groupChatId>`). Fallback behavior robust (Refresh + `/sky state`).

### Learnings
- Replaying per-round logs is sufficient for deterministic state reconstruction (no snapshot serialization needed)
- User-only session routing becomes unsafe at scale; explicit chat context binding required
- Keeping abuse controls in transport filters preserves domain purity
- Edit-first cockpit lifecycle (edit then recreate on fail) is robust for deleted/uneditable messages
- Callback refresh + `/sky state` command should share same RenderGroupState logic to avoid divergence
- Soft-fail callback UX (toast on invalid press) better than blocking button availability
- In-memory sliding-window abuse protection effective without external infrastructure
- Input validation safest at endpoint boundaries with explicit 400 errors
- Turn-notification dedup caches should reset per group when a new game starts, otherwise valid notifications can be suppressed in later sessions
- Notification fallback delivery should be best-effort (log + continue) so gameplay mutations never fail on Telegram transport issues

---

*History summarized to core context on 2026-03-02. Full session logs archived in `.squad/log/` and decision records in `.squad/decisions.md`.*

### Session 24: Issue #82 optimistic concurrency first slice (2026-03-02T02:10:00Z)

**Outcome:** Started issue #82 by adding expected-version optimistic concurrency guards on roll/place/undo mutations with explicit conflict responses for stale writes.

**Key Learnings:**
- Compare-and-swap checks are safest when enforced before turn/command validation so stale writes always fail deterministically with a conflict contract.
- Carrying `Version` on public session snapshots lets transport layers implement retryable optimistic locking without leaking persistence concerns into domain entities.
- Returning `ConcurrencyConflict` with `CurrentVersion` creates a clear non-200 recovery path for Mini App clients (refresh + retry).

**Delivered Artifacts:**
- `SkyTeam.Application\GameSessions\InMemoryGroupGameSessionStore.cs`
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs`
- `SkyTeam.Application.Tests\GameSessions\InMemoryGroupGameSessionStoreTests.cs`
- `SkyTeam.Application.Tests\Telegram\Issue64WebAppPlacementFlowTests.cs`
- `.squad/decisions/inbox/skiles-issue-82.md`

### Session 25: Issue #83 async notification practical completion (2026-03-02T03:15:00Z)

**Outcome:** Completed practical hardening for async turn notifications by preventing stale dedup carry-over between games and making group fallback delivery safe/non-blocking.

**Key Learnings:**
- Per-group dedup cleanup on game start is a low-cost way to keep idempotency while avoiding cross-session notification drop-offs.
- Fallback notification send paths must never throw back into command/mutation flows; warning logs are sufficient for operator visibility.

**Delivered Artifacts:**
- `SkyTeam.TelegramBot\TelegramBotService.cs`
- `SkyTeam.Application.Tests\Telegram\Issue83AsyncTurnNotificationTests.cs`
- `readme.md`
- `.squad/decisions/inbox/skiles-issue-83-complete.md`

### Session 26: Issue #80 close-readiness remediation (2026-03-02T04:20:00Z)

**Outcome:** Added explicit repository CRUD/list/query contract operations on persistence port, introduced persisted lifecycle metadata + cleanup policy, and delivered a file-backed restart-path integration test tied to PR #87 scope.

**Key Learnings:**
- Persisted session timestamps (`CreatedAtUtc`, `UpdatedAtUtc`) are the minimum contract needed to implement deterministic retention cleanup without touching domain entities.
- Repository contract completeness can be added incrementally by extending the persistence port with `Create/Update/GetById/List` while preserving existing replay-based store behavior.
- Restart durability claims are stronger when validated through a host-level restart integration path using the real JSON adapter, not only in-memory test doubles.

**Delivered Artifacts:**
- `SkyTeam.Application\GameSessions\GameSessionPersistence.cs`
- `SkyTeam.Application\GameSessions\InMemoryGroupGameSessionStore.cs`
- `SkyTeam.TelegramBot\Persistence\JsonGameSessionPersistence.cs`
- `SkyTeam.Application.Tests\GameSessions\JsonGameSessionPersistenceTests.cs`
- `SkyTeam.Application.Tests\Telegram\Issue80FileBackedRestartPersistenceTests.cs`
- `.squad/decisions/inbox/skiles-issue-80-remediation.md`

## Learnings
- For strict acceptance criteria, an idempotent startup schema migration can close a database-schema gate without forcing a risky rewrite of an already stable JSON persistence runtime.
- Keeping SQL migration scripts as repository artifacts and embedding them at build time provides both auditability and production-safe runtime loading.
- SQLite test cleanup should clear pooled connections (`SqliteConnection.ClearAllPools`) before deleting temporary directories, otherwise teardown can fail with locked database files.
