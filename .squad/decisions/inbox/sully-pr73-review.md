# Sully — PR #73 Review (Issue #65)

**PR:** https://github.com/glconti/sky-team-bot/pull/73
**Branch:** `skiles/issue-65-miniapp-hardening`
**Verdict:** ❌ **REJECT**
**Reviser:** Skiles (implementation), Aloha (missing test coverage)

---

## Acceptance Criteria Assessment

| # | Criterion | Status |
|---|-----------|--------|
| 1 | Enforce `auth_date` freshness window + clear error UX on expiry | ❌ Not addressed — skipped test placeholder only |
| 2 | Prevent replay/double-apply on placement endpoints (idempotency) | ✅ Implemented — bounded replay cache with `idempotencyKey` overload |
| 3 | Ensure every secret path is Mini-App-only (no DM hand/dice) | ❌ Not addressed — README updated but no code enforcing the constraint |
| 4 | E2E-ish integration tests: open→lobby→start→roll→place→undo | ⚠️ Partial — one store-level flow test added; no transport/endpoint E2E |
| 5 | Bot commands redirect to Mini App when secrets would be shown | ❌ Not addressed — README mentions it, no implementation in diff |

**Result:** 1 of 5 criteria fully met, 1 partial, 3 missing.

## Code Quality (for implemented scope)

The idempotency implementation is **well-designed**:
- Bounded LRU cache (`MaxRecentPlacementResults = 256`) with `Queue<string>` + `Dictionary` — correct eviction pattern.
- Cache cleared on round transition (`InitializeRoundFromRoll`) — prevents stale cross-round replays.
- Overload preserves backward compatibility (`PlaceDie` without key delegates to `null` key variant).
- Lock scope already covers the idempotency check (existing `lock (_sessions)`), so thread safety is maintained.

Tests for the idempotency path are thorough: replay-same-player, idempotent-key-reuse, roll-replay guard, spectator guard.

## Blockers (must fix before re-review)

1. **Deleted CockpitMessageId tests** — Two existing passing tests (`CockpitMessageId_ShouldBePersistedPerGroupSession`, `CockpitMessageId_ShouldKeepSingleLatestValue_WhenRecreated`) were removed without replacement. These are not present in `Issue51CockpitLifecycleTests.cs` or elsewhere. **Restore or relocate these tests.**

2. **Missing AC #1 — auth_date freshness:** The skipped test stub is not sufficient. Either implement enforcement in the WebApp middleware/filter (where `TelegramInitDataValidator` already lives) or split this criterion out to a separate issue and remove it from the "Closes #65" claim.

3. **Missing AC #3 — secret-path enforcement:** DM commands (`/sky hand`, `/sky place`) must reject or redirect when Mini App is configured. No code change addresses this.

4. **Missing AC #5 — command redirects:** Bot command handlers need to detect secret-context commands and respond with a Mini App deep-link instead. No implementation present.

5. **Missing AC #4 — E2E tests at transport layer:** The store-level flow test is good but does not satisfy "E2E-ish" — need at least one `WebApplicationFactory`-based test hitting the placement endpoint with idempotency key header/param.

## Recommendation

- **Skiles:** Restore deleted CockpitMessageId tests. Implement AC #3 + #5 (DM command redirect logic in `TelegramBotService`). Wire idempotency key through the WebApp placement endpoint.
- **Aloha:** Add transport-layer E2E test for the open→lobby→start→roll→place→undo flow. Add auth_date expiry enforcement test (AC #1).
- **Alternative:** If scope is intentionally narrowed, rename PR to reflect partial delivery, remove "Closes #65", and open follow-up issues for remaining criteria.

## Residual Observations

- xUnit1051 warnings (CancellationToken) pre-exist in other test files — not introduced by this PR, not a blocker.
- All 251 tests pass (145 domain + 106 application, 18 skipped pre-existing). No regressions beyond the deleted tests.
