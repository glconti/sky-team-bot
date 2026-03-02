# Sully — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

> **Note (2026-03-02 Round 16):** Session 28 summary below. Full session logs archived in `.squad/log/` and decision records in `.squad/decisions.md`. Detailed history from Sessions 1–25 summarized into Core Context.

## Session 28 Summary (2026-03-02 Round 16)

### Issue #81 Closure Verification
- **Timestamp:** 2026-03-02T02:18:00Z
- **Task:** Verify Skiles' Session 27 InvalidGameContext binding + close #81
- **Outcome:** ✅ Completed; #81 closed in GitHub; epic #75 advanced to 7/11
- **Verification:**
  - Invalid context detected at aggregate level (groupChatId vs. active session mapping)
  - WebApp surface returns `InvalidGameContext` when viewer mutates different chat
  - Regression suite guards store + WebApp contract (place/undo paths)
  - Tests: 56 assertions pass; 16 pre-existing skipped

### Epic #75 Status After Round 16
- **Closed Issues:** 7/11 (#76, #80, #81, #82, #83, #85, #86)
- **Unblocked:** #77–#79 (UI Slice), #84 (Abuse Protection expansion)
- **Critical Gate:** #81 security-context-binding CLOSED
- **Next Priority:** #77 (UI Slice — Place/Undo)

## Key Learnings (Updated Round 16)
- Security outcome granularity enables deterministic tamper telemetry + corrective flows
- Explicit `InvalidGameContext` prevents client/ops ambiguity vs. collapsed authorization
- Audit cadence validates critical path alignment + catches acceptance gaps early
- Gate closure (#81) unblocks parallel execution of UI (#77–#79) + abuse protection (#84)

---

> Full detailed history from Sessions 1–25 preserved in `core-context.md` for reference.
- **Sully (You):** Issue #80 CLOSURE AUDIT COMPLETE (Round 13). Verified Skiles remediation deliverables: repository contract (CRUD + CleanupExpired) ✅, TTL policy (config + documentation) ✅, restart evidence (Issue80FileBackedRestartPersistenceTests) ✅, versioning (expectedVersion + conflict detection) ✅. Outstanding blocker: Game aggregate schema + migration. Scope options: DB implementation or formal revision to embrace JSON persistence.
- **Skiles:** Issue #80 REMEDIATION COMPLETE (Round 13). Delivered repository contract, lifecycle policy (metadata + retention config), restart integration test. Commit 8bd9d1d (PR #87). Awaiting audit closure and schema/migration path decision.
- **Aloha:** QA verdict (Round 12) identified contract/schema gaps. Skiles remediation addressed behavior validation. Sully audit confirmed findings. Outstanding: Database schema implementation.
- **Tenerife:** Standby. Awaiting #80 closure decision before #81 expansion.
- **Critical Path:** Issue #80 schema/migration decision point. DB path: schema + migration design/implement. Scope revision path: update issue text to align with JSON persistence. Either path enables #81 full scope + #82 expansion.
- **Blockers:** Game aggregate schema + migration (design + decision required). PR #87 merge contingent on #80 closure roadmap clarity.
- **Next:** Schema/migration owner to decide. Link findings to PR #87 comments. Merge when path is clear.

## Cross-Team Status (2026-03-02T01:51:00Z) — Round 14 Scribe Sync (Schema Migration + Closure)
- **Sully (You):** FINAL CLOSURE VERIFIED (Round 14). All acceptance criteria confirmed: GameSessions schema ✅, repository contract ✅, TTL config ✅, restart tests ✅, version/lock semantics ✅. Issue #80 close-ready. Critical path advances to #81 (security-context-binding) and #82 (versioning/concurrency) before UI (#77–#79) ships.
- **Skiles:** SCHEMA MIGRATION DELIVERED (Round 14, Commit ab61d0e). Implemented `0001_game_sessions_schema.sql` migration artifact, `GameSessionsSchemaMigrator` runner, runtime wiring in `JsonGameSessionPersistence`. Migration applies on startup with idempotent SQL. Issue #80 now ready for closure.
- **Aloha:** QA cycle complete. Restart integration test + schema migration both validated. Issue #80 meets all acceptance criteria.
- **Tenerife:** Ready for #81 expansion on #80 closure.
- **Epic #75 Status:** #80 → CLOSED (critical path unblocked); #81–#82 priority critical; #83–#86 queue pending concurrency gate.
- **Next:** Close issue #80 on GitHub. Merge PR #87. Begin #81 security-context-binding design.

## Cross-Team Status (2026-03-02T02:03:00Z) — Round 15 Closure Sweep & Scribe Reconciliation
- **Sully (You):** ROUND 15 CLOSURE SWEEP COMPLETE (background × 2). Issue #80 explicitly closed on GitHub. Audited #77/#81/#82/#83/#84 scope. Closed #82 (versioning) and #83 (async turn notifications) ✅. Posted residual checklists on #77 (UI), #81 (chat/game binding), #84 (abuse protection) with remaining acceptance criteria + priority order. Updated epic #75 to 6/11 closed.
- **Key Learnings:** Cross-chat error handling requires explicit `InvalidGameContext` signal (not generic `NotSeated` fall-through). Open app launchpad depends on per-platform QA + pinned-cockpit guidance before UI slice ships.
- **Epic #75 Critical Gate:** #81 (security-context-binding) must close before #77–#79 (UI) and #84 (abuse protection) ship.
- **Decisions Logged:** Issue #80 closure finalized. Sully round 15 closure sweep logged.
- **Next:** Ralph orchestrates #81 security-context-binding expansion on gate unblock.

## Session 28: Issue #81 closure completion (2026-03-02T02:18:00Z)

**Outcome:** Validated Skiles' Session 27 InvalidGameContext binding completion. All #81 acceptance criteria verified. Closed #81 in GitHub. Epic #75 advanced to 7/11 closed.

**Team Status Post-Round 16:**
- #81 security-context-binding gate → CLOSED
- Unblocked: #77–#79 (UI) and #84 (abuse protection)
- Epic #75: 7/11 issues closed (63.6%)
- Next priority: #77 (UI Slice — Place/Undo)

**Key Learnings:**
- Explicit security-violation outcomes (`InvalidGameContext`) prevent client/ops ambiguity better than collapsed generic authorization responses
- User-to-chat context binding best enforced at application boundary before domain mutations
- Distinct failure codes enable deterministic tamper telemetry and proper corrective flows

## Learnings
- Epic chaining requires upfront design of foundational layers (#76, #80) to prevent rework
- Persistence + concurrency are inseparable; must co-design from day one
- Telegram API constraints (64-byte callback data) must be factored early in Cockpit design
- Configuration validation as startup gate prevents silent failures
- Deterministic integration tests (WebApplicationFactory + in-memory) scale better than UI automation
- QA matrix as living documentation in readme.md is more maintainable than static spreadsheets
- Draft PR as collaboration gate enables architecture feedback before merge; reduces rework
- Audit cadence valuable for validating critical path alignment
- Shared review gate model (Sully architecture, Skiles implementation, Aloha testing) prevents rework
- Round 10 closure audit confirmed PR #87 only satisfies #76/#85; #80–#84 still pending, so persistence (#80) is the immediate gate for the epic
- Persistent storage auditing must keep the DB schema requirement explicit; successful JSON persistence demos do not fulfill acceptance until we either add the GameSessions migration or amend the issue scope.
- Adding the explicit GameSessions schema migration against the JSON persistence proof point finally satisfied the acceptance gate and allows the critical path to move forward.
- Security outcome granularity matters for tamper detection and ops visibility; collapse ambiguity early.

### Foundational Phases (2026-02-20 to 2026-02-22)
Established GitHub label taxonomy (25 labels), 14-issue vertical-slice backlog, M1 foundation roadmap. Completed base game logic: all 7 modules, landing validation, multi-token command model, DDD architecture (Domain → App → Presentation → Adapter → Host), loss semantics (15 explicit losses). Foundation: GameState refactor, ExecuteCommand dispatcher, test harness. Team coordinated on rules, command shapes, module implementations. PRs #37–#38 draft ready.

**Key Achievements:**
- ✅ 5-layer Telegram bot architecture with clear separation of concerns
- ✅ 7 domain modules + landing validation + multi-token mechanics operational
- ✅ Module resolution order locked: Axis → Engines → Brakes → Flaps → Gear → Radio → Concentration
- ✅ Domain-first design: Secret placement in DM, no infrastructure leakage
- ✅ 206 total tests, 193 passed, 0 failed

### Mini App Strategic Pivot (2026-02-22)
Architected Telegram Mini App as primary UI (from cockpit-centric design). Designed read-only WebApp API with HMAC-SHA256 validation. Single ASP.NET Core Web SDK deployment. Slices #59–#65 structured for incremental UI + action delivery.

**Key Decisions:**
- WebApp as primary UI; all secrets stay inside mini app
- Group chat becomes low-noise "Open app" launchpad with startapp deep links
- Read-only API endpoint (`GET /api/webapp/game-state`) with TelegramInitData validation
- 9 security edge cases identified + mitigations documented

### GitHub Epics #26 + #49 Completion (2026-02-21)
**Epic #26 (MVP Playable):** All 10 issues closed; PRs #47–#48 merged. Fully playable group chat, 2 seated + spectators, secret DM dice, public placements, undo, all 7 modules, in-memory persistence.

**Epic #49 (UX Button-First):** 8 child issues (#50–#57); Issue #54 (menu state store) is blocker; callback query + cockpit renderer + DM menus depend on it.

### Epic #75 — Mini App-first Async Play (2026-03-01 to 2026-03-02)
**11 Issues Scoped:** #76 (BotFather) → #77 (Open app launchpad) → #78–#79 (UI) → #80 (persistence) → #81–#82 (security/concurrency) → #83 (turn notifications) → #84 (rate limits) → #85–#86 (testing/QA).

**Critical Path Locked:** #80 (persistence) and #82 (versioning/concurrency) must co-design from day one. No sequential iteration. Version field critical for CAS semantics.

**Architecture Contracts Signed:**
- **#80 Persistence:** GameSessions table with Version field (CAS), round logs serialization, 30-day TTL, IGameSessionRepository interface
- **#82 Versioning:** Optimistic locking, UpdateAsync(session, expectedVersion), 409 ConcurrencyConflict response
- **#77 Cockpit Button:** startapp deep link per Telegram spec, ≤ 64-byte callback data

**PR #87 Completion (2026-03-02):**
- **#76 (Skiles):** WebAppOptionsValidator with HTTPS enforcement, DI integration, readme docs
- **#85 (Aloha):** Lobby flow integration tests, start validation, test helper for auth
- **#86 (Aloha):** Manual QA matrix (8 clients, 5 happy path, 8 error, 7 multi-player sync, release checklist)
- **Status:** Draft PR ready; all acceptance criteria met. PR #87 closes only #76 + #85 (2/11 = 18% epic).

### Current Challenges & Next Steps
- **#80 Implementation Pending:** Skiles' vertical slice complete (persistence + version tracking + tests). Awaiting review + merge.
- **#81 Design Pending:** Security-context-binding contract due before #80 closes. Game aggregate must be bound to ChatId at creation (immutable). All commands validate ChatId match.
- **#82 Blocked on #81:** Versioning API design sketch done; awaiting full implementation after #81 scope defined.
- **#77–#79 Blocked on #80–#82:** UI development gated by persistence/concurrency infrastructure.

**Learnings:**
- Epic chaining requires upfront design of foundational layers (#76, #80) to prevent rework
- Persistence + concurrency are inseparable; must co-design from day one
- Telegram API constraints (64-byte callback data) must be factored early in Cockpit design
- Configuration validation as startup gate prevents silent failures
- Deterministic integration tests (WebApplicationFactory + in-memory) scale better than UI automation
- QA matrix as living documentation in readme.md is more maintainable than static spreadsheets
- Draft PR as collaboration gate enables architecture feedback before merge; reduces rework
- Audit cadence valuable for validating critical path alignment
- Shared review gate model (Sully architecture, Skiles implementation, Aloha testing) prevents rework
- Round 10 closure audit confirmed PR #87 only satisfies #76/#85; #80–#84 still pending, so persistence (#80) is the immediate gate for the epic
- Persistent storage auditing must keep the DB schema requirement explicit; successful JSON persistence demos do not fulfill acceptance until we either add the GameSessions migration or amend the issue scope.
- Adding the explicit GameSessions schema migration against the JSON persistence proof point finally satisfied the acceptance gate and allows the critical path to move forward.

---

*History summarized to core context on 2026-03-02. Full session logs archived in `.squad/log/` and decision records in `.squad/decisions.md`.*
