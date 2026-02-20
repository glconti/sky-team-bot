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

---
