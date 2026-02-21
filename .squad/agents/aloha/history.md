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

---
