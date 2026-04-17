# Solo Mode Architecture Decisions — Sully

**Date:** 2026-03-02  
**Session:** 31  
**Context:** Solo testing mode architecture + Issues #78/#79 scope review  
**Requested by:** Gianluigi Conti  
**Document:** `.squad/agents/sully/solo-mode-architecture.md`

---

## Decision 1: GameMode as Domain Enum

**Decision:** Introduce `GameMode` enum (`TwoPlayer`, `Solo`) at the Domain level as a constructor parameter for the `Game` aggregate.

**Rationale:**
- Game mode is a configuration concern that affects application-layer permission checks (who can place dice for which seat)
- The domain's turn tracking is **seat-based** (`Player.Pilot` / `Player.Copilot`), not user-based
- Domain doesn't need to know about user IDs or authentication; it only needs to track which seat's turn it is
- Adding `GameMode` to the constructor makes the game's operational mode explicit without polluting turn-flow logic

**Alternative Considered:** Keep `GameMode` purely in the application layer (e.g., flag on `GameSession`).
- **Rejected:** Domain should own configuration that affects its public API surface. If future rules differ by mode (e.g., solo-specific difficulty), the domain needs to know.

**Impact:**
- Domain: Add `GameMode.cs` enum, extend `Game` constructor with `GameMode mode = GameMode.TwoPlayer` parameter
- Backward compatibility: Default to `TwoPlayer` for existing code
- No changes to existing turn-flow logic; seat tracking remains unchanged

---

## Decision 2: Solo Lobby as Separate API (CreateSoloLobby)

**Decision:** Add new method `InMemoryGroupLobbyStore.CreateSoloLobby(long groupChatId, LobbyPlayer player)` and new endpoint `POST /api/webapp/lobby/new-solo`.

**Rationale:**
- Clear separation: solo games are distinct from normal lobby flow (no "waiting for second player" state)
- Prevents ambiguous "join" semantics (solo = instant ready, no join needed)
- Easy to extend with solo-specific settings in the future (e.g., difficulty toggles, scenario selection)
- Explicit API surface makes intent clear to API consumers

**Alternative Considered:** Extend existing `CreateNew` with optional `GameMode` parameter.
- **Rejected:** Mixes two distinct flows (normal vs. solo) and makes "join" semantics confusing. Solo mode should **never** allow a second player to join.

**Implementation:**
- Solo lobby auto-seats the creating user in both Pilot and Copilot slots
- Display name for Copilot slot: `"{player.DisplayName} (Solo)"` to visually signal solo mode
- `LobbySnapshot.IsReady` returns `true` immediately after solo creation
- WebApp state includes `IsSoloMode: bool` flag derived from `Pilot.UserId == Copilot.UserId`

---

## Decision 3: No Turn-Flow Changes in Domain or Application Layer

**Decision:** Do **not** modify turn validation logic in `InMemoryGroupGameSessionStore`. Existing code naturally supports solo mode.

**Rationale:**
- The application layer's `GetSeatForUser(session, userId)` dynamically resolves user → seat mapping based on `CurrentPlayer`
- In solo mode, the same user ID is seated in both Pilot and Copilot slots
- When `CurrentPlayer == Pilot`, `GetSeatForUser` returns `Pilot` (valid turn for solo user)
- When `CurrentPlayer == Copilot`, `GetSeatForUser` returns `Copilot` (valid turn for solo user)
- Domain advances `CurrentPlayer` seat normally; application layer sees it as the same user controlling both seats

**Key Insight:** Seat-based turn tracking naturally supports solo mode because the domain doesn't care about user identity, only which seat's turn it is.

**Impact:**
- Zero changes to `PlaceDie` / `UndoLastPlacement` validation logic
- Zero changes to domain turn-flow methods (`SwitchPlayer`, `NextRound`)
- Application layer's dynamic seat resolution is the architectural win that makes solo mode trivial

---

## Decision 4: Issues #78 and #79 Are Complete

**Decision:** Recommend closing issues #78 (WebApp Lobby UI) and #79 (WebApp In-Game UI) after manual Telegram client QA.

**Rationale:**
- Comprehensive review of `index.html` (lines 1-800) confirmed all acceptance criteria are implemented:
  - #78: All lobby UI elements present (placeholders, buttons, forms, validation messages, display name truncation)
  - #79: All in-game UI elements present ("In Game" header, "Round & Turn" section, "Cockpit" section, concurrency conflict handling, `expectedVersion` in actions)
- Tests in `Issue78WebAppLobbyUiTests.cs` and `Issue79WebAppInGameUiTests.cs` all pass (string-literal assertions against `index.html`)
- Issues were marked `go:needs-research` prematurely; no implementation gaps exist

**Remaining Work:**
- Manual QA on iOS, Android, Desktop, and Web Telegram clients to verify:
  - Lobby UI renders correctly on all platforms
  - In-game UI sections are readable and interactive
  - Concurrency conflict UI flow (409 response → refresh) works as expected
  - Validation messages appear correctly on input errors

**Next Action:** Aloha to perform manual QA checklist, then Sully to approve issue closure.

---

## Decision 5: Implementation Order for Solo Mode

**Decision:** Solo mode implementation follows this order:
1. **Tenerife:** Confirm rules alignment (reroll policy, coffee tokens, landing thresholds)
2. **Sully:** Architecture review complete (this document)
3. **Skiles:** Implement domain enum, lobby logic, WebApp endpoint, tests
4. **Aloha:** QA solo turn flow, edge cases (undo, concurrency, version conflicts)

**Rationale:**
- Tenerife sign-off on rules prevents rework if solo mode requires different mechanics
- Domain changes are minimal (enum + constructor), so low risk
- Application layer requires no turn-flow changes, so implementation is straightforward
- UI changes are localized to lobby screen (new button, badge, conditional rendering)

**Timeline Estimate:** ~7.5 hours total across all team members.

---

## Open Questions for Tenerife

1. **Reroll Behavior:** In solo mode, does the player get a reroll token for **each seat** (2 total per round) or **shared** (1 total)?
   - **Sully Recommendation:** Shared (1 reroll per round) to avoid exploiting solo mode for practice.

2. **Landing Validation:** Should solo mode have easier landing thresholds (e.g., Axis ±3 instead of ±2)?
   - **Sully Recommendation:** No. Keep rules identical to multiplayer for consistency.

3. **Coffee Tokens:** Should solo mode start with extra coffee tokens to compensate for single-player mental load?
   - **Sully Recommendation:** No. Keep token economy identical to multiplayer.

**Action:** Ralph to collect Tenerife's answers before Skiles begins implementation.

---

## Risks and Mitigations

### Risk 1: Solo Mode Confusion in Production
**Mitigation:** Add explicit UI warning: "Solo mode is for testing only. Use normal lobby for multiplayer games."

### Risk 2: Accidental Solo Mode Activation
**Mitigation:** Require confirmation dialog: "Start solo mode? You will control both Pilot and Copilot."

### Risk 3: Solo Mode State Leakage Across Restarts
**Mitigation:** Ensure `GameMode` is persisted with session and restored on restart. Add to `GameSessionPersistence` schema if not already present.

---

## Next Steps

1. ✅ **Sully:** Deliver architecture document (`.squad/agents/sully/solo-mode-architecture.md`)
2. ✅ **Sully:** Log decisions to `.squad/decisions/inbox/sully-solo-mode-arch.md`
3. ⏳ **Ralph:** Create GitHub issue for solo mode (copy draft from architecture doc section 5)
4. ⏳ **Tenerife:** Review open questions and confirm rules alignment
5. ⏳ **Aloha:** Manual QA for #78/#79 on Telegram clients
6. ⏳ **Skiles:** Implement solo lobby logic + tests (after Tenerife sign-off)

---

**End of Decision Record**
