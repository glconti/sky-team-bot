# Aloha — Core Context (Summarized from History.md)

## Identity
**Role:** QA Engineer  
**Focus:** Mini App QA matrix, concurrency testing, deterministic test harness validation

## Current Position (2026-03-02T01:10:22Z)
- **Issue #86 Status:** COMPLETED — Manual QA matrix (8 client × surface, 30+ test cases) merged to readme.md
- **Issue #82 Integration:** Ready to activate expanded concurrency conflict tests on Skiles' #82 Slice 1 CAS implementation
- **Issue #81 Coordination:** Available for #81 full scope (security-context-binding) final QA sign-off
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
