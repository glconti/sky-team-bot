# Skiles — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

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

### Session 3: Telegram Architecture + MVP Backlog Sprint (2026-02-21)
**Outcome:** Four agents drafted comprehensive Telegram bot architecture, UX specification, implementation plan; Skiles created `SkyTeam.TelegramBot` project; Sully produced 5-layer architecture + 7-Epic backlog + 8 user interview questions; Tenerife specified full Telegram UX (570+ lines, 7 transcripts).

**Key Decisions:**
- **Project Created:** `SkyTeam.TelegramBot` console app (references `SkyTeam.Domain` directly; adapter/application/presentation layers TBD per Sully architecture)
- **Architecture Drafted:** Domain → Application → Presentation → Telegram Adapter → Bot Host (5-layer clean separation)
- **Core Ports Defined:** `IChatGateway`, `IGameSessionRepository`, `IDiceRoller` (application-level contracts)
- **MVP Backlog Structured:** Epics A–G (foundation → transport → session → round interaction → domain completion → presentation → hardening)
- **UX Specification Locked:** Secret placement (DM-based), public reveal (group), button-driven token mechanics, 7 example transcripts

**Interview Questions for User (prioritized):**
1. DM onboarding required (players must `/start` bot privately)?
2. Strict alternation (one placement at a time) vs. submit-all?
3. Button/inline keyboard UX vs. typed commands?
4. Token transparency (announce immediately vs. round-end reveal)?
5. Persistence required across bot restart?
6. Undo policy (undo-last vs. cancel-round-only)?
7. 2+ humans in group: enforce 2 seated + spectators?
8. Must-have non-base-game UX (pin cockpit, auto-advance, reminders)?

**Delivered Artifacts:**
- `.squad/orchestration-log/2026-02-21T08-22-30Z-sully.md` — Architecture orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-31Z-skiles.md` — Project orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-32Z-tenerife.md` — UX orchestration log
- `.squad/orchestration-log/2026-02-21T08-22-33Z-aloha.md` — QA orchestration log
- `.squad/log/2026-02-21T08-22-00Z-telegram-bot-backlog.md` — Session log
- `.squad/decisions.md` — Merged 4 new decisions (user directive, architecture, project, UX spec)

**Pending Actions:**
- User answers interview questions (UX clarifications)
- Skiles begins Phase 1: GameState + ExecuteCommand (unblocks all downstream Epics)
- Tenerife validates module implementations vs. UX spec
- Aloha integrates test harness with implementation phases

---

### Session 4: Issue #31 Landing + Multi-Token Spend (2026-02-21)
**Outcome:** Implemented final landing win/loss criteria per `.squad/decisions.md` and added coffee-token multi-spend die adjustment end-to-end (command surfacing + execution + tests).

**Key Learnings:**
- Landing outcome must validate multiple independent thresholds (axis range, engines/brakes/flaps/gear, approach cleared); keeping them explicit in `Game.EvaluateLandingOutcome()` prevents regressions.
- Coffee token adjustment is simplest when encoded in command ids as `Rolled>Effective`, consuming the rolled die but assigning an effective-value die to modules.
- Centralizing the token budget at `Game.GetAvailableCommands()` (via `ConcentrationModule.TokenPool`) keeps modules token-aware without introducing infrastructure concerns.

### Session 5: Issue #31 Completion Round (2026-02-21T10:21:03Z)
**Outcome:** Skiles delivered draft PR #37 (squad/31-domain-base-modules-landing) with all 7 module implementations, landing win/loss validation, and coffee-token multi-spend. Tenerife finalized comprehensive 500+ line spec with all 7 modules, invariants, edge cases, and verification checklist. Aloha created draft PR #38 (squad/31-domain-tests) with boundary conditions, landing outcome matrix, and token mechanics tests.

**Team Coordination:**
- **Tenerife's Spec Lock:** Module resolution order (Axis → Engines → Brakes → Flaps → Gear → Radio → Concentration), 6 landing criteria (all must pass for win), 3 immediate loss conditions, multi-token spend cost formula
- **Aloha's Test Findings:** Identified Brakes landing criterion inconsistency (BrakesValue switch count 0–3 vs. speed comparison >= 9 impossible); token-adjusted command IDs validated with cost `k = |effective - rolled|`; awaiting clarification on Brakes semantics

**Cross-Agent Dependencies:**
- Awaiting Sully code review (PR #37, #38) for module design + command dispatcher + aggregate cohesion
- Awaiting user clarification on Brakes landing criterion (Tenerife spec vs. Aloha findings)
- Concentration token design complete; ready for Telegram adapter integration once Epic B (transport) baseline established

**Delivered Artifacts:**
- Draft PR #37 (Skiles): GameState refactor, 7 modules, landing validation, coffee-token multi-spend, command ID surface
- Tenerife spec (500+ lines): All modules, invariants, edge cases, verification checklist, clarifications
- Draft PR #38 (Aloha): Boundary tests, landing outcome matrix (1 win + 6 loss scenarios), token pool/die value boundaries
- Orchestration logs (3 agents): Skiles, Tenerife, Aloha
- Session log: Ralph round summary
- Decision inbox merged: Tenerife spec + Aloha findings + Copilot directive (placement undo rules)

**Pending Escalations:**
1. **Brakes Landing Criterion:** Spec states `BrakesValue == 3 AND BrakesValue > LastSpeed` but BrakesValue max is 3; if LastSpeed ≥ 9, condition is unsatisfiable. Current code checks `BrakesValue >= 6`. Recommend reconciliation before test finalization.
2. **Token-Adjusted Command IDs:** Design surface (e.g., `Axis.AssignBlue:1>3` for "place rolled 1 as 3 for 2 tokens") validated in tests. Awaiting Telegram button rendering spec (Sully + Tenerife).

---

### Session 6: Issue #28 Application round/turn state + secret hand (2026-02-21)

**Outcome:** Implemented application-layer round/turn state primitives to support strict alternation and secret dice hands (no Telegram SDK types).

**Key Learnings:**
- Keeping round/turn orchestration in application avoids leaking domain-internal dice/player types into Telegram UX.
- Modeling the round as a small state machine (`InProgress` → `ReadyToResolve` after 8 placements) keeps downstream use-cases simple.
- Undo gating is easiest to express as: "only the player who placed last can undo, and only before the other player plays".

**Delivered Artifacts:**
- `SkyTeam.Application.Round`: `PlayerSeat`, `DieValue`, `SecretDiceHand`, `RoundTurnState`.
- Design note: `.squad/decisions/inbox/skiles-issue28-design.md`.

---

### Session 7: Telegram /sky undo (2026-02-21)

**Outcome:** Implemented `/sky undo` in `SkyTeam.TelegramBot\Program.cs` (private chat only) to undo the last die placement and refresh the player's secret hand.

**Key Paths & Behavior:**
- Telegram surface: `SkyTeam.TelegramBot\Program.cs` → `HandleSkyUndoAsync()`.
- Application use-case: `SkyTeam.Application\GameSessions\InMemoryGroupGameSessionStore.UndoLastPlacement(userId)` returning `GameUndoResult` (`NoActiveSession`, `NotSeated`, `RoundNotRolled`, `UndoNotAllowed`, `DomainError`).
- On success: bot posts a public message to `PublicInfo.GroupChatId` and DMs the requesting user with the updated hand and available commands (same rendering style as `/sky place`).
- Group chat guard: `/sky undo` in a group replies "Use /sky undo in a private chat with me."

### Session 8: Telegram Button-First UX — Callback Architecture Breakdown (2026-02-21T23:05:13Z)

**Outcome:** Produced comprehensive technical breakdown of callback_query handling, single edited cockpit message pattern, DM menu design, 64-byte callback_data mitigation strategy, menu state store architecture, deep-link onboarding flow, and constraint-handling strategies.

**Key Decisions:**

**1. Callback Query Handler Pattern**
- Validate payload: `(userId, groupChatId, menuVersion, actionId)`
- Server-side lookup: `MenuState[(userId, groupChatId, menuVersion)][actionId]` → command/target
- Execute corresponding domain command
- `AnswerCallbackQuery` response (spinner stop, error toast)
- Idempotency: track `(userId, groupChatId, menuVersion, actionId)` to dedupe retries

**2. Single Edited Cockpit Message Lifecycle**
- On first interaction (`/sky new`): send cockpit, persist `message_id`
- On state change (after round resolution): `EditMessageText` only (no new messages)
- Cockpit deleted on game end or session timeout

**3. Menu State Store (In-Memory, Per-Group)**
- Storage: `Dictionary<(userId, groupChatId, menuVersion), MenuState>`
- MenuState: `{ actionId → commandId, targetId → moduleSlot, timestamp }`
- Thread-safe with `ReaderWriterLockSlim` per group
- GC: expire after 1 hour or session end
- Supports versioning (`v1:`, `v2:` for schema evolution)

**4. callback_data 64-Byte Constraint Solution**
- Problem: full command IDs + dynamic options exceed 64 bytes
- Solution: short versioned action tokens (`v1:place:d2`) + server-side state
- No long IDs in callback; all mapping is server-side
- Format: `v1:action:index`

**5. DM Menu Design**
- Display secret hand + available placements
- Persist DM `message_id` per user; edit on state change
- Button layout: die-selector (0–3) + target buttons (module slots) + undo/cancel

**6. Deep-Link Onboarding (/start?game=<groupId>)**
- Entry: group "Join Game" button or manual `/start?game=123`
- Handler: register user, join session, show DM hand menu
- Fallback: plain `/start` lists active games

**Constraints & Mitigations:**
- **Retry dedup:** idempotency key prevents duplicate actions

### Session 4: Slice #59 — WebApp Foundation Implementation (2026-02-22)

**Outcome:** Converted TelegramBot to ASP.NET Core Web SDK; implemented TelegramInitDataValidator, TelegramInitDataFilter, and GET /api/webapp/game-state endpoint.

**Deliverables:**
1. **Project SDK conversion:** `SkyTeam.TelegramBot.csproj` → `Microsoft.NET.Sdk.Web`
2. **Program.cs refactor:**
   - WebApplication.CreateBuilder() pattern (vs. Host.CreateDefaultBuilder)
   - InMemoryGroupLobbyStore, InMemoryGroupGameSessionStore registered as singletons
   - TelegramBotService (IHostedService) wraps existing polling loop
   - UseStaticFiles() for wwwroot/ serving
   - MapWebAppEndpoints() for /api/webapp/* routing
3. **TelegramInitDataValidator service:**
   - Pure, testable validation logic
   - HMAC-SHA256 per Telegram spec
   - FixedTimeEquals constant-time comparison
   - auth_date freshness check (default 5 min, configurable)
   - Returns InitDataValidationResult (success or specific failure reason)
4. **TelegramInitDataFilter (IEndpointFilter):**
   - Reads X-Telegram-Init-Data header
   - Delegates to validator
   - On success: injects TelegramWebAppUser into HttpContext.Items
   - On failure: returns 401 Unauthorized
5. **GET /api/webapp/game-state endpoint:**
   - Query param: gameId (from start_param)
   - Cross-check signed start_param against query gameId (reject 400 if mismatch)
   - Query in-memory stores; return public game state (200)
   - Return 404 if no lobby/session, 400 if invalid gameId, 401 if auth fails
6. **TelegramBotOptions configuration class:**
   - BotToken (from env TELEGRAM_BOT_TOKEN)
   - WebApp section (InitDataMaxAgeSeconds, etc.)
7. **appsettings.json:** Added WebApp:InitDataMaxAgeSeconds (default 300)

**Architecture Decisions:**
- **Single host process:** No IPC needed; in-memory stores are DI singletons
- **Hosted service for polling:** ASP.NET Core lifecycle management + graceful shutdown
- **Filter-based auth:** Clean separation; reusable across endpoints
- **Read-only in Slice #59:** Public state only; no secrets. Write endpoints deferred to Slice #64

**Testing:**
- Issue #59 validator tests: Valid initData, tampered hash, expired auth_date, missing hash, empty initData, constant-time comparison ✅
- Issue #59 endpoint integration tests: Valid (200), no game (404), missing header (401), invalid initData (401), mismatched gameId (400) ✅
- Issue #53 callback tests: Roll/Place(DM)/Refresh callbacks, privacy contract, fallback continuity ✅
- Result: 206 total, 193 passed, 13 skipped, 0 failed

**Integrations:**
- Backward compatible with existing Telegram callback routing (Issues #50–#53)
- Lock guards in stores ensure no race conditions under ASP.NET Core's multi-threaded requests
- Static files served alongside Telegram polling in single process

**Next Steps:**
- Slice #60: Launch surface ("Open app" button + start_param wiring in group cockpit)
- Slice #61: Mini app lobby UI (New/Join/Start buttons)
- Slice #62: Mini app in-game UI (cockpit + private hand, no DMs)

- **Restart recovery:** stale buttons → "menu expired" toast; retry from `/sky hand`
- **Concurrency:** per-group serialization + async/await on all Telegram API calls

**GitHub Artifacts Created:**
- Epic #49: https://github.com/glconti/sky-team-bot/issues/49 (Telegram button-first UX)
- Child issues #50–#57: callback handler, cockpit renderer, DM menu, callback_data design, state store, onboarding, button lifecycle, E2E tests

**Implementation Sequencing:**
- Issue #54 (menu state store) unblocks #50–#52 (callbacks, renderer, menu)
- Phases: #50–#52 (Phase 1), #53–#55 (Phase 2), #56–#57 (Phase 3 polish/tests)

**Cross-Team:**
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
