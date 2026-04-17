# Solo Mode Architecture — Sully Review

**Date:** 2026-03-02  
**Requested by:** Gianluigi Conti  
**Context:** Solo testing mode + Issues #78/#79 scope review

---

## Part 1: Solo Mode Architecture Design

### 1. Domain Model Changes

**Recommendation: Introduce `GameMode` enum at the Domain level**

```csharp
// SkyTeam.Domain/GameMode.cs
namespace SkyTeam.Domain;

public enum GameMode
{
    TwoPlayer,  // Standard cooperative play (Pilot + Copilot = 2 users)
    Solo        // Testing mode (1 user controls both seats)
}
```

**Game Aggregate Changes:**
- Add `GameMode Mode { get; }` property to `Game` class
- Pass `GameMode` to constructor: `public Game(Airport airport, Altitude altitude, GameModule[] modules, GameMode mode = GameMode.TwoPlayer)`
- **No turn-flow changes needed in domain** — the domain's `CurrentPlayer` is already seat-based (`Player.Pilot` / `Player.Copilot`), not user-based
- The domain doesn't care about user identity; it only tracks which seat's turn it is

**Rationale:**
- Keep domain pure: `Game` doesn't know about user IDs or authentication
- `GameMode` is a configuration concern that affects how the application layer interprets user permissions
- Backward compatibility: default to `TwoPlayer` for existing code

---

### 2. Lobby Changes

**Recommendation: Extend lobby to support solo-mode creation**

Two implementation paths:

#### **Option A: Solo-specific lobby flow (Recommended)**
- Add `LobbyCreateResult CreateSoloLobby(long groupChatId, LobbyPlayer player)` to `InMemoryGroupLobbyStore`
- Solo lobby auto-seats the single player in both Pilot and Copilot slots with a marker (e.g., display name suffix `" (Solo)"`)
- Solo lobby skips the "waiting for second player" state
- `LobbySnapshot.IsReady` returns `true` immediately after solo creation

```csharp
public LobbyCreateResult CreateSoloLobby(long groupChatId, LobbyPlayer player)
{
    lock (_sync)
    {
        if (_sessions.TryGetValue(groupChatId, out var existing))
            return new(LobbyCreateStatus.AlreadyExists, existing.ToSnapshot());

        var created = new LobbySession(groupChatId)
        {
            Pilot = player,
            Copilot = new LobbyPlayer(player.UserId, $"{player.DisplayName} (Solo)")
        };
        _sessions.Add(groupChatId, created);
        return new(LobbyCreateStatus.Created, created.ToSnapshot());
    }
}
```

**Pros:**
- Clear separation: solo games are distinct from normal lobby flow
- No risk of confusing "join" semantics (solo = instant ready)
- Easy to extend with solo-specific settings (e.g., difficulty, scenario selection)

**Cons:**
- Adds a new lobby API surface
- Requires UI changes to expose solo mode toggle

#### **Option B: Extend existing lobby with mode flag**
- Add `GameMode Mode { get; set; }` to `LobbySession`
- `CreateNew` accepts optional `GameMode` parameter
- When `Mode == Solo`, first player joining auto-fills both seats

**Pros:**
- Reuses existing lobby APIs
- Minimal code duplication

**Cons:**
- Mixes two distinct flows (normal vs. solo)
- "Join" semantics become ambiguous in solo mode

**Verdict:** Go with **Option A** for clarity and future extensibility.

---

### 3. WebApp API Changes

**Recommendation: Add new endpoint `POST /api/webapp/lobby/new-solo`**

```csharp
// WebAppEndpoints.cs
group.MapPost("/lobby/new-solo", CreateSoloLobby);

private static async Task<IResult> CreateSoloLobby(
    string? gameId,
    HttpContext httpContext,
    InMemoryGroupLobbyStore lobbyStore,
    InMemoryGroupGameSessionStore gameSessionStore,
    TelegramBotService telegramBotService,
    CancellationToken cancellationToken)
{
    var result = ResolveRequestContext(gameId, httpContext);
    if (result.Error is not null)
        return result.Error;

    var displayName = result.Context!.Viewer.DisplayName.Trim();
    if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > MaxDisplayNameLength)
        return Results.BadRequest(new { error = "Invalid display name." });

    var player = new LobbyPlayer(result.Context.Viewer.UserId, displayName);
    var createResult = lobbyStore.CreateSoloLobby(result.GroupChatId!.Value, player);

    if (createResult.Status == LobbyCreateStatus.Created)
        await telegramBotService.RefreshGroupCockpitFromWebAppAsync(result.GroupChatId.Value, cancellationToken);

    return Results.Ok(MapStateResponse(createResult.Snapshot, result.GroupChatId.Value, result.Context.Viewer.UserId));
}
```

**State Response Changes:**
- Add `bool IsSoloMode` to `WebAppLobbyState` to signal the UI
- Derive from `Pilot.UserId == Copilot.UserId` in `MapStateResponse`

```csharp
public sealed record WebAppLobbyState(
    WebAppLobbySeat? Pilot,
    WebAppLobbySeat? Copilot,
    bool IsReady,
    bool IsSoloMode); // NEW

// In MapStateResponse:
IsSoloMode = lobby.Pilot?.UserId == lobby.Copilot?.UserId
```

**UI Detection:**
- Frontend checks `state.lobby.isSoloMode` to show/hide solo-specific UI elements
- In solo mode: hide "Join Lobby" button, show "Solo Mode" badge, adjust seat card labels

---

### 4. Turn Flow in Solo Mode

**Critical Insight: No domain changes needed.**

The domain's `CurrentPlayer` is **seat-based**, not user-based. The `Game` aggregate doesn't know about user IDs; it only tracks `Player.Pilot` / `Player.Copilot`.

**Application Layer Enforcement:**

The `InMemoryGroupGameSessionStore` already has the right structure:
- `TryGetSessionByUserId(long userId)` resolves user → session
- `GetHandForSession(session, userId)` checks if user is seated
- In solo mode, the same user ID is seated in both slots

**Key Validation Change in `PlaceDie` / `UndoLastPlacement`:**

Current logic:
```csharp
var viewerSeat = GetSeatForUser(session, requestingUserId);
if (viewerSeat != session.TurnState?.CurrentPlayer)
    return NotPlayersTurn error;
```

Solo-aware logic (no change needed!):
```csharp
// Already works: Solo user's ID is mapped to both seats.
// When CurrentPlayer == Pilot, GetSeatForUser returns Pilot (valid).
// When CurrentPlayer == Copilot, GetSeatForUser returns Copilot (valid).
// The domain advances CurrentPlayer normally; app layer sees it as the same user.
```

**Conclusion:** The existing application layer already supports solo mode turn flow without modification, because `GetSeatForUser` dynamically resolves seat based on `CurrentPlayer`, and solo mode seats the same user in both roles.

**Optional Enhancement (Future):**
If you want to show "You control both seats" messaging, add logic in `GetHandForSession` to expose both hands when `IsSoloMode == true`. But for MVP, the existing flow works fine.

---

### 5. GitHub Issue Draft: Solo Testing Mode

**Title:** Solo Testing Mode for One-Player Control

**Labels:** `feature`, `ui`, `testing`

**Description:**
Add a solo testing mode that allows a single player to control both Pilot and Copilot seats for faster iteration during development and game rule validation.

**Acceptance Criteria:**
1. **Lobby Creation:**
   - New WebApp endpoint `POST /api/webapp/lobby/new-solo` creates a solo lobby
   - Solo lobby auto-seats the creating user in both Pilot and Copilot slots
   - Solo lobby state includes `IsSoloMode: true` flag in `WebAppLobbyState`

2. **Domain Support:**
   - `Game` aggregate accepts `GameMode.Solo` in constructor
   - No turn-flow changes needed (domain is seat-based, not user-based)

3. **Application Layer:**
   - `InMemoryGroupLobbyStore.CreateSoloLobby(groupChatId, player)` method
   - `InMemoryGroupGameSessionStore.Start` stores solo mode flag on session
   - Existing turn validation works unchanged (user is seated in both roles)

4. **WebApp UI:**
   - New "Solo Mode" button in lobby screen
   - Solo mode badge replaces "Waiting for Copilot" placeholder
   - Hide "Join Lobby" button when `isSoloMode == true`
   - Both seat cards show same player name with "(Solo)" suffix

5. **Round Flow:**
   - Solo player places all 4 Pilot dice, then all 4 Copilot dice
   - Private hand API returns correct seat's dice based on `CurrentPlayer`
   - Undo works normally (undoes last placement regardless of seat)

**Out of Scope (Future):**
- Persistent solo mode preference (always use solo for a specific user/group)
- Solo-specific difficulty settings (e.g., easier landing thresholds)
- Simultaneous dual-hand view (show both Pilot and Copilot dice at once)

**Suggested Test Names:**
- `SoloModeLobbyCreation_ShouldAutoSeatSinglePlayerInBothRoles`
- `SoloModeGameStart_ShouldRecordGameModeOnSession`
- `SoloModeTurnFlow_ShouldAllowSameUserToPlacePilotAndCopilotDice`
- `SoloModeUndo_ShouldRevertLastPlacementRegardlessOfSeat`
- `SoloModeWebAppState_ShouldExposeIsSoloModeFlag`
- `SoloModeHandApi_ShouldReturnCorrectSeatDiceBasedOnCurrentPlayer`
- `SoloModeRoundResolution_ShouldAdvanceToNextRoundAfterAllDicePlaced`

**Dependencies:**
- None (orthogonal to existing multiplayer flow)

**Estimate:**
- Domain: 30 min (enum + constructor parameter)
- Application: 2 hours (lobby + session logic + tests)
- WebApp: 1 hour (endpoint + state mapping)
- UI: 2 hours (solo button + badge + conditional rendering)
- Tests: 2 hours (7 test cases + integration coverage)
- **Total:** ~7.5 hours

---

## Part 2: Issues #78 and #79 Scope Review

### Issue #78: WebApp Lobby UI — Gap Analysis

**What the Tests Expect:**
1. ✅ Pilot placeholder: `"Waiting for Pilot"` — **FOUND** (line 497)
2. ✅ Copilot placeholder: `"Waiting for Copilot…"` — **FOUND** (line 498)
3. ✅ "New Lobby" button — **FOUND** (line 501)
4. ✅ "Join Lobby" button — **FOUND** (line 563)
5. ✅ "Start Game" button — **FOUND** (line 605)
6. ✅ "Game name" input — **FOUND** (line 520, label "Game name")
7. ✅ "Player count" input — **FOUND** (line 528, label "Player count")
8. ✅ "Lobby settings" input — **FOUND** (line 534, label "Lobby settings")
9. ✅ "Game code" input — **FOUND** (line 582, label "Game code")
10. ✅ Display name truncation: `maxDisplayNameLength = 32` — **FOUND** (line 291)
11. ✅ Truncation function: `truncateDisplayName` — **FOUND** (line 327)
12. ✅ Validation message: `"Game name is required."` — **FOUND** (line 335)
13. ✅ Validation message: `"Player count must be ${requiredLobbyPlayerCount} (Pilot + Copilot)."` — **FOUND** (line 340)
14. ✅ Validation message: `"Enter a numeric game code."` — **FOUND** (line 354)

**Verdict:** **Issue #78 is COMPLETE.** All acceptance criteria are implemented and tested.

**Recommendation:** Close #78 after verifying manual QA on Telegram clients (iOS/Android/Desktop).

---

### Issue #79: WebApp In-Game UI — Gap Analysis

**What the Tests Expect:**
1. ✅ "In Game" header — **FOUND** (line 247, `<div class="panel-title">In Game</div>`)
2. ✅ "Round & Turn" section — **FOUND** (line 637, `createCard('Round & Turn', ...)`)
3. ✅ "Cockpit" section — **FOUND** (line 642, `createCard('Cockpit', ...)`)
4. ✅ Concurrency conflict handling: `"ConcurrencyConflict"` — **FOUND** (line 464)
5. ✅ Expected version in actions: `expectedVersion` — **FOUND** (line 371, 668, 676, 795)

**Verdict:** **Issue #79 is COMPLETE.** All acceptance criteria are implemented and tested.

**Recommendation:** Close #79 after verifying manual QA on Telegram clients (iOS/Android/Desktop).

---

### Implementation Order Recommendation

#### **Immediate Actions:**
1. **Close #78 and #79** — Both issues meet acceptance criteria; only manual QA remains
2. **Manual QA Checklist** (add to `.squad/agents/aloha/qa-checklist.md`):
   - iOS: Verify lobby UI placeholders, create/join forms, validation messages
   - Android: Verify in-game UI sections (Round & Turn, Cockpit), concurrency conflict handling
   - Desktop: Verify full round flow (roll, place, undo) with version tracking
   - Web: Verify responsive layout on narrow viewports

#### **Next Priority (Post-#78/#79 Closure):**
3. **Implement Solo Mode** (new issue):
   - Tenerife: Spec solo mode rules (does landing validation change?)
   - Sully: Review domain + application architecture (this document)
   - Skiles: Implement `CreateSoloLobby` + WebApp endpoint + tests
   - Aloha: Validate solo turn flow + edge cases (undo, concurrency, version conflicts)

#### **Parallel Workstream (Optional):**
4. **UI Polish** (separate issue):
   - Add loading spinners during API calls
   - Improve error message clarity (e.g., "Lobby full" → "Lobby is full. You can spectate but not join.")
   - Add keyboard shortcuts (e.g., "R" for Roll, "U" for Undo)
   - Add accessibility labels (ARIA attributes for screen readers)

---

## Key Architectural Decisions

### Decision 1: GameMode as Domain Enum
**Rationale:** Game mode is a configuration concern that affects application-layer permission checks, but the domain doesn't need to enforce user-based turn logic. Seat-based turn tracking is sufficient.

**Trade-off:** Slightly more complex lobby creation logic, but cleaner separation of concerns.

### Decision 2: Solo Lobby as Separate API
**Rationale:** Clear distinction between normal and solo flows prevents ambiguous "join" semantics and keeps the API surface explicit.

**Trade-off:** One extra endpoint vs. conditional logic in existing endpoint.

### Decision 3: No Turn-Flow Changes in Domain
**Rationale:** The domain's `CurrentPlayer` is already seat-based. The application layer's `GetSeatForUser` dynamically resolves the user → seat mapping, which naturally supports solo mode (same user mapped to both seats).

**Trade-off:** None. This is a fortunate architectural win.

---

## Risks and Mitigations

### Risk 1: Solo Mode Confusion in Production
**Mitigation:** Add explicit UI warning: "Solo mode is for testing only. Use normal lobby for multiplayer games."

### Risk 2: Accidental Solo Mode Activation
**Mitigation:** Require confirmation dialog: "Start solo mode? You will control both Pilot and Copilot."

### Risk 3: Solo Mode State Leakage
**Mitigation:** Ensure `IsSoloMode` flag is persisted with session and restored on restart. Otherwise, a restart mid-game could break turn flow.

---

## Open Questions for Tenerife (Rules Validation)

1. **Reroll Behavior:** In solo mode, does the player get a reroll token for **each seat** (2 total per round) or **shared** (1 total)?
   - Recommendation: Shared (1 reroll per round) to avoid exploiting solo mode for practice.

2. **Landing Validation:** Should solo mode have easier landing thresholds (e.g., Axis ±3 instead of ±2)?
   - Recommendation: No. Keep rules identical to multiplayer for consistency.

3. **Coffee Tokens:** Should solo mode start with extra coffee tokens to compensate for single-player mental load?
   - Recommendation: No. Keep token economy identical to multiplayer.

---

## Next Steps

1. **Sully:** ✅ Deliver this architecture document
2. **Ralph:** Create GitHub issue for solo mode (copy draft from section 5)
3. **Tenerife:** Review open questions and confirm rules alignment
4. **Skiles:** Implement solo lobby logic + tests (after Tenerife sign-off)
5. **Aloha:** Manual QA for #78/#79, then solo mode edge cases

---

**End of Architecture Review**
