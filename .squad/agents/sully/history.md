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
**Outcome:** Assessed secret placement and coffee token UX fit against DDD aggregate pattern; produced extended interaction contract and command model for Skiles.

**Key Decisions:**
- **Secret placement:** Architecturally Excellent — aligns with DDD game aggregate. No public infrastructure changes needed.
- **Token command model:** Option A (Recommended) — Token spend as multi-token parameter on PlaceDieCommand (`SpendTokens: int?` or `AdjustedValue: int?` with derived cost). Prevents ordering ambiguity and state-machine complexity.
- **Extended command shape:** Single `PlaceDieCommand` with optional adjustment parameters: `UseTokenForAdjustment: bool`, `AdjustedValue: int?`, `TokensToSpend: int` (derived). Validation method checks pool availability and value validity.
- **Telegram contract:** Ephemeral UI (private keyboards, color-coded options), readiness handling, reveal broadcasting. Domain stays UI-agnostic.
- **Module resolution order locked:** Land on Concentration → Gain token → Advance (prevents race conditions)

**Delivered Artifacts:**
- Telegram placement + token architecture assessment (`.squad/decisions.md`)
- Extended interaction contract: Bot ↔ Domain interface with multi-token support
- Command shape guidance for Skiles: `PlaceDieOnConcentrationCommand` with spend validation
- One orchestration log entry: Sully (architecture assessment)

**Cross-Coordination:**
- **Tenerife** finalized official rules spec with multi-token spend locked (cost `k = |adjusted - rolled|`)
- **Skiles** extended domain model to support multi-token spend per this architecture guidance
- **Aloha** can incorporate secret storage + multi-token testing per Skiles' extended proposal

### Session 3: Decision Consolidation & Team Handoff (2026-02-21)
**Outcome:** Scribe merged decision inbox, updated agent histories, committed `.squad/` state; team unblocked for Phase 1 implementation with multi-token spec fully validated.

**Key Actions:**
- Merged multi-token spec into decisions.md (Tenerife canonical reference + Skiles extended command model)
- Deleted deduplicated inbox files
- Updated Sully history with multi-token command shape guidance
- Committed `.squad/` changes (orchestration logs, session log, updated histories)

**Team Readiness:**
- ✅ Multi-token spec complete: cost `k = |adjusted - rolled|`, no wraparound, full die value range supported
- ✅ Command shape extended: `AdjustedValue` optional parameter with derived spend cost
- ✅ Architecture validated: Single command, no separate token spend command, domain ↔ UI boundary clear
- ✅ Skiles can implement Phase 1 with confidence in token model and command shape
- ✅ Aloha can test multi-token flows (1-token, 2-token, 3-token, insufficient tokens edge cases)

---
