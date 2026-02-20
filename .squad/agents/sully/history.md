# Sully — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Learnings

### Session 1: Backlog Setup & GitHub Label Taxonomy (2026-02-20)
- Created 25 GitHub labels across 5 categories: Type, Priority, Status, Area, and Routing
- Established 14 vertical-slice issues for M1 foundation work
- Key design decision: All issues represent end-to-end playable increments, not infrastructure-only work
- Dependency graph: Rules clarification (#14) is the critical path blocker for module work
- Squad routing embedded in labels for easy filtering and handoff
- Status model: `ready` for foundation, `blocked` for work waiting on clarification, `review` for PR gates
- Milestones structured for incremental delivery: MVP → Bot → Polish → Advanced

### Cross-Team Context
- **Tenerife** completed M1 rules specification (all 7 modules, landing criteria, clarifications)
- **Skiles** identified Phase 1 blocker: GameState aggregate + ExecuteCommand dispatcher must be built first
- **Aloha** standing by with test harness for module implementations

### Session 2: Telegram Placement + Concentration Token Architecture (2026-02-21)
**Outcome:** Assessed secret placement and coffee token UX fit against DDD aggregate pattern; produced minimal interaction contract for Telegram bot.

**Key Decisions:**
- **Secret placement:** Architecturally Excellent — aligns with DDD game aggregate. No public infrastructure changes needed.
- **Token command model:** Option A (Recommended) — Token spend as parameter on PlaceDieCommand, not separate command. Prevents ordering ambiguity and state-machine complexity.
- **Telegram contract:** Ephemeral UI (private keyboards, color-coded options), readiness handling, reveal broadcasting. Domain stays UI-agnostic.
- **Module resolution order locked:** Land on Concentration → Gain token → Advance (prevents race conditions)

**Delivered Artifacts:**
- Telegram placement + token architecture assessment with risk mitigation table (`.squad/decisions.md`)
- Minimal interaction contract: Bot ↔ Domain interface, session layer model, UX recommendations
- One orchestration log entry: Sully (architecture assessment)

**Cross-Coordination:**
- **Tenerife** finalized official rules spec (99% faithful, 1 open question on multi-token)
- **Skiles** proposed immutable domain model that aligns with this architecture
- **Aloha** can incorporate secret storage testing per Skiles' proposal

---
