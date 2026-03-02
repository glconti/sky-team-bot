# Skiles — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Cross-Team Status (2026-03-02T00:58:00Z) — Round 9 Completion
- **Sully:** Closure audit complete for #77/#80/#83/#84/#75. All 4 targets remain OPEN; PR #87 closes only #76/#85 (18% epic completion). Next: Design #81; review #82 API.
- **Skiles (You):** Issue #81 first slice COMPLETED. Chat/game binding enforced at app boundary via `(groupChatId, userId)` mutations. Tests + commit 70d84ef posted. Full scope remaining.
- **Aloha:** Standby (Issue #80 QA coverage completed in prior round).
- **Tenerife:** Standby.
- **Critical Path:** #80 (done) → #81 (slice 1 done, full scope pending) → #82 (design + implementation) → #77 (UI).
- **Blockers Resolved:** Sully's closure audit clears no issues; #81 first slice unblocks full #81 implementation. #82 remains blocked on #81 completion.
- **Next:** Merge PR #87; finalize #81 security-context-binding design; implement #81 full scope + #82 versioning/concurrency APIs.

## Learnings

### Session 1: Codebase Audit & Milestone 1 Planning (2026-02-21)

**Current State Summary:**
- Game loop foundation is ~40% in place, with skeleton code and working value objects
- All 19 unit tests passing (Die, Altitude, Airport domain logic)
- ✅ Working: Die rolls (BlueDie/OrangeDie), Altitude progression, Airport/PathSegment queue, GameModule interface, AxisPositionModule wired with placeholders
- ❌ Missing: GameState aggregating dice, ExecuteCommand implementation, remaining 5+ modules (Engines, Brakes, Flaps, LandingGear, Radio, Concentration), command dispatch binding, win/loss conditions
- Code health: Guard clauses solid, early returns good, but incomplete command implementations throw NotImplementedError

**Proposed Milestone 1 (Base Game Working) — 4 Phases:**
1. **GameState entity** – Aggregate for unused dice per player (refactor from Game.cs) — **PHASE 1 BLOCKER**
2. **ExecuteCommand dispatcher** – Route commands to modules, remove dice, validate turn flow — **PHASE 1 BLOCKER**
3. **Engines + Brakes modules** – Implement with Tenerife-approved rules (parallelizable after Phase 1)
4. **Win/Loss logic** – Landing success, crash detection, reroll mechanics
5. **Round flow complete** – Integrate all loops: roll → assign → advance → reroll/land
6. **Remaining modules** – Flaps, LandingGear, Radio, Concentration (Phase 4)

**Dependency Graph (Critical):**
- **Phase 1 (Foundation):** GameState + ExecuteCommand (1–2 hours) — MUST COMPLETE FIRST
- **Phase 2 (Modules):** Engines, Brakes (parallel, awaiting Tenerife rules)
- **Phase 3 (Round flow):** Win/Loss, landing validation, reroll mechanics
- **Phase 4 (Remaining):** Flaps, LandingGear, Radio, Concentration

**Known Risks:**
- Game.cs mixes aggregate + command orchestration (architectural smell); GameState refactor needed
- AxisPositionModule command constructors still throw NotImplemented; need closure on design
- No tests for Game.ExecuteCommand yet (tests exist but reference GameState not Game)
- Random seeding in Die.Roll() makes tests non-deterministic if many games rolled in sequence (acceptable for now, flag for future)

### Cross-Team Context
- **Sully** established label taxonomy and GitHub backlog
- **Tenerife** provided comprehensive M1 rules and module specifications
- **Aloha** preparing test harness for module implementations

### Session 2: Coffee Tokens Domain Modeling (2026-02-21)
**Outcome:** Designed minimal immutable CoffeeTokenPool value object and GameState integration for Concentration module coffee token mechanic.

**Key Decisions:**
- **CoffeeTokenPool:** Immutable record with Count (0–3), Spend() (throws if empty), Earn() (capped at 3), CanSpend predicate
- **GameState ownership:** TokenPool property + EarnCoffeeToken() / SpendCoffeeToken() methods (shared across players)
- **PlaceDieOnConcentrationCommand:** UseTokenForAdjustment boolean, AdjustedValue optional, Validate() method for invariants
- **ConcentrationModule.PlaceDieOnConcentration():** Validate, deduct token if adjusted, place die, earn token immediately
- **Secret storage:** PendingPlacement internal class for Telegram secret assignments

**Design Principles:**
- Immutability enables auditing, replay, deterministic testing
- GameState-level placement: tokens are shared; natural at aggregate root
- Immediate earn-after-spend: matches board game flow
- Command-driven adjustment: UI chooses value; domain validates
- Explicit guard clauses prevent overspend

**Delivered Artifacts:**
- Minimal domain modeling spec with C# reference implementation (`.squad/decisions.md`)
- Implementation checklist (8 items)
- Test categories enumerated (token count, spend failures, boundaries, immutability, secret storage)
- One orchestration log entry: Skiles (token modeling)

**Ready to Code:**
- [ ] CoffeeTokenPool value object
- [ ] GameState.TokenPool integration
- [ ] PlaceDieOnConcentrationCommand + validation
- [ ] ConcentrationModule implementation + secret storage

**Cross-Coordination:**
- **Tenerife** finalized official rules spec (multi-token pending clarification)
- **Sully** confirmed architecture fit — minimal changes needed
- **Aloha** can now write token-specific test suite

### Core Context: Foundational Sessions (2026-02-21 through 2026-02-22)

**Key Achievements:**
- ✅ Telegram architecture: 5-layer design (Domain → Application → Presentation → Adapter → Host)
- ✅ `SkyTeam.TelegramBot` project: Created + integrated into solution
- ✅ Application layer primitives: `Round`, `PlayerSeat`, `DieValue`, `SecretDiceHand`, `RoundTurnState`
- ✅ Telegram implementation: `/sky undo` + private chat guarding + hand refresh
- ✅ Callback architecture: Query handler pattern + versioned action tokens + server-side state store
- ✅ Domain implementations: All 7 modules + landing validation + multi-token spend
- ✅ WebApp foundation: ASP.NET Core SDK conversion + TelegramInitDataValidator + TelegramInitDataFilter + GET /api/webapp/game-state
- ✅ Test coverage: 206 total, 193 passed, 13 skipped, 0 failed

**Telegram Pattern Summary:**
- Single edited cockpit message (group) + DM hand menus (private)
- Callback versioning: `v1:action:index` for 64-byte constraint compliance
- Menu state store: In-memory per-group with 1-hour GC
- Deep-link onboarding: `/start?game=<groupId>`
- Idempotency tracking: Prevents duplicate actions on retry

**WebApp Design:**
- Mini App as primary UI; group chat is launch surface only
- Secrets stay inside Mini App (no DM placements)
- Public state endpoint: `GET /api/webapp/game-state?gameId=...` with HMAC validation
- TelegramInitDataValidator: 5-minute freshness window, constant-time comparison
- Backward compatible with existing Telegram callback routing
- Sully created Epic #49 + child issues, routed to Skiles
- Tenerife validates button rendering against UX spec
- Aloha designs E2E callback test harness (mock Telegram SDK)
- Scribe logs all work + merges decision inbox (3 files)

### Session 9: Issue #50 CallbackQuery plumbing + safe Refresh (2026-02-22)

**Outcome:** Implemented end-to-end callback plumbing in `SkyTeam.TelegramBot\Program.cs` for both `Message` and `CallbackQuery` updates, added a minimal callback router with `v1:grp:refresh`, and wired Refresh to edit the originating group state message.

**Key Learnings:**
- Reusing the existing per-group lock model for callback queries is straightforward by deriving lock keys from callback message chat (or mapped user→group fallback), preserving message/callback serialization consistency.
- A shared `RenderGroupState(groupChatId)` path keeps `/sky state` fallback and callback refresh behavior aligned, avoiding duplicate state rendering logic.
- Always answering callback queries (success and error/expired) is easiest when callback handlers are treated as no-throw UX operations with a clear recovery toast: `Menu expired — press /sky state`.

### Session 10: Issue #51 Cockpit lifecycle + auto-pin (2026-02-22)

**Outcome:** Added cockpit lifecycle management in `SkyTeam.TelegramBot\Program.cs` with edit-first refresh, recreate-on-edit-failure fallback, and best-effort pinning; persisted one cockpit message id per group in `InMemoryGroupGameSessionStore`.

**Key Learnings:**
- A single `RefreshGroupCockpitAsync()` entry point keeps `/sky` command flows and callback refresh behavior consistent and reduces drift in lifecycle handling.
- Treating `EditMessageText` failures as non-fatal and recreating cockpit immediately is a robust strategy for deleted/uneditable message recovery.
- Pinning should remain side-effect-only (`try/catch` ignore), so state refresh is never blocked by missing Telegram pin permissions.

### Session 11: Publish prep for issues #50 and #51 (2026-02-22)

**Outcome:** Prepared draft PR publication flow by consolidating #50 callback plumbing and #51 cockpit lifecycle changes on a dedicated branch, with focused test execution evidence.

**Key Learnings:**
- Keeping callback refresh and `/sky state` on the same cockpit refresh pipeline reduces divergence during publish/review.
- Persisting cockpit message id in application state is sufficient to support edit-first lifecycle without introducing infrastructure coupling.

### Session 12: Issue #52 Slice 3/7 — Lobby cockpit buttons (2026-02-22)

**Outcome:** Implemented group cockpit lobby buttons end-to-end in `SkyTeam.TelegramBot\Program.cs`: `New`, `Join`, `Start`, `Refresh` callbacks now route through server-side validation, invalid presses no-op with callback toast, and successful presses refresh by editing the cockpit.

**Key Learnings:**
- Keeping lobby buttons always visible is compatible with role/seat safety as long as callback handlers enforce legality via existing lobby/session stores.
- Callback UX should fail softly (`AnswerCallbackQuery` toast) while text command fallbacks (`/sky new|join|start`) remain unchanged and cockpit-refreshing.

### Session 13: PR #58 publish for issue #52 (2026-02-22)

**Outcome:** Published issue #52 implementation on draft PR #58 by committing/pushing lobby callback + tests changes, updating PR scope/checklist with test evidence, and posting issue status with PR linkage.

**Key Learnings:**
- Publishing in-place on the existing draft branch keeps review continuity for #50/#51/#52 cockpit work.
- Test evidence is strongest when combining executable checks and explicit skipped-contract rationale for remaining callback test seams.

### Session 14: Slice #59 WebApp foundation (2026-02-22)

**Outcome:** Converted `SkyTeam.TelegramBot` into an ASP.NET Core host serving `wwwroot/` + `/api/webapp/*` while continuing Telegram update processing via a `BackgroundService`; added Telegram Mini App `initData` validation and a read-only `GET /api/webapp/game-state` endpoint per Sully contract.

**Key Learnings:**
- Convert host in-place (Web SDK) to keep in-memory stores shared via DI; move polling logic into hosted service to avoid static singletons.
- Telegram `initData` validation is sensitive to HMAC key/data order (`secret_key = HMAC("WebAppData", bot_token)`); use constant-time compare and `auth_date` max age.
- Tests that reflect on `Program` break with top-level statements; point brittle source-string assertions at a stable class (`TelegramBotService`) instead.

### Session 3: Mini App Button Implementation (2026-03-01)

**Outcome:** Proposed web_app button approach with signed chat context for Mini App launch.

**Key Decision:**
- Use InlineKeyboardButton.web_app for group cockpit Open app button
- Derive game/group from signed chat.id; fallback to start_param for private launches
- Artifacts: Orchestration log \& decision merged into decisions.md

### Session 15: Issue #76 BotFather Main Mini App guardrails (2026-03-01)

**Outcome:** Delivered first actionable in-repo slice for BotFather Main Mini App setup: runtime URL validation, focused tests, and operator runbook docs.

**Architecture / Patterns:**
- Added `IValidateOptions<WebAppOptions>` + `ValidateOnStart()` to fail fast when Mini App URL config is malformed.
- Validation rule chosen for operator safety: Mini App URL must be absolute HTTPS and must not include query/fragment.
- Kept external BotFather actions out of runtime code; captured as explicit manual checklist in docs and decision inbox.

**User / Product Preferences Captured:**
- Mini App-first launch experience is preferred over DM-first secret flows.
- `startapp` deep-link syntax and BotFather Main Mini App setup must be explicitly documented for operators.

**Key Paths:**
- `SkyTeam.TelegramBot\WebApp\WebAppOptionsValidator.cs`
- `SkyTeam.TelegramBot\Program.cs`
- `SkyTeam.Application.Tests\Telegram\Issue76BotFatherMainMiniAppConfigurationTests.cs`
- `readme.md`
- `.squad/decisions/inbox/skiles-issue-76.md`

### Session 16: Issue #80 durable game persistence slice (2026-03-02)

**Outcome:** Added the first durable persistence vertical slice for active game sessions with safe reload after restart.

**Key Learnings:**
- Replaying per-round dice + placement logs is enough to reconstruct domain game state deterministically without introducing domain-level serialization concerns.
- Persisting cockpit message ids with game sessions preserves edit-in-place cockpit lifecycle after process restart.
- Introducing a persistence port in `SkyTeam.Application` and implementing JSON persistence in `SkyTeam.TelegramBot` keeps DDD boundaries clear while enabling future DB-backed repositories.

**Delivered Artifacts:**
- `SkyTeam.Application\GameSessions\GameSessionPersistence.cs`
- `SkyTeam.Application\GameSessions\InMemoryGroupGameSessionStore.cs` (rehydration + versioned snapshot export/import)
- `SkyTeam.TelegramBot\Persistence\JsonGameSessionPersistence.cs`
- `SkyTeam.Application.Tests\GameSessions\InMemoryGroupGameSessionStoreTests.cs` (persistence round-trip)
- `.squad/decisions/inbox/skiles-issue-80.md`

### Session 17: Issue #80 publish (2026-03-02)

**Outcome:** Committed and pushed durable persistence vertical slice on feat/issue-76-85-botfather-config-webapp-tests branch; consolidated with #76 + #85 work in draft PR #87.

**Scope:**
- All #80 acceptance criteria met: Game state survives restart, sessions keyed by (ChatId, GameId), round-trip persistence validation passing
- Deferred to #82: Version field + optimistic locking, TTL cleanup, database migration (beyond initial vertical slice scope)

**Executed Actions:**
1. Committed 8 file changes (395 insertions) with clean message: `feat(issue-80): Durable game session persistence...`
2. Pushed branch to origin; +1 commit on PR #87 (now 7 total commits, 4680 additions)
3. Documented in history (this entry)
4. Created decision document for team reference

**Test Status:** All 273 tests passing (includes round-trip persistence + deterministic rehydration)

### Session 18: Issue #77 launchpad hardening slice (2026-03-02)

**Outcome:** Started Issue #77 on PR #87 branch with an in-group launchpad hardening slice focused on robust Open app deeplinks and safe fallback behavior.

**Key Learnings:**
- In-group Open app reliability improves when cockpit launch uses `startapp=<groupChatId>` consistently instead of mixing launch surfaces.
- Deeplink safety should be explicit in transport code: validate bot username shape and chat id before emitting Telegram URLs.
- When launch URL generation is unsafe/unavailable, preserving group fallback (`Refresh` + `/sky state`) keeps users in Mini App-first flow without DM-first drift.

**Delivered Artifacts:**
- `SkyTeam.TelegramBot\TelegramBotService.cs`
- `SkyTeam.Application.Tests\Telegram\Issue60LaunchMiniAppButtonTests.cs`
- `readme.md`
- `.squad/decisions/inbox/skiles-issue-77.md`

### Session 19: Issue #83 async turn notifications slice (2026-03-01)

**Outcome:** Started issue #83 with a minimal async turn notification implementation wired into existing Telegram + WebApp flows.

**Key Learnings:**
- A Telegram-side notification orchestrator can stay minimal by deriving active-turn context from `GetSnapshot` + `GetPublicState`, avoiding immediate domain-event refactors.
- Transition-key deduplication (`roll/place/undo`) is a low-risk first idempotency layer that fits existing in-memory state patterns.
- DM-first with group fallback keeps async responsiveness high while preserving delivery when private chat permissions block direct messages.

**Delivered Artifacts:**
- `SkyTeam.TelegramBot\TelegramBotService.cs`
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs`
- `readme.md`
- `.squad/decisions/inbox/skiles-issue-83.md`

### Session 20: Issue #83 async turn notifications completion (2026-03-02)

**Outcome:** Completed Issue #83 with full async turn-notification slice in `SkyTeam.TelegramBot` with DM-first + group fallback policy and transition-key deduplication.

**Key Learnings:**
- Idempotency guard via bounded in-memory dedup cache (groupChatId + transitionKey + recipientUserId + seat) prevents notification duplicates on retries.
- Transport-driven notifications stay minimal; full domain event infrastructure deferred to future slices.
- Public turn summaries + action prompts protect secret information while maintaining async responsiveness.

**Delivered Artifacts:**
- `SkyTeam.TelegramBot\TelegramBotService.cs` (notification orchestration)
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs` (WebApp roll/place/undo triggers)
- `readme.md` (policy documentation)
- Test coverage: All passing
- Commit: d91925f
- Issue comment posted; decision merged into decisions.md

### Session 21: Issue #84 abuse protection + input validation slice 1 (2026-03-02)

**Outcome:** Started issue #84 on PR #87 with minimal, production-useful guardrails in the WebApp transport layer.

**Key Learnings:**
- A singleton in-memory sliding-window guard (`IEndpointFilter`) is an effective first abuse-protection layer without introducing infrastructure dependencies.
- Keeping abuse controls in transport filters preserves domain purity while still enforcing practical per-user/per-IP limits.
- Input validation guardrails are safest when enforced at endpoint boundaries with explicit `400` errors and no domain-state side effects.

**Delivered Artifacts:**
- `SkyTeam.TelegramBot\WebApp\WebAppAbuseProtector.cs`
- `SkyTeam.TelegramBot\WebApp\WebAppAbuseProtectionFilter.cs`
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs`
- `SkyTeam.TelegramBot\WebApp\TelegramInitDataFilter.cs`
- `SkyTeam.TelegramBot\Program.cs`
- `readme.md`
- `.squad/decisions/inbox/skiles-issue-84.md`

### Session 22: Issue #84 Abuse Protection Slice 1 (2026-03-02T00:49:00Z)

**Outcome:** Implemented minimal, production-useful abuse guardrails in WebApp transport layer without external infrastructure dependencies.

**Decisions:**
- **Rate Limiting:** Per-user 10 req/sec, per-IP 100 req/min, lobby creation 1 req/user/5min
- **Input Validation:** Oversized headers (>4096), invalid commandId, invalid display names
- **Logging:** Throttled requests + rejected initData with validation status
- **Pattern:** Endpoint filters in DI preserve DDD boundaries; domain untouched

**Scope Deferral (Future Slices):**
- Expand to /sky command + callback patterns
- Per-game action cadence/idempotency-key policy
- Distributed limiter for multi-instance deployments
- Comprehensive abuse telemetry/alerting

**Test Status:** All 273 tests passing

**Artifacts:**
- SkyTeam.TelegramBot\WebApp\WebAppAbuseProtector.cs
- SkyTeam.TelegramBot\WebApp\WebAppAbuseProtectionFilter.cs
- SkyTeam.Application.Tests\Telegram\*AbusProtectionTests.cs
- .squad/decisions/inbox/skiles-issue-84.md → merged to decisions.md
- Commit: 02293fe

### Session 23: Issue #81 chat/game binding first slice (2026-03-02T01:25:00Z)

**Outcome:** Started issue #81 by binding mutation calls to explicit `(groupChatId, userId)` context at the application boundary and wiring WebApp mutation endpoints to that context.

**Key Learnings:**
- User-only mutation lookup is unsafe once the same player is active in multiple group chats; explicit chat context is required for deterministic routing.
- Binding WebApp actions to signed request chat context + seated-player validation safely rejects cross-chat mutation attempts.
- Backward-compatible wrappers let us migrate callers incrementally without breaking existing test scaffolding.

**Test Status:** Focused suite passing (`InMemoryGroupGameSessionStoreTests` + `Issue64WebAppPlacement*`), solution build passing.

**Delivered Artifacts:**
- `SkyTeam.Application\GameSessions\InMemoryGroupGameSessionStore.cs`
- `SkyTeam.TelegramBot\WebApp\WebAppEndpoints.cs`
- `SkyTeam.Application.Tests\GameSessions\InMemoryGroupGameSessionStoreTests.cs`
- `SkyTeam.Application.Tests\Telegram\Issue64WebAppPlacementFlowTests.cs`
- `.squad/decisions/inbox/skiles-issue-81.md`

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
