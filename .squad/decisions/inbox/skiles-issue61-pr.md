# Decision Note: Issue #61 PR publication

**Date:** 2026-02-22  
**By:** Skiles  
**Requested by:** Gianluigi Conti

## Outcome
- Committed issue #61 local work on branch `squad/61-webapp-lobby`.
- Opened PR **#69** against `master`: https://github.com/glconti/sky-team-bot/pull/69
- PR status at publish time:
  - State: `OPEN`
  - Draft: `false`
  - Mergeability: `CLEAN`

## Acceptance coverage in PR description
- `POST /api/webapp/lobby/new` creates lobby.
- `POST /api/webapp/lobby/join` seats viewer.
- `POST /api/webapp/lobby/start` starts ready session.
- Mini app lobby actions refresh/edit cockpit on success.
- `/sky new|join|start` fallback path remains available.

## Validation cited
- `dotnet test .\SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --filter "FullyQualifiedName~Issue61WebAppLobbyEndpointsTests|FullyQualifiedName~Issue61WebAppLobbyFlowTests|FullyQualifiedName~Issue59WebAppGameStateEndpointTests"`  
  Result: 11 passed, 4 skipped, 0 failed.
