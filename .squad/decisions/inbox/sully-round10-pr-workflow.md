# Decision: Issue #76 + #85 PR Workflow & Architecture Approval

**Date:** 2026-03-02T00:15:00Z  
**Actor:** Sully (Lead / Architect)  
**Context:** Issues #76 (BotFather config) and #85 (WebApp lobby tests) completed by Skiles and Aloha; ready for merge to main.

## Decision

**Approve both issues for merge via draft PR #87.**

### Issue #76: BotFather Mini App Configuration Validation
**Architecture Review: APPROVED ✅**

**Rationale:**
- Configuration validation cleanly separated in DI layer (`WebAppOptionsValidator`).
- Enforces strict HTTPS + no query/fragment per Telegram BotFather spec.
- Fails fast on startup via `ValidateOnStart()` — prevents silent misconfigurations.
- No domain model changes; zero coupling to business logic.
- Public API unchanged; documentation clear for operators.

**Key Design Points:**
- Immutable validation rules (HTTPS scheme check, URI parsing, query/fragment rejection).
- `ValidateOptionsResult` pattern aligns with .NET DI best practices.
- Readme updated with BotFather setup procedure + operator checklist.

**Acceptance Criteria Met:**
- ✅ Validator logic isolated and testable
- ✅ DI integration validates at startup
- ✅ Documentation covers setup + operator procedures
- ✅ No new dependencies or external integrations required
- ✅ All 145 Domain tests passing; 114 Application tests passing

---

### Issue #85: WebApp Lobby Endpoints Integration Tests
**Architecture Review: APPROVED ✅**

**Rationale:**
- Two new integration tests cover happy path (3-step lobby flow) and failure path (single-player rejection).
- Test helper method `CreateAuthenticatedRequest()` reduces boilerplate; reusable for future WebApp API tests.
- AAA pattern + FluentAssertions; clean, readable, maintainable.
- Tests validate HTTP status codes + response content; no fragile mocking.
- Integration layer comprehensively tested before persistence layer (#80).

**Key Design Points:**
- Lobby flow test: Create → Join (pilot) → Join (copilot) → Start → Verify phase=InGame.
- Validation test: Create → Join (1 player) → Start → Verify 409 Conflict + error message.
- Test helper centralizes Telegram init data construction; DRY principle applied.

**Acceptance Criteria Met:**
- ✅ Lobby endpoints (new, join, start) fully tested with integration factory
- ✅ Happy path (full 3-player flow) validates state transitions
- ✅ Error path (insufficient players) validates 409 response + message
- ✅ Test helpers reduce maintenance surface area
- ✅ All 259 tests passing (145 Domain + 114 Application)

---

## Outcome

**Draft PR #87 created** with both issues merged cleanly.

**Branch:** `feat/issue-76-85-botfather-config-webapp-tests`  
**Commit Hash:** f10c834  
**Test Status:** 259 passing (145 Domain + 114 Application)  
**GitHub PR:** https://github.com/glconti/sky-team-bot/pull/87

**Issue Comments Posted:**
- Issue #76: Architecture approval + next-gate unblock (PR link)
- Issue #85: Architecture approval + integration readiness (PR link)

---

## Next Steps

1. **Gianluigi (User):** Review + merge draft PR #87 to main (final decision).
2. **Skiles:** Begin Issue #77 (Open App Launchpad) — cockpit button + startapp routing.
3. **Sully:** Stand by for Issue #80 (Game Persistence) — architecture review of aggregate shape + Version field.
4. **Aloha:** Prepare Issue #86 test harness for persistence layer integration.

---

## Team Coordination Notes

- **Cross-issue traceability:** Feature branch + commit message reference both #76 and #85; GitHub closes both on merge.
- **Async team workflow:** Draft PR enables async collaboration; architecture approval posted to issues before merge.
- **Critical path unblocked:** #76 complete → #77 unblocked → #80 ready for design → #78–#79 UI dependent on both.
- **No domain changes:** Both issues live in Telegram host/test layers; domain model untouched; ready for concurrent persistence work.
