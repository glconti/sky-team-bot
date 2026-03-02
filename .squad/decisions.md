# Decisions

> Append-only ledger of team decisions. Never retroactively edit entries.

## 2026-03-02T01:26:00Z — Issue #80 QA Validation Verdict (Aloha)

**Issue:** https://github.com/glconti/sky-team-bot/issues/80  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  

**Verdict:** Not close-ready.

### Test Coverage
- Focused #80 tests: 3/3 passed
- Full suite: 270/286 passed (16 skipped)

### Acceptance Criteria Status
✅ Game state survives restart/reload path (store rehydration tested)  
✅ Version field supports optimistic locking (expectedVersion + VersionConflict verified)  
❌ Game aggregate schema defined and migrated to database  
❌ GameRepository CRUD/List contract implemented as written  
❌ TTL/cleanup policy implemented/documented  
❌ Integration persistence test verifies file-backed restart path end-to-end  

### Blocking Gaps (for close-ready)
1. **Align implementation to issue contract:** Implement DB schema + migration + repository CRUD/List operations, or explicitly update issue scope to JSON snapshot persistence pattern.
2. **TTL cleanup policy:** Define and document in runtime-facing config/docs.
3. **File-backed restart test:** Add at least one integration test validating persistence across store rehydration in realistic host flow.

### Key Learning
Separate **behavior validation** (tests confirming rehydration + conflict mechanics work) from **contract validation** (issue-specified architecture artifacts). Passing tests alone insufficient for close-ready if acceptance criteria require missing deliverables.
