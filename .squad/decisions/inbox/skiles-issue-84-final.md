# Issue #84 Final Residual Closure (Skiles)

**Timestamp:** 2026-03-02T01:42:53Z  
**Issue:** https://github.com/glconti/sky-team-bot/issues/84  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Requested by:** Gianluigi Conti

### Context
- #84 residual checklist required explicit per-user/per-IP enforcement evidence, clear 400/429 contracts with retry guidance, and completion of input-validation guardrails including payload/idempotency protections.
- PR #87 already contained slice-1 throttling and basic input validation; this final slice closes the remaining API hardening gaps.

### Decision
- Keep abuse protection at the transport boundary via endpoint filters so invalid/replayed requests are rejected before domain mutation.
- Require `X-Idempotency-Key` on WebApp game mutation endpoints (`roll/place/undo`) and reject replayed keys with a clear 400 path.
- Add max payload enforcement for WebApp POST requests and return action-oriented retry hints on both 400 and 429 responses.

### Delivered Artifacts
- `SkyTeam.TelegramBot/WebApp/WebAppAbuseProtector.cs`
  - added mutation idempotency replay window and key tracking (`TryAllowMutationIdempotency`).
- `SkyTeam.TelegramBot/WebApp/WebAppAbuseProtectionFilter.cs`
  - added payload-size guard (`> 2 KB`), idempotency key validation, replay rejection, and richer 429 response payload (`retryAfterSeconds`, `retryHint`).
- `SkyTeam.TelegramBot/WebApp/TelegramInitDataFilter.cs`
  - added retry hint on oversized initData 400 response.
- `SkyTeam.TelegramBot/WebApp/WebAppEndpoints.cs`
  - added retry hints to displayName/dieIndex/commandId validation 400 responses.
- `SkyTeam.Application.Tests/Telegram/Issue84AbuseProtectionResidualTests.cs`
  - added residual integration coverage for per-user/per-IP throttling, idempotency key validation/replay, and payload-size rejection.
- `SkyTeam.Application.Tests/Telegram/Issue64WebAppPlacementFlowTests.cs`
  - updated WebApp mutation test requests to include `X-Idempotency-Key`.
- `readme.md`
  - documented #84 residual completion behavior and safe logging/retry semantics.

### Done Scope
- Residual #84 API checklist items are complete on PR #87:
  - per-user/per-IP/lobby-create throttles enforced with `429 + Retry-After`,
  - clear retry hints in throttle/error payloads,
  - idempotency-key + payload-size validation returning deterministic 400 paths,
  - abuse/suspicious request logging with safe metadata.

### Remaining Scope
- No remaining items in the #84 residual API checklist.
- Future hardening (outside this residual closure): distributed limiter/telemetry for multi-instance deployments.

### Tests
- `dotnet test SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --filter "FullyQualifiedName~Issue84AbuseProtectionResidualTests|FullyQualifiedName~Issue64WebAppPlacementFlowTests" --nologo` ✅ (14 passed)
- `dotnet build skyteam-bot.slnx --nologo` ✅
- `dotnet test skyteam-bot.slnx --nologo` ✅ (302 total, 286 passed, 16 skipped, 0 failed)

### Learnings
- Idempotency replay protection fits naturally beside rate limiting in endpoint filters, because both require transport context and should fail before domain execution.
- Retry-hint fields make 400/429 contracts operationally useful for Mini App clients without logging sensitive payload contents.
