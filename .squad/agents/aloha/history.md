# Aloha — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Cross-Team Status (2026-03-02T00:25:39Z)
- **Sully:** Issue #80/#82 architecture contract designed. Persistence contract stabilized; versioning scope deferred to #82. Next: #81 design + #82 API review.
- **Skiles:** Issue #80 vertical slice COMPLETED. Persistence + version tracking + tests passing. #82 versioning APIs pending design review.
- **Aloha (You):** Issue #80 QA coverage COMPLETED. Round-trip + deterministic concurrency validated. Version-conflict test skipped (blocked on #82 API).
- **Critical Path:** #80→#81 (security-context-binding) → #82 (versioning/concurrency) before UI integration.
- **Next:** Await #82 versioning API implementation; activate skipped version-conflict test; expand concurrency test suite.

## Learnings

### Session 1: Telegram Architecture + MVP Backlog Sprint (2026-02-21)
**Outcome:** QA recommendations prepared (verbal); test-backlog strategy ready for integration with implementation phases.

**Key Context:**
- **From Sully:** 5-layer architecture (Domain → Application → Presentation → Adapter → Host); 7 Epic MVP backlog (A–G) with vertical slices
- **From Tenerife:** Comprehensive UX spec (570+ lines, 7 example transcripts, secret placement, button-driven token mechanics)
- **From Skiles:** Project created (`SkyTeam.TelegramBot`); Phase 1 blocker identified (GameState + ExecuteCommand)

**QA Strategy (Recommendations):**
- **Unit tests:** Token pool invariants (spend/earn boundaries), command validation (token availability, adjusted value ranges), module resolution order
- **Integration tests:** Secret placement + reveal flow (both players submit → bot broadcast), token reconciliation (spend -1 + earn +1 = 0 net), timeout handling
- **E2E tests:** 7 deterministic transcripts from Tenerife spec (simple round, token spend, reroll, landing/victory, collision, axis imbalance, concentration net-zero)
- **Edge case coverage:** Token pool 0 & spend (guard), reroll unavailable (prevent), pilot bad roll (timeout), radio over-clear (cap at 0), altitude at 6000 round 7 (no landing)

**Test-Backlog Structure (by Epic):**
- **Epic A (Foundation):** ChatMessage/ChatKeyboard models; application ports (IChatGateway, IGameSessionRepository, IDiceRoller)
- **Epic B (Transport):** Callback handling; DM onboarding detection
- **Epic C (Session):** Session creation, player assignment (/join), status display (/state)
- **Epic D (Turn/Round):** Secret submission (DM); alternating assignment; readiness gate; timeout policy
- **Epic E (Domain):** Module implementations; win/loss validation; reroll mechanics
- **Epic F (Presentation):** Cockpit rendering; module resolution output; landing result formatting
- **Epic G (Hardening):** Concurrency safety; idempotency keys; admin diagnostics

**Pending Actions:**
- User answers Sully's 8 interview questions (will refine test strategy for UX edge cases)
- Skiles implements Phase 1 (test framework integration points)
- Aloha begins E2E test harness prep (Tenerife's 7 transcripts as golden tests)

### Session 3: Issue #31 Completion Round (2026-02-21T10:21:03Z)
**Outcome:** Prepared draft PR #38 (squad/31-domain-tests) with comprehensive test coverage for all 7 modules per Tenerife's final spec. Identified spec mismatches and working assumptions that require reconciliation.

**Test Coverage Added:**
- **Axis:** Out-of-bounds immediate resolution (< -2 or > +2), boundary positions (-2, +2)
- **Landing Outcome Matrix:** 1 passing WIN case + 1 focused LOSS case per criterion
  - Axis out-of-bounds at landing
  - Engines thrust < 9
  - Brakes not fully deployed (< 3 switches)
  - Flaps not fully deployed (< 4 switches)
  - Landing Gear not fully deployed (< 3 switches)
  - Approach track not fully cleared (planes remain)
- **Concentration / Coffee Tokens:**
  - Token pool ctor bounds (0–3)
  - Earn/Spend transitions (including multi-token k=1, k=2)
  - Die value bounds (1–6, no wraparound)
  - Spend availability guard (cannot spend if pool == 0)

**Spec Mismatches Identified:**

1. **Brakes Landing Criterion Inconsistency** (Critical blocker for test finalization)
   - Tenerife spec states: `BrakesValue == 3 AND BrakesValue > LastSpeed`
   - Problem: BrakesValue is switch count (0–3); if LastSpeed ≥ 9 (requirement for Engines), condition `BrakesValue > LastSpeed` is mathematically unsatisfiable (3 ≯ 9)
   - Current implementation: Treats BrakesValue as last activated required value (2/4/6) and checks `BrakesValue >= 6` without speed comparison
   - **Recommendation:** Clarify intended landing check before finalizing tests:
     - Option A: All 3 switches deployed, no speed comparison (landing check only validates switch count == 3)
     - Option B: Sum/magnitude of switches (switch values 2+4+6 = 12) compared to speed (allowing BrakesValue > LastSpeed)
     - Option C: Different Brakes representation (e.g., sequential switch values as separate condition)

2. **Coffee-Token Die Adjustment Implementation**
   - `Game.GetAvailableCommands()` surfaces token-adjusted command IDs like `Axis.AssignBlue:1>3` when tokens available
   - Cost: `k = |effective - rolled|` tokens (supports multi-token spend)
   - `Game.ExecuteCommand()` spends required tokens, consumes rolled die, assigns effective value (bounded to 1–6, no wraparound)
   - Tests validate command surfacing, spend behavior, pool bounds (0–3), die-value bounds (1–6)
   - Design is validated and working; awaiting Telegram button rendering spec (how to surface token-adjusted options to user)

**Test Framework & Organization:**
- xUnit + FluentAssertions, AAA pattern
- Separate `SkyTeam.Domain.Tests` assembly (appropriate separation from `SkyTeam.Application.Tests`)
- Test method naming: `[Module]_[Behavior]_[Condition]` (e.g., `Axis_ImmediatelyLoses_WhenOutOfBounds`)

**Blockers:**
- **Brakes Landing Criterion:** Awaiting clarification from Tenerife/Skiles before finalizing test suite
- **Token-Adjusted Command Rendering:** Awaiting Telegram UX spec (Sully/Tenerife) on button options/display

**Pending Actions:**
- Tenerife + Skiles reconcile Brakes landing criterion semantics (switch count vs. magnitude vs. speed comparison)
- Sully provides Telegram button rendering spec for token-adjusted options
- Finalize PR #38 tests once Brakes criterion is clarified
- Merge PRs #37 + #38 after reviews pass

**Cross-Team Coordination:**
- **Tenerife:** Spec complete; awaiting reconciliation on Brakes landing criterion
- **Skiles:** PR #37 implemented; code supports both Brakes interpretations (currently checks >= 6)
- **Sully:** Awaiting code review of PR #37/38 for module design + command dispatcher + token UX surface

### Session 4: PR #37 Unblock & Loss Semantics Finalization (2026-02-21T18:06:26Z)
**Outcome:** Sully fixed token pool wiring in PR #37. Tenerife produced comprehensive loss condition checklist. Added ExecuteCommand smoke tests. Scribe logged all work and merged decisions.

**ExecuteCommand Smoke Tests Added:**
- Base-scenario coverage: valid command execution, token spend, invalid rejection
- AAA pattern, FluentAssertions, data-driven matrix
- Tests green; ready for broader integration suite
- Command validation: token availability, die placement legality, module state transitions

**Test Quality Improvements:**
- Deterministic test cases with mock dependency injection
- Isolated test per concern (token spend, command availability, die management)
- Edge case coverage: token exhaustion, invalid die placements, state desync detection
- Ready to merge with PR #37 once Sully validation complete

**Delivered Artifacts (Session 4):**
- `.squad/orchestration-log/2026-02-21T18-06-26Z-aloha.md` — ExecuteCommand smoke tests orchestration log
- `.squad/decisions.md` — Merged loss condition checklist (comprehensive taxonomy + bug findings)

**Pending Actions:**
- Validate ExecuteCommand implementation against smoke test suite
- Merge PR #37 + test PR once Sully approves
- Extend test coverage for reroll mechanics (blocked on implementation)

---

### Session 2: Issue #31 Domain QA (2026-02-21)
**Outcome:** Added fast, deterministic `SkyTeam.Domain.Tests` coverage for Tenerife’s locked rules spec: axis out-of-bounds immediate loss, landing win/loss criteria matrix, and coffee-token/die boundary cases.

**Notes / Learnings:**
- Axis resolution must fail fast when the resulting position is `< -2` or `> +2`.
- Landing result is an AND of criteria (approach cleared, axis in range, engines ≥ 9, brakes/flaps/gear fully deployed); a single failure yields LOSS.
- Brakes landing criterion appears inconsistent between spec and code; captured in `.squad/decisions/inbox/aloha-issue31-test-findings.md`.

---

### Session 5: Issue #36 — application turn/undo invariants (2026-02-21)
**Outcome:** Mapped application orchestration to `RoundTurnState` + `InMemoryGroupGameSessionStore` and added tests for turn alternation, secret-hand mutation isolation, and command gating.

**Where orchestration is exercised (today):**
- `SkyTeam.Application\\Round\\RoundTurnState.cs` — strict alternation, placement tracking, undo gating, ready-to-resolve transition.
- `SkyTeam.Application\\GameSessions\\InMemoryGroupGameSessionStore.cs` — per-group session wiring: hand exposure + placement orchestration + resolve/advance.
- Tests live in `SkyTeam.Application.Tests`:
  - `SkyTeam.Application.Tests\\Round\\RoundTurnStateTests.cs`
  - `SkyTeam.Application.Tests\\GameSessions\\InMemoryGroupGameSessionStoreTests.cs`

**Concrete test plan (Issue #36):**
- Turn alternation:
  - `GetHand_ShouldReturnNoCommands_WhenRequestingPlayerIsNotCurrentPlayer` (implemented)
  - `PlaceDie_ShouldReturnNotPlayersTurn_WhenRequestingPlayerIsNotCurrentPlayer` (implemented)
- Secret hand mutation:

### Session 6: Slice #59 — WebApp Integration Tests & Issue #53 Callback Tests (2026-02-22)

**Outcome:** Implemented comprehensive test suites for Telegram Mini App auth and in-game callback routing. All Slice #59 + Issue #53 tests pass.

**Deliverables:**
1. **Issue59WebAppInitDataValidationTests.cs:**
   - Unit tests for TelegramInitDataValidator
   - Valid initData → success with userId, displayName, start_param
   - Tampered hash → InvalidHash
   - Expired auth_date → Expired
   - Missing hash field → InvalidHash
   - Empty/null initData → failure
   - Deterministic signature generation using test bot token
   - Constant-time comparison code review verified

2. **Issue59WebAppGameStateEndpointTests.cs:**
   - Integration tests using WebApplicationFactory<SkyTeam.TelegramBot.Program>
   - Disabled TelegramBotService polling in test host (via ConfigureWebHost override)
   - Seeded test lobby/game sessions into in-memory stores
   - Test scenarios:
     - Valid initData + existing lobby → 200 with phase="lobby"
     - Valid initData + existing game → 200 with phase="in_game", cockpit data
     - Valid initData + no game → 404 Not Found
     - Missing X-Telegram-Init-Data header → 401 Unauthorized
     - Invalid initData (bad hash, expired) → 401 Unauthorized
     - Mismatched gameId vs signed start_param → 400 Bad Request
     - Empty/malformed gameId → 400 Bad Request
   - Deterministic HMAC signatures matching production algorithm

3. **Issue53InGameCockpitButtonFlowTests.cs:**
   - Callback routing tests (Roll, Place(DM), Refresh)
   - Roll callback → edits cockpit message, renders updated state
   - Place(DM) callback → sends placement DM with onboarding hint if user lacks private chat
   - Refresh callback → re-renders cockpit without state change
   - Group privacy contract validation (no secret data leaks to group)
   - Fallback continuity (/sky roll, /sky hand commands still work)

**Test Strategy:**
- WebApplicationFactory pattern for realistic end-to-end testing
- Polling service disabled to avoid network calls; hermetic and fast
- Deterministic test data generation for reproducible HMAC validation
- AAA pattern with FluentAssertions for clarity

**Test Result:**
- **206 total tests, 193 passed, 13 skipped, 0 failed** ✅
- All Slice #59 suites green
- All Issue #53 callback suites green
- No blocking failures; CI ready for merge

**Integration Checkpoint:**
- Slice #59 validator + endpoint + Issue #53 callbacks all work together
- In-memory stores thread-safe under ASP.NET Core's multi-threaded requests
- Backward compatible with existing Telegram polling loop
- Ready for frontend shell (Gimli) + launch surface (Slice #60)

  - `PlaceDie_ShouldMarkOnlyRequestingPlayersDieUsed_WhenPlacementIsAccepted` (implemented)
- Undo gate rules:
  - `UndoLastPlacement_ShouldThrow_WhenRequestingPlayerIsNotLastPlacer` (implemented)
  - (pending #33) store-level undo orchestration tests once an undo API exists on `InMemoryGroupGameSessionStore`.

**Checklist once #33 lands (undo orchestration):**
- Add tests for store-level undo API (name TBD by implementation):
  - Should restore requesting player’s die to unused and rewind `CurrentPlayer`.
  - Should reject undo after opponent has played.
  - Should reject undo when round is `ReadyToResolve`.
- Ensure `GetHand`/available command exposure stays consistent after undo.

### Session 6: Issue #50 CallbackQuery test scaffolding (2026-02-22)
**Outcome:** Added xUnit + FluentAssertions callback-flow test scaffolding for issue #50 in `SkyTeam.Application.Tests\Telegram\Issue50CallbackQueryFlowTests.cs`.

**Learnings:**
- Current `SkyTeam.TelegramBot\Program.cs` routes only `Update.Message`; `CallbackQuery` routing is not implemented yet.
- Callback-specific acceptance criteria (#50) are now captured as explicit pending tests (skip-scaffold) for: callback route, spinner stop via `AnswerCallbackQuery` on success/error, refresh re-render path, graceful unknown/expired callbacks, and `/sky state` fallback validity.
- Minimal assumption used: once callback plumbing is implemented, tests can be unskipped and wired to the callback handler without reworking expected behaviors.

### Session 7: Issue #51 Cockpit lifecycle + auto-pin test scaffolding (2026-02-22)
**Outcome:** Added xUnit + FluentAssertions pending contract tests for issue #51 in `SkyTeam.Application.Tests\Telegram\Issue51CockpitLifecycleTests.cs`.

**Learnings:**
- Current `SkyTeam.TelegramBot\Program.cs` has refresh callback support but does not yet persist a per-group cockpit `message_id` lifecycle or auto-pin behavior.
- Issue #51 acceptance criteria are now captured as explicit skip-scaffold tests for: single cockpit message id persistence, edit-in-place updates, recreate-on-missing/uneditable flow, best-effort pin failure tolerance, and `/sky state` fallback refresh.
- Minimal assumption used: once cockpit lifecycle/auto-pin implementation lands, tests can be unskipped and wired without changing behavioral expectations.

### Session 8: Issue #52 lobby button flow tests (2026-02-22)
**Outcome:** Added `SkyTeam.Application.Tests\Telegram\Issue52LobbyButtonFlowTests.cs` and updated test project references so callback-contract tests can inspect `SkyTeam.TelegramBot` behavior.

**Learnings:**
- Verified current callback keyboard path exposes `Refresh` callback and retains `/sky state` fallback contract (`ExpiredMenuToast` + group `/sky state` handling).
- Captured explicit pending contracts (skip with rationale) for `New/Join/Start` callback paths, invalid press no-op side-effect assertions, and successful callback integration with existing handlers + cockpit edit lifecycle.
- Current implementation appears partial versus issue #52 target behavior; tests are now ready to be unskipped as callback handlers are completed.

### Session 6: Mini App Launch QA & Tests (2026-03-01)

**Outcome:** QA test suite for Mini App launch surface covering all acceptance criteria.

**Tests Added:**
- Mini App launch from group cockpit message
- Signed initData validation \& routing
- Menu state transitions after launch
- Static hosting HTTPS configuration

**Artifacts:**
- Orchestration log: 2026-03-01T21-55-06Z-aloha-miniapp-tests.md
- Test cases for launch flow, callback state, static serving

### Session 9: Issue #85 — WebApp API integration expansion (2026-03-01)
**Outcome:** Extended WebApp lobby integration coverage with a full create/join/start API flow and a negative-path start validation aligned with current endpoint behavior.

**Learnings:**
- **Architecture decision:** WebApp lobby endpoints remain thin transport adapters over `InMemoryGroupLobbyStore` + `InMemoryGroupGameSessionStore`, with conflict status mapping for non-ready start attempts.
- **Pattern:** Deterministic integration tests use `WebApplicationFactory<Program>` + in-memory bot token config and hosted-service removal to keep Telegram polling disabled.
- **User preference:** Keep diffs surgical and validate via direct `dotnet test` runs in `SkyTeam.Application.Tests`.
- **Key file paths:** `SkyTeam.Application.Tests\Telegram\Issue61WebAppLobbyEndpointsTests.cs`, `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs`.

### Session 10: Issue #80 — Persistence contracts + concurrency guard (2026-03-02)
**Outcome:** Added deterministic concurrency guard coverage in `InMemoryGroupGameSessionStoreTests` and captured durable persistence/version expectations as explicit issue #80 contract tests.

**Learnings:**
- Deterministic concurrency coverage can be achieved by releasing two same-die placement requests behind a shared gate and asserting one `Placed` + one `NotPlayersTurn`.
- Persistence round-trip QA became testable after the `IGameSessionPersistence` seam; rehydration behavior is now covered with an in-memory persistence double.
- Version-conflict QA is blocked until write APIs accept an expected version token and return a dedicated stale-write conflict status.
- When hooks are missing, keep momentum with skipped contract tests plus a concrete blocker handoff in `.squad/decisions/inbox/aloha-issue-80.md`.
