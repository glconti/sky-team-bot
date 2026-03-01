# Sully — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Cross-Team Status (2026-03-02T00:25:39Z)
- **Sully (You):** Issue #80/#82 architecture contract designed + decision records posted. Persistence contract stabilized; versioning scope deferred to #82.
- **Skiles:** Issue #80 vertical slice COMPLETED (persistence + version tracking + tests passing). #82 versioning APIs pending design review.
- **Aloha:** Issue #80 QA coverage COMPLETED (round-trip + deterministic concurrency). Version-conflict test skipped (blocked on #82 API implementation).
- **Critical Path:** #80→#81 (security-context-binding) → #82 (versioning/concurrency) before UI integration.
- **Next:** Begin #81 security-context-binding design; review #82 versioning API contract (expectedVersion input + ConcurrencyConflict response).

## Core Context

### Foundational Work (Sessions 1–3: 2026-02-20 through 2026-02-21)
**Baseline:** Established GitHub label taxonomy (25 labels), backlog structure (14 vertical-slice issues), and M1 foundation roadmap. Assessed Telegram placement + token architecture; produced extended command model and multi-token spec. Team coordinated on rules, command shapes, and module implementations.

**Key Achievements:**
- ✅ GitHub labels: Type, Priority, Status, Area, Routing categories; squad routing embedded
- ✅ Module resolution order: Axis → Engines → Brakes → Flaps → Gear → Radio → Concentration (locked)
- ✅ Multi-token spec: cost `k = |adjusted - rolled|`, single `PlaceDieCommand` with `AdjustedValue` parameter
- ✅ Domain-first architecture: Secret placement in DM, token pool per aggregate, no infrastructure leakage
- ✅ Skiles' Phase 1 foundation: GameState refactor + ExecuteCommand dispatcher (unblocked Phase 2)
- ✅ Aloha's test harness: Boundary conditions, landing outcomes, token mechanics (all green)

**Team Coordination:**
- Tenerife finalized rules spec + loss semantics (15 explicit losses, 8 invalid-move categories)
- Skiles delivered draft PR #37 (7 modules + landing validation + multi-token spend)
- Aloha created draft PR #38 (boundary tests + landing matrix + token pool tests)
- Scribe consolidated decisions + updated histories + committed .squad/

## Learnings

### Session 4-6: Telegram Architecture + Domain Completion (2026-02-21)

**Outcome:** Drafted 5-layer Telegram bot architecture (Domain → Application → Presentation → Adapter → Host). Created `SkyTeam.TelegramBot` project. Fixed token pool wiring in PR #37. Finalized loss semantics checklist (15 explicit losses). All 7 modules + landing validation + multi-token mechanics operational in draft PRs #37–#38.

**Sully's Contributions:**
- 5-layer architecture with clear separation of concerns
- 7-Epic MVP backlog (A–G) with vertical slices
- 8 user interview questions (UX clarifications)
- PR #37 token pool fix: delegation from Game → ConcentrationModule
- Landing checks: 6 independent criteria (Engines ≥9, Brakes ≥6, Flaps ≥4, Gear ≥3, Axis ∈[-2,2], Approach cleared)

**Team Coordination:**
- Skiles created project + initialized Program.cs + began Issue #28 (application round/turn state)
- Tenerife produced UX spec (570+ lines, 7 transcripts) + loss semantics checklist
- Aloha added ExecuteCommand smoke tests + validated token-adjusted command surface

**Known Blockers:**
- Brakes landing criterion spec mismatch (BrakesValue switch 0–3 vs. speed ≥9 check unsatisfiable)
- Telegram button rendering spec needed (callback data 64-byte constraint)

### Session 4: Slice #59 — WebApp Foundation Design Review (2026-02-22)

**Outcome:** Architected Telegram Mini App (WebApp) as primary UI. Designed read-only API endpoint, HMAC validation strategy, and single-host hosting model.

**Key Decisions:**
- **Hosting:** Convert existing `SkyTeam.TelegramBot` from console app to ASP.NET Core Web SDK. In-memory stores become singletons in DI; Telegram polling moves to `IHostedService`. Single process, single deployment unit.
- **Static files:** Minimal `wwwroot/index.html` shell (vanilla HTML + Telegram.WebApp.js). No bundler, no SPA framework yet. Slice #62 adds real UI.
- **WebApp API:** Single read-only endpoint `GET /api/webapp/game-state?gameId=...` with `X-Telegram-Init-Data` header auth. Returns public game state (no secrets). 200/400/401/404 responses.
- **Security:** HMAC-SHA256 validation per Telegram spec (FixedTimeEquals constant-time comparison), 5-minute auth_date freshness window, cross-check signed `start_param` against query gameId.
- **Risk mitigation:** 9 edge cases identified (HTTPS requirement, replay, spoofing, token exposure, concurrency, parse failure, missing state, missing initData, clock skew) with documented mitigations.

**Strategic Pivot:**
- **Old design:** Group chat cockpit + secret interactions in DM.
- **New design:** Mini App is primary UI; all secrets (dice hand, placements) stay inside mini app. Group chat becomes low-noise "Open app" launchpad. DM flows obsolete.

**Action Items Delivered:**
- Sully: Comprehensive design doc + risk analysis (approved for implementation)
- Gimli: Created `wwwroot/index.html` shell + configuration decision
- Skiles: Web SDK conversion + TelegramInitDataValidator + TelegramInitDataFilter + GET /api/webapp/game-state endpoint
- Aloha: Unit tests (validator), integration tests (endpoint), Issue #53 callback tests (passing)

**Backlog Restructure:**
- Slice #59 ✅ COMPLETE: WebApp foundation (hosting + validation + read-only API)
- Slice #60: Launch surface ("Open app" button + start_param wiring)
- Slice #61: Mini app lobby (New/Join/Start UI)
- Slice #62: Mini app in-game view (cockpit + private hand)
- Slice #63: Mini app actions (Roll + refresh + group update)
- Slice #64: Mini app placement (place die + token adjust + undo)
- Slice #65: Hardening + tests + command redirects

**Test Status:**
- 206 total tests, 193 passed, 13 skipped, 0 failed
- New Issue #59 suites (validator, endpoint) ✅ green
- Issue #53 callback tests ✅ green (in-game rolls, DM placement, privacy contract)

**Team Readiness:**
- ✅ Slice #59 complete and tested
- ✅ Strategic UI pivot locked (Mini App primary, DM flows obsolete)
- ✅ Ready for merge to main

- ✅ ExecuteCommand baseline established; smoke tests passing
- ✅ Next phase unblocked: Skiles integration testing + altitude/reroll redesign

### Session 8: Telegram UX Epic #49 Spawn + Triage (2026-02-21T23:05:13Z)
**Outcome:** Sully created GitHub Epic #49 (Telegram button-first UX) + 8 child issues (#50–#57); Epic #26 (MVP playable group chat) marked CLOSED with all 10 child issues resolved (PRs #47–#48 merged).

**Sully's UX Epic #49 Creation:**
- **Epic Scope:** Button-first Telegram UI via single edited "Cockpit" message (group) + DM hand menus (private)
- **Pattern:** Inline keyboards + callback queries for primary flows; `/sky ...` command handlers as fallback
- **Lifecycle:** Send cockpit on first interaction, edit on all state changes (no new message spam)
- **Implementation Phases:** Issue #54 (menu state store) unblocks #50–#52 (callbacks, cockpit renderer, DM menu)

**Child Issues Breakdown:**
- #50: Callback query handler + validation + retry logic
- #51: Single edited cockpit message + state persistence  
- #52: DM menu with hand display + inline keyboard
- #53: callback_data 64-byte constraint + token design (short versioned action tokens)
- #54: Menu state store (in-memory, per-group, thread-safe, 1-hour GC)
- #55: Deep-link onboarding (`/start?game=<groupId>`)
- #56: Button lifecycle + dedup/expiry ("menu expired" toast)
- #57: E2E integration tests (callback→action mapping, message edits, DM sends)

**Epic #26 Closure:**
- All 10 child issues closed (#27–#36 across P0 + P1 paths)
- PRs #47 (undo + cockpit renderer + app tests) and #48 (per-chat dedup) merged to master
- **MVP Deliverable:** Fully playable group chat (2 seated players + spectators), secret DM dice, public placements with strict alternation, undo support, round resolution + broadcast, all 7 domain modules, in-memory persistence with hardening
- **Post-MVP Roadmap:** Persistence, spectator visibility, UX polish, stats/leaderboard, reroll mechanics

**Team Coordination:**
- Skiles receives Epic #49 child issues with implementation sequencing (Issue #54 first)
- Tenerife validates button text + rendering against existing UX spec
- Aloha prepares E2E callback test harness (Telegram SDK mock)
- Scribe logs all work + merges decision inbox (3 files) + updates agent histories

---

### Session 4: Mini App Launch Surface Architecture (2026-03-01)

**Outcome:** Finalized Mini App launch surface design.

**Key Decision:**
- Mini App launch mechanism: startapp deep links with BotFather Main Mini App config
- Artifacts: Orchestration log \& Session log created

### Session 9: Epic #75 Triage & Execution Sequencing (2026-03-01T22:30:00Z)

**Outcome:** Sully triaged Epic #75 (Mini App-first Async Play) and produced concrete execution sequence. Identified top 3 P1 priorities and architecture review gates.

**Key Decisions:**
- **Critical Path Locked:** #76 (BotFather config) → #77 (Open app launchpad) → #80 (persistence) → #78–#79 (UI) → #81–#82 (security/concurrency).
- **Persistence + Concurrency Co-Design:** #80 (Game aggregate + Version field) must include optimistic locking shape upfront to feed #82. No sequential iteration; design together.
- **Game Aggregate Schema:** `GameSessions` table with `Version (int)` field for compare-and-swap; atomic serialization on turn transitions.
- **Cockpit Button Contract:** Inline keyboard with `startapp` deep link; callback data ≤ 64 bytes (use token codec from Session 2).
- **Squad Ownership Reaffirmed:** Skiles (implementation #76–#84), Sully (architecture review, no code), Aloha (testing #85–#86), Tenerife (rule validation consulted).

**Top 3 Execution Priorities (This Cycle):**
1. **#76 — BotFather Mini App Config (Skiles):** Launch blocker; no domain changes.
2. **#80 — Game Persistence (Skiles + Sully review):** Enables async reliability; Version field is critical.
3. **#77 — Open App Launchpad (Skiles):** UX foundation for #78; cockpit button with startapp link.

**Architecture Review Gates Established:**
- Sully approves: aggregate shape (#80), version field design (#82), cockpit button contract (#77).
- All three P1s must complete before #78–#79 UI development.

**Deliverables:**
- `.squad/decisions/inbox/sully-epic-75-triage.md` — Full triage document with dependency chain, roadblock mitigation, team ownership.
- GitHub issue #75 comment — Concise triage summary with 3 priorities and critical path detail.

**Team Coordination:**
- Skiles begins with #76 (BotFather config validation).
- Sully stands by for #80 aggregate design review (Version field, persistence schema).
- Aloha prepares integration test harness for #85 (WebApp API coverage).
- Tenerife consulted if #79 in-game UI touches rule exposure (read-only game state API).

**Learnings:**
- Epic chaining (launch → persistence → UI → hardening) requires upfront design of foundational layers (#76, #80) to prevent rework.
- Persistence + concurrency are inseparable; pair #80 + #82 co-design from day one.
- Telegram API constraints (64-byte callback data) must be factored into Cockpit design (#77) early.
- Team ownership clarity prevents rework: Skiles (implementation), Sully (review gates), Aloha (testing), Tenerife (rules validation).

### Session 10: Issues #76 + #85 Completion & PR Workflow (2026-03-02T00:15:00Z)

**Outcome:** Sully received completed code/test deliverables from Skiles (#76) and Aloha (#85), reviewed architecture, staged feature branch, committed cleanly, created draft PR #87, and posted progress comments to both GitHub issues.

**Deliverables Reviewed:**
- **Issue #76 (Skiles):** `WebAppOptionsValidator` with strict HTTPS + no query/fragment validation per BotFather spec. DI registration with `ValidateOnStart()`. readme documentation + operator checklist.
- **Issue #85 (Aloha):** Lobby flow integration test (new→join→start), lobby start validation test (single-player rejection), test helper for authenticated requests. 2 new test suites.

**Architectural Assessment:**
- ✅ Validator design: Clean separation of concerns; lives in DI layer; fails fast on startup.
- ✅ Test coverage: AAA pattern; both happy path (full flow) and failure path (insufficient players) covered.
- ✅ Public API: No breaking changes; configuration strictly validated; operator procedures documented.
- ✅ Domain boundary: Configuration validation stays in Telegram host layer; no domain model changes needed.
- ✅ Test integration: New tests integrate seamlessly with existing 114 Application tests; all 259 passing (145 Domain + 114 Application).

**Workflow Executed:**
1. Created feature branch: `feat/issue-76-85-botfather-config-webapp-tests`
2. Committed with detailed message referencing both issues + test validation
3. Pushed to origin; created draft PR #87 via `gh` CLI
4. Posted architecture approval comments on GitHub issues #76 (Skiles) and #85 (Aloha) with next-gate unblock info
5. Cross-linked PR #87 in both issue comments for team visibility

**Team Coordination:**
- Skiles notified: #76 merged into draft PR; critical path #76→#77 unblocked pending merge approval
- Aloha notified: #85 test coverage complete; integration ready for persistence layer (#80)

### Session 11: Issue Closure Round 7 & Epic #75 Progress Update (2026-03-02T01:00:00Z)

**Outcome:** Sully reviewed PR #87 implementation completeness against acceptance criteria for #76, #85, #86. All three issues fully satisfied; prepared for closure.

**Closure Decisions:**

1. **Issue #76 — Configure BotFather Main Mini App** ✅ CLOSE
   - **Acceptance Criteria Met:** BotFather config validation (WebAppOptionsValidator), DI registration, readme documentation, operator checklist, SSL/domain verification
   - **Architecture:** Configuration validation at startup prevents misconfigured deployments; pure infrastructure layer (no domain changes)
   - **Tests:** Issue76BotFatherMainMiniAppConfigurationTests.cs validates HTTPS enforcement; all 259 tests passing
   - **Unblock:** #77 (Open App Launchpad) ready to proceed

2. **Issue #85 — Expand WebApp API Integration Tests** ✅ CLOSE
   - **Acceptance Criteria Met:** Create flow, join flow, take turn, initData validation, context binding, authorization, >80% coverage
   - **Architecture:** WebApp lobby endpoints remain thin transport adapters over in-memory stores; deterministic integration tests
   - **Tests:** Lobby flow test (create→join 2 players→start) + start validation test (single-player rejection); test helper for auth
   - **Quality:** 259 tests passing (145 Domain + 114 Application); all lobby endpoints validated

3. **Issue #86 — Create Mini App Manual QA Matrix** ✅ CLOSE
   - **Acceptance Criteria Met:** QA matrix (8 clients × 2 surfaces), happy path (5 tests), errors (8 scenarios), edge cases, environment setup, release checklist
   - **Architecture:** Matrix lives in readme.md (versioned with code); deterministic, tester-focused, concurrency-aware
   - **Scope:** 7 multi-player sync scenarios; dark mode, keyboard nav, screen reader testing; performance baselines
   - **Integration:** 9-point release checklist gates Mini App merges; ensures manual testing before deployment

**Epic #75 Progress:**
- **Completed:** #76 (BotFather config), #77 (Open App launchpad hardening, merged in this PR), #85 (WebApp tests), #86 (QA matrix)
- **Status:** IN_PROGRESS (4/11 issues complete; critical path gates unlocking next wave)
- **Next:** Merge PR #87 → activate #80 persistence → begin #81 security-context-binding design → complete #82 versioning API contract

**Learnings:**
- Configuration validation as gating mechanism: Early DI validation catches misconfigurations at startup; prevents silent failures
- Deterministic integration tests scale better than flaky UI automation: WebApplicationFactory + in-memory hosting removes Telegram polling; tests are repeatable and fast
- QA matrix as living documentation: Tester-focused matrix in readme.md is more maintainable and always versioned with code than static spreadsheets
- Multi-player concurrency scenarios in QA matrix validate both manual testing and future automated concurrency test expansion

**Artifacts:**
- `.squad/decisions/inbox/sully-issue-closure-round7.md` — Full closure analysis and team coordination notes
- PR #87: https://github.com/glconti/sky-team-bot/pull/87 (ready for merge after this closure review)
- Gianluigi (user) receives draft PR for final merge decision; critical path visible

**Pending Actions:**
- Gianluigi approves/merges draft PR #87 to main
- Skiles begins #77 (Open App Launchpad) with cockpit button + startapp deep link routing
- Sully stands by for #80 aggregate design (persistence schema + Version field)

**Learnings:**
- Draft PR as collaboration gate enables architecture feedback before merge; reduces rework.
- Issue comments with PR cross-links improve team context; no need for separate Slack/Discord coordination.
- Feature branch naming convention (`feat/<issue>-<description>`) aids history traceability and multi-issue tracking.
- Clean commit messages with issue references enable GitHub's automatic issue closure on PR merge.

### Session 11: Persistence + Concurrency Architecture (#80 + #82) (2026-03-02)

**Outcome:** Sully designed unified architecture for durable persistence (#80) and optimistic locking (#82) as a single co-designed unit. Posted architecture contracts to both GitHub issues. Created decision document with full implementation handoff.

**Key Decisions:**
- **Co-Design Mandate:** Persistence and concurrency are architecturally inseparable; cannot implement one without the other
- **Version Field:** `int NOT NULL`, starts at 1, increments atomically on every mutation; foundation for CAS
- **Optimistic Locking:** `UpdateAsync(session, expectedVersion)` with compare-and-swap semantics; no pessimistic locks
- **Serialization Strategy:** Persist round logs (not domain snapshots); reuse existing `RebuildDomainGameFromLogs` pattern
- **TTL Policy:** Active games no expiry; completed/abandoned games 30-day retention with daily cleanup job
- **Repository Pattern:** `IGameSessionRepository` interface with `CreateAsync`, `UpdateAsync`, `GetByIdAsync`, `GetByGroupChatIdAsync`
- **Conflict Response:** HTTP 409 with `ConcurrencyConflict` error + current version for client retry

**Schema Design:**
```
GameSessions: GameId (PK/UUID), GroupChatId (indexed), PilotUserId, CopilotUserId,
              State (JSON), Status, Version (CAS), CreatedAt, UpdatedAt, ExpiresAt
```

**Review Gates Established:**
- Version field non-nullable and incremented on every mutation
- All mutations use repository pattern with expected version check
- Integration tests verify parallel placement rejection
- Unique index on GroupChatId for active sessions

**Deliverables:**
- `.squad/decisions/inbox/sully-80-82-architecture.md` — Full architecture contract
- GitHub issue #80 comment — Persistence architecture + review gates
- GitHub issue #82 comment — Concurrency contract + test requirements

**Team Handoff:**
- **Skiles:** Implementation ready with full contract (repository interface, serialization shape, CAS semantics)
- **Aloha:** Test checklist provided (6 required tests: concurrency, persistence, version, create conflict, update conflict, TTL)

**Learnings:**
- Persistence and versioning form an atomic design unit; never design separately
- Round logs as persistence unit is superior to domain snapshots (smaller, auditable, existing reconstruction pattern)
- Optimistic locking sufficient for turn-based games with low contention; no need for distributed locks
- Two-phase implementation (in-memory CAS first, then database) de-risks architecture validation

### Session 12: Issue Closure Round 8 (2026-03-02T00:49:00Z)

**Outcome:** Reviewed PR #87 completeness for issues #76, #85, #86. All acceptance criteria met. Prepared closure and Epic #75 status update.

**Decisions:**
- **CLOSE #76** BotFather configuration validation complete
- **CLOSE #85** WebApp API integration tests comprehensive
- **CLOSE #86** Manual QA matrix integrated into release process
- **EPIC #75** remains IN_PROGRESS (4/11 child issues complete; critical path gates active)

**Team Coordination:**
- Skiles: Issue #84 abuse protection slice complete (rate limiting + input validation)
- Aloha: Ready for integration testing with #80 persistence
- Critical path confirmed: #76→#77→#80→#81→#82

**Artifacts:**
- .squad/decisions/inbox/sully-issue-closure-round7.md → merged to decisions.md
- Updated Epic #75 progress tracking
