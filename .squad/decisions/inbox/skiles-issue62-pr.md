# 2026-02-23: Issue #62 PR publication note (Skiles)

- Branch: squad/62-webapp-ingame-view
- PR: https://github.com/glconti/sky-team-bot/pull/70
- Status: OPEN (draft)
- Merge status: UNKNOWN
- Issue linkage: Closes #62

## Acceptance coverage captured in PR
- WebApp in-game view returns cockpit snapshot and viewer role.
- privateHand is returned only for seated viewers and withheld for spectators.
- In-memory hand lookup is scoped by (groupChatId, requestingUserId).
- Mini app in-game UI renders cockpit, role, private hand section, and diagnostics payload.
- Validation evidence included: dotnet test .\\skyteam-bot.slnx -v minimal => 234 total, 217 passed, 17 skipped, 0 failed.