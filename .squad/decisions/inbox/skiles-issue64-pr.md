# 2026-02-23 — Issue #64 PR published (Skiles)

Requested by: Gianluigi Conti
Issue: #64
Branch: `squad/64-webapp-placement-undo`
PR: https://github.com/glconti/sky-team-bot/pull/72
Base: `master`
Head: `squad/64-webapp-placement-undo`
Draft: No
Mergeability: MERGEABLE (`mergeStateStatus: CLEAN`)

## Acceptance criteria summary
- Exposed Mini App placement endpoint: `POST /api/webapp/game/place?gameId=...`
- Exposed Mini App undo endpoint: `POST /api/webapp/game/undo?gameId=...`
- Successful place/undo refreshes group cockpit via `RefreshGroupCockpitFromWebAppAsync(...)`
- Placement supports token-adjusted command selection (`commandId` path)
- WebApp flow keeps secret options in private-hand state (no group-chat secret leakage)

## Linkage
- PR body includes: `Closes #64`