# Aloha — Core Context

**Agent:** Aloha (QA Lead)  
**Project:** Sky Team Bot — Telegram bot for the cooperative board game Sky Team  
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, DDD

## Mission
Validate implementation against domain acceptance criteria. Provide deterministic QA coverage for critical paths (concurrency, persistence, security, token mechanics). Post evidence-backed verdicts that unblock merge decisions.

## Current Status (2026-03-02T01:26:00Z — Round 12)
- **Issue #80 (Durable Persistence):** QA COMPLETED. Verdict: **Not close-ready**.
  - ✅ Rehydration round-trip + version conflict mechanics tested (3/3 focused tests pass).
  - ❌ Contract gaps: DB schema/migration, GameRepository CRUD/List, TTL policy, file-backed restart test.
  - **Action:** QA verdict posted to GitHub #80 with explicit 3-item blocker checklist.
  - **Blocks:** Epic #75 (#81 full scope, #82 expansion). PR #87 merge.

## Key QA Patterns (Established & Reusable)
- **Next Phase:** Run #82 conflict matrix, begin #77 UI launch validation

## Key Technical Learnings
1. **Deterministic Concurrency Validation:** Two same-die placements behind shared gate yields one `Placed` + one `NotPlayersTurn` (predictable test outcome)
2. **Persistence Round-Trip:** In-memory persistence double validates rehydration without external I/O; `IGameSessionPersistence` seam enables fast iteration
3. **Version Conflict Testing:** Must be unblocked by write APIs accepting `expectedVersion`; Skiles' #82 Slice 1 completes this blocker
4. **Manual QA at Scale:** Matrix format (client × surface × feature) is most effective when scoped, deterministic, integrated with release checklist, and developer-friendly

## Test Architecture
- **Framework:** xUnit + FluentAssertions, AAA pattern
- **Integration Pattern:** `WebApplicationFactory<Program>` + in-memory bot token, polling disabled
- **Concurrency Pattern:** Shared gate release for deterministic multi-threaded outcomes
- **Test Naming:** `[Feature]_[ShouldOutcome]_[WhenCondition]`

## Cross-Team Status (Brief)
- **Skiles:** #82 Slice 1 complete (CAS mutations); parallel tests pending
- **Sully:** #81 architecture review in progress
- **Tenerife:** Standby for #83
- **Critical Path:** #80 (done) → #81 (scope finalization) → #82 (conflict expansion) → #77/#83/#84

## High-Priority Handoff Items
1. Run expanded #82 concurrency matrix when Skiles posts conflict expansion scope
2. Begin #77 UI (group launchpad) QA coordination with Gimli
3. Archive old history.md entries (>30 days) to keep core-context fresh
