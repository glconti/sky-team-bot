# Sully — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Cross-Team Status (2026-03-02T01:10:22Z) — Round 10 Scribe Sync
- **Sully (You):** Closure audit (Round 9) complete. PR #87 (BotFather + WebApp tests) merged candidate. Architecting #81 full scope (identity/role context binding + UI state). Ready to design #82 full conflict expansion.
- **Skiles:** Issue #82 Slice 1 COMPLETED. Optimistic concurrency (CAS mutations + 409 responses) in place. Version exposed in GameSessionSnapshot. Commit 6001682 posted. Tests active; parallel stale write expansion pending.
- **Aloha:** Completed #80 QA coverage. Available for #77 UI implementation.
- **Tenerife:** Standby for #83 turn notifications.
- **Critical Path:** #80 (persistence done) → #81 (slice 1 done, architecture review + full scope pending) → #82 (slice 1 done, parallel tests + non-WebApp surfaces pending) → #83/#84 (parallel).
- **Next:** Merge PR #87. Review #81 full scope design. Plan #82 conflict test expansion. Schedule #83/#84 parallel start.

## Core Context (Summarized from Sessions 1–13)

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

---

*History summarized to core context on 2026-03-02. Full session logs archived in `.squad/log/` and decision records in `.squad/decisions.md`.*
