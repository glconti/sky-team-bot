# Tenerife — PR #73 Final Review (Issue #65)

**Reviewer:** Tenerife (Rules Expert, independent)
**Requested by:** Gianluigi Conti
**Input:** PR #73 (`skiles/issue-65-miniapp-hardening`) + Sully/Aloha revision cycle

---

## Verdict: ✅ APPROVE

PR #73 (base + revision cycle) satisfies all five Issue #65 acceptance criteria. Merge-ready once revision changes are committed and pushed.

---

## AC-by-AC Checklist

| AC | Criterion | Status | Evidence |
|----|-----------|--------|----------|
| 1 | Enforce `auth_date` freshness window + clear error UX on expiry | ✅ | `TelegramInitDataValidator` (slice #59) already enforces 5-min window returning `Expired` + `AuthDate`. Aloha revision adds `TelegramInitDataValidator_ShouldExposeExpiredStatus_ForAuthDateFreshnessUx` asserting explicit `Status = Expired` with `AuthDate` preserved for UX mapping. |
| 2 | Prevent replay/double-apply on placement endpoints | ✅ | `PlaceDie` idempotency-key overload + bounded 256-entry `RememberPlacementResult` cache, round-scoped clearing. Tests: `PlaceDie_ShouldBeIdempotent_WhenIdempotencyKeyIsReused`, `PlaceDie_ShouldReturnNotPlayersTurn_WhenPlacementIsReplayedBySamePlayer`, `RegisterRoll_ShouldReturnRoundNotAwaitingRoll_WhenRollIsReplayed`. |
| 3 | Every secret path is Mini-App-only (no DM hand/dice) | ✅ | Sully revision: `/sky hand`, `/sky place`, `/sky undo` in group and private chat redirect to Mini App. Place(DM) callback returns Mini App-only guidance. No secret payload exposed in any DM path. |
| 4 | E2E-ish integration tests (open → lobby → start → roll → place → undo) | ✅ | Aloha revision: `WebAppTransportFlow_ShouldCoverOpenLobbyStartRollPlaceUndo` in `Issue64WebAppPlacementFlowTests`. Also: `OpenLobbyStartRollPlaceUndo_ShouldKeepRoundInProgress_WhenUndoingFirstPlacement` in store tests. |
| 5 | Bot commands fallback → redirect to Mini App for secrets | ✅ | Sully revision: group `hand`/`place`/`undo` call `RedirectSecretPathToMiniAppAsync`. Private chat handlers redirect. `readme.md` updated to document Mini App flow. |

## Regression Check

- `dotnet test .\skyteam-bot.slnx -c Release` → **94 passed, 0 failed, 16 skipped**
- CockpitMessageId coverage restored: tests moved to `Issue51CockpitLifecycleTests` (stronger, per-group isolation) and re-added in `InMemoryGroupGameSessionStoreTests` (multi-group variant).
- xUnit1051 warnings are pre-existing / non-blocking.

## Observations (non-blocking)

1. **Skipped `AuthExpiryUx_ShouldPromptReopenFlow_WhenSessionTokenIsExpired`**: Correctly annotated — auth expiry UX lives in the transport layer, not the application store. AC #1 is covered by the validator test. No action needed.
2. **16 skipped tests** remain from earlier slices (scaffolded skip-stubs). These are tracked by their respective issues and do not affect this PR.

## Process Note

Revision changes from Sully (code) and Aloha (tests) are currently **unstaged** on the local branch. They must be committed and pushed before the GitHub PR reflects the full acceptance. Once committed, the PR is merge-ready.

---

**Merge readiness: CONFIRMED** — all ACs met, tests green, no rule violations detected.
