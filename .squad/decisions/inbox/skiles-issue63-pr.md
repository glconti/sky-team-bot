# Skiles Issue #63 PR Note

- Requested by: Gianluigi Conti
- Issue: #63
- PR: https://github.com/glconti/sky-team-bot/pull/71
- Title: feat: issue #63 mini app actions (roll + refresh bridge)
- Branch: squad/63-webapp-actions -> master
- Draft: False
- State: OPEN
- Mergeable: MERGEABLE
- Merge state status: CLEAN

## Acceptance criteria coverage
- Added authenticated POST /api/webapp/game/roll endpoint and wired it in MapWebAppEndpoints.
- Roll endpoint validates context/session readiness, registers roll, refreshes group cockpit via RefreshGroupCockpitFromWebAppAsync, and returns updated game-state response.
- Mini App in-game UI now renders Refresh and conditional Roll actions for seated users during AwaitingRoll.
- /sky roll fallback now redirects users to /sky app and avoids DM secret-dice delivery warnings in group chat.
- Tests updated/added to cover issue #63 contracts:
  - Issue63WebAppInGameActionsTests
  - Issue53InGameCockpitButtonFlowTests (updated fallback expectation)

## Validation
- Baseline (origin/master): dotnet test --nologo => 234 total, 217 passed, 17 skipped, 0 failed.
- Branch (squad/63-webapp-actions): dotnet test --nologo => 239 total, 222 passed, 17 skipped, 0 failed.