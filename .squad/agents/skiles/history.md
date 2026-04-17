# Skiles — History

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

### Session 27: Issue #81 residual InvalidGameContext completion (2026-03-02T05:05:00Z)

**Outcome:** Completed the residual #81 security checklist by introducing explicit `InvalidGameContext` mutation outcomes for cross-chat attempts, codifying the invariant at the game-session aggregate boundary, and extending regression/integration coverage on place+undo paths in PR #87.

**Key Learnings:**
- Security violations (`cross-chat mutation attempt`) should not be collapsed into generic authorization outcomes (`NotSeated`), because the API contract needs a distinct signal for tampering scenarios.
- Detecting invalid context by checking whether the user is seated in a different active session preserves backward-compatible `NotSeated` behavior for users who are simply not participants anywhere.

**Delivered Artifacts:**
- `SkyTeam.Application\GameSessions\InMemoryGroupGameSessionStore.cs`
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs`
- `SkyTeam.Application.Tests\GameSessions\InMemoryGroupGameSessionStoreTests.cs`
- `SkyTeam.Application.Tests\Telegram\Issue64WebAppPlacementFlowTests.cs`
- `.squad/decisions/inbox/skiles-issue-81-residual.md`

### Session 28: Issue #81 complete - #81 closed by Sully (2026-03-02T02:18:00Z)

**Outcome:** After your Session 27 InvalidGameContext binding completion, Sully validated acceptance criteria and closed #81 in GitHub. Epic #75 advanced to 7/11 closed.

**Team Status:**
- #81 security-context-binding gate now CLOSED → unblocks #77–#79 (UI) and #84 (abuse protection)
- Epic #75 progress: 7/11 issues closed
- Next critical path: #77 (UI Slice — Place/Undo)

**Key Learnings:**
- Security violation outcomes must remain distinct from authorization failures; `InvalidGameContext` prevents ambiguity at client/ops levels
- User-to-chat context mapping required whenever multiple sessions coexist for same user

## Session 28–29 Summary (2026-03-02)

### Issue #77 Final Residual Closure by Skiles (Session 28 Round 17)
- **Timestamp:** 2026-03-02T06:40:00Z
- **Task:** Implement #77 residual checklist from Sully's sweep
- **Outcome:** ✅ Completed; residual QA/fallback sign-off documented
- **Deliverables:**
  - `TelegramBotService.cs` fallback text tightened for group-first context
  - `Issue60LaunchMiniAppButtonTests` callback payload length assertions (≤64 bytes)
  - `Issue53InGameCockpitButtonFlowTests` fallback-safety coverage
  - `readme.md` documented #77 residual QA sign-off (iOS/Android/Desktop + callback-size safety)
- **Launchpad Strategy:** Group-first fallback keeps `/sky app`/`/sky state` explicit; unavailable deep-link guidance points to group chat

### Issue #77 Final Closure by Sully (Session 28 Round 17)
- **Timestamp:** 2026-03-02T07:10:00Z
- **Task:** Declare #77 ready for closure post-Skiles residual implementation
- **Outcome:** ✅ Completed; closure approved; epic #75 advanced to 8/11
- **QA Narrative Lock:** Explicit iOS/Android/Desktop client coverage + callback-size guardrails ≤64 bytes
- **Impact:** Unblocks #77–#79 (UI slices) on critical path; callback-size constraint establishes pattern for future Telegram integrations

### Epic #75 Status After Round 17
- **Closed Issues:** 8/11 (#76, #77, #80, #81, #82, #83, #85, #86)
- **Next Priority:** #78–#79 (UI slices) now unblocked
- **Secondary Gate:** #84 (Abuse Protection expansion)
- **Critical Gate Status:** All pre-UI gates now CLOSED (#80 schema, #81 security-context, #77 launchpad)

### Round 18 Summary — #78–#79 UI Residuals & #84 Impact (2026-03-02T02:47:00Z)
- **Task:** Complete #78–#79 UI residual checklist
- **Outcome:** ✅ Feature-complete; manual Telegram client QA is final blocker
- **Deliverables:**
   - `WebAppFrontend` Place/Undo button state management + module display refinement
   - Error toast messaging (invalid placement, network timeout)
   - Cockpit state sync validation (refresh logic)
   - WebApp integration tests for all paths (32/32 passing)
- **Testing:** 273 total tests passing; all automated gates cleared
- **Blocker:** Manual client QA required (iOS/Android/Desktop/Web launch + button interaction)
- **Impact:** #84 (abuse protection) explicitly closed by Sully; Epic #75 now at 8/11 closed (72.7%)

### Epic #75 Status After Round 18
- **Closed Issues:** 8/11 (#76, #77, #80, #81, #82, #83, #84, #85–#86 implementation complete)
- **Pending Manual QA:** #78–#79 (UI — awaiting Telegram client validation)
- **Infrastructure Gates:** ALL CLEARED (#80 schema, #81 security, #82 concurrency, #84 abuse protection)
- **Next Priority:** Manual Telegram client QA (iOS/Android/Desktop/Web) before #78–#79 closure

---

## Session 28 Summary (2026-03-02 Round 16)

### Issue #81 Closure Verification
- **Timestamp:** 2026-03-02T05:05:00Z
- **Task:** Implement residual checklist for #81 security-context-binding
- **Outcome:** ✅ Completed with explicit cross-chat tampering outcome
- **Deliverables:**
  - `GamePlacementStatus.InvalidGameContext` + `GameUndoStatus.InvalidGameContext` statuses
  - `InMemoryGroupGameSessionStore` cross-chat detection: checks if user seated in different active session
  - 4 new regression tests (place/undo × cross-chat + endpoint surface)
  - WebApp returns 409 with `"InvalidGameContext"` error string
  - Full test suite passing

### Issue #81 Closure by Sully (Session 28)
- **Timestamp:** 2026-03-02T02:18:00Z
- **Task:** Verify #81 acceptance criteria and close
- **Outcome:** ✅ Completed; issue closed; epic #75 advanced to 7/11
- **Verification:**
  - Invalid context detected at aggregate level (groupChatId vs. active session mapping)
  - WebApp surface returns `InvalidGameContext` when viewer mutates different chat
  - Regression suite guards store + WebApp contract on place/undo paths
  - Tests: 56 assertions pass, 16 pre-existing skipped

### Epic #75 Status After Round 16
- **Closed Issues:** #76, #77, #80, #81, #82, #83 (6 partial + 1 full = ~7/11)
- **Unblocked:** #77–#79 (UI), #84 (abuse protection)
- **Next Priority:** #77 (UI Slice — Place/Undo)
- **Critical Gate:** #81 security-context-binding now CLOSED

## Key Learnings (Updated Round 16)
- Security outcome granularity matters: explicit `InvalidGameContext` prevents client/ops ambiguity vs. collapsed authorization
- Distinguishing "wrong chat" from "not seated anywhere" safe at application boundary without domain entity changes
- User-to-chat context mapping required whenever multiple coexistent sessions possible
- Idempotent startup schema migration can satisfy DB artifact requirements without runtime rewrite

---

> Full detailed history from Sessions 1–26 preserved in `core-context.md` for reference.
- **Skiles (You):** Issue #80 REMEDIATION COMPLETE (Round 13). Delivered repository contract (CRUD + CleanupExpired), lifecycle policy (TTL metadata + config), restart integration test. Commit 8bd9d1d (PR #87). Outstanding: Game aggregate schema + migration.
- **Sully:** Issue #80 CLOSURE AUDIT COMPLETE (Round 13). Verified deliverables: repository contract ✅, TTL policy ✅, restart evidence ✅, versioning ✅. Outstanding blocker: Schema/migration criterion. Path forward: DB implementation or scope revision.
- **Aloha:** QA verdict (Round 12) identified gaps. Skiles + Sully remediation/audit completed. Issue #80 remains open pending schema/migration decision.
- **Tenerife:** Standby. Awaiting #80 closure decision before #81 expansion.
- **Critical Path:** Issue #80 schema/migration decision → close → #81 full scope → #82 expansion.
- **Blockers:** Game aggregate schema + migration (design + implementation decision required).
- **Next:** Schema/migration owner to commit. PR #87 merge contingent on #80 closure path clarity.

## Cross-Team Status (2026-03-02T01:51:00Z) — Round 14 Scribe Sync (Schema Migration + Closure)
- **Skiles (You):** SCHEMA MIGRATION DELIVERED (Round 14, Commit ab61d0e). Implemented `0001_game_sessions_schema.sql` migration artifact, `GameSessionsSchemaMigrator` runner, runtime wiring in `JsonGameSessionPersistence`. Migration applies on startup with idempotent SQL. Issue #80 now ready for closure.
- **Sully:** FINAL CLOSURE VERIFIED (Round 14). All acceptance criteria confirmed: GameSessions schema ✅, repository contract ✅, TTL config ✅, restart tests ✅, version/lock semantics ✅. Issue #80 close-ready. Critical path advances to #81 (security-context-binding) and #82 (versioning/concurrency) before UI (#77–#79) ships.
- **Aloha:** QA cycle complete. Restart integration test + schema migration both validated. Issue #80 meets all acceptance criteria.
- **Tenerife:** Ready for #81 expansion on #80 closure.
- **Epic #75 Status:** #80 → CLOSED (critical path unblocked); #81–#82 priority critical; #83–#86 queue pending concurrency gate.
- **Next:** Close issue #80 on GitHub. Merge PR #87. Begin #81 security-context-binding design.

## Learnings
- For strict acceptance criteria, an idempotent startup schema migration can close a database-schema gate without forcing a risky rewrite of an already stable JSON persistence runtime.
- Keeping SQL migration scripts as repository artifacts and embedding them at build time provides both auditability and production-safe runtime loading.
- SQLite test cleanup should clear pooled connections (`SqliteConnection.ClearAllPools`) before deleting temporary directories, otherwise teardown can fail with locked database files.
- Cross-chat mutation tampering requires an explicit `InvalidGameContext` contract to keep security telemetry and client behavior deterministic.
- The safest compatibility path is to emit `InvalidGameContext` only when the user is seated in another active session, while leaving true non-participants on `NotSeated`.
- Security outcome granularity matters for tamper detection and ops visibility.
- Aggregates must maintain user-to-chat mappings when multiple coexistent sessions are possible.
- Group launchpad fallbacks should always point back to group controls (`/sky app`, `/sky state`) so Mini App-first navigation does not regress into DM-first behavior.
- Callback payload budget needs explicit regression coverage at the cockpit-button layer, not only at codec decode time, to keep Telegram 64-byte guarantees visible.
- Abuse guardrails are easiest to enforce consistently when transport filters centralize 429 throttling, idempotency replay checks, and payload-size validation before domain mutation.
- 400/429 responses should always include action-oriented retry hints so Mini App clients can recover deterministically without exposing sensitive request payloads in logs.

### Session 29: Issue #78 Mini App Lobby UI Verification (2026-03-03)

**Outcome:** Verified that Issue #78 (Mini App Lobby UI) is already fully implemented and all tests are passing. The lobby UI was previously implemented as part of the WebApp foundation work and includes all required elements.

**Key Learnings:**
- The lobby UI implementation already contains all required elements from the test specifications
- All three Issue78WebAppLobbyUiTests test cases passing:
  1. `LobbyView_ShouldExposeSeatPlaceholdersAndActions_ForMiniAppLobbyUi` ✅
  2. `LobbyView_ShouldTruncateDisplayNames_ToTelegramLimit` ✅
  3. `LobbyView_ShouldExposeValidationMessages_ForInvalidCreateAndJoinInput` ✅
- Implementation includes:
  - Seat display with "Waiting for Pilot…" / "Waiting for Copilot…" placeholders
  - Action buttons: "New Lobby", "Join Lobby", "Start Game"
  - Create form: "Game name", "Player count", "Lobby settings" inputs
  - Join form: "Game code" input
  - Client-side validation with exact error messages
  - `maxDisplayNameLength = 32` constant and `truncateDisplayName` function
  - Display name truncation applied to pilot/copilot seat rendering

**Test Results:**
- Issue78 tests: 3/3 passing
- Total test suite: 164 passing, 4 failing (unrelated Issue60 tests), 16 skipped

**No changes required** — the issue acceptance criteria were already met by previous implementation.

### Session 30: Issue #79 Mini App In-Game UI Verification (2026-03-03)

**Outcome:** Verified that Issue #79 (Mini App In-Game UI) is already fully implemented and all tests are passing. The in-game UI was previously implemented as part of the WebApp foundation work and includes all required elements.

**What Was Verified:**
- All six Issue79WebAppInGameUiTests test cases passing:
  1. ConcurrencyConflict handling (409 status + refresh flow)
  2. expectedVersion parameter passed in action requests
  3. "In Game" section header
  4. "Round & Turn" info card showing placements
  5. "Cockpit" card displaying module status (Axis, Engines, Brakes, Flaps, Landing gear)
  6. Undo button conditionally shown (requires privateHand + viewerSeat)
  7. Roll button conditionally shown (viewerSeat + AwaitingRoll status)
  8. Private hand gated on viewer seat check (viewerSeat === hand.seat)
  9. Module status indicators for all required modules

**Existing Implementation Coverage:**
- Line 247: `<div class="panel-title">In Game</div>` section header
- Lines 637-656: Three cards for "Round & Turn", "Cockpit", "Systems", "Flight"
- Lines 642-646: Cockpit card with Axis position, Engines speed, Approach
- Lines 648-650: Brakes, Flaps, Landing gear status in Systems card
- Lines 665-671: Undo button with `canUndo = !!(state.privateHand && viewerSeat)` condition
- Lines 673-679: Roll button with viewerSeat + roundStatus check
- Lines 684-698: `renderPlacement` function gates on `!hand || !viewerSeat` early return
- Lines 368-374: `buildUrl` function adds expectedVersion parameter
- Lines 464-467: ConcurrencyConflict handling with 409 status check + loadState() refresh
- Lines 792-796: Place action passes `state.version` as expectedVersion

**Test Results:**
- Issue79 tests: 6/6 passing
- Total test suite: 309 passing, 4 failing (unrelated Issue60 tests), 16 skipped

**No changes required** — the issue acceptance criteria were already met by previous implementation.
