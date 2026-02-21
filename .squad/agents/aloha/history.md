# Aloha — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

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

### Session 2: Issue #31 Domain QA (2026-02-21)
**Outcome:** Added fast, deterministic `SkyTeam.Domain.Tests` coverage for Tenerife's locked rules spec: axis out-of-bounds immediate loss, landing win/loss criteria matrix, and coffee-token/die boundary cases.

**Notes / Learnings:**
- Axis resolution must fail fast when the resulting position is `< -2` or `> +2`.
- Landing result is an AND of criteria (approach cleared, axis in range, engines ≥ 9, brakes/flaps/gear fully deployed); a single failure yields LOSS.
- Brakes landing criterion appears inconsistent between spec and code; captured in `.squad/decisions/inbox/aloha-issue31-test-findings.md`.

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

---
