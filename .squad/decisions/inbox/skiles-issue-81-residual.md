# Issue #81 Residual — InvalidGameContext completion (Skiles)

**Timestamp:** 2026-03-02T05:05:00Z  
**Issue:** https://github.com/glconti/sky-team-bot/issues/81  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Requested by:** Gianluigi Conti

## Context
- #81 residual checklist required an explicit `InvalidGameContext` outcome for cross-chat tampering rather than collapsing all unauthorized mutations into `NotSeated`.
- Existing context-bound mutations (`PlaceDie`, `UndoLastPlacement`) validated seat membership but did not distinguish between true non-participants and active users mutating the wrong chat.

## Decision
- Introduce explicit `InvalidGameContext` statuses in application mutation outcomes:
  - `GamePlacementStatus.InvalidGameContext`
  - `GameUndoStatus.InvalidGameContext`
- Keep compatibility-safe behavior:
  - Return `InvalidGameContext` only when the user is seated in a different active session.
  - Preserve `NotSeated` when the user is not seated in any active session.
- Surface the explicit contract through WebApp mutation error mapping with `error = "InvalidGameContext"`.
- Codify the invariant at aggregate boundary documentation (`GameSession` summary).

## Evidence
- Application store cross-chat checks now emit explicit invalid-context outcomes for place/undo.
- Integration coverage asserts WebApp conflict responses return `InvalidGameContext` on out-of-chat mutation attempts.
- Validation commands run:
  - `dotnet test SkyTeam.Application.Tests\SkyTeam.Application.Tests.csproj --filter "FullyQualifiedName~PlaceDie_ShouldReturnInvalidGameContext_WhenUserMutatesDifferentChatSession|FullyQualifiedName~UndoLastPlacement_ShouldReturnInvalidGameContext_WhenUserMutatesDifferentChatSession|FullyQualifiedName~PlaceEndpoint_ShouldReturnInvalidGameContext_WhenViewerMutatesDifferentChatSession|FullyQualifiedName~UndoEndpoint_ShouldReturnInvalidGameContext_WhenViewerMutatesDifferentChatSession" --nologo`
  - `dotnet build skyteam-bot.slnx --nologo`
  - `dotnet test skyteam-bot.slnx --nologo`

## Learnings
- Security outcome granularity matters: explicit invalid-context signaling avoids ambiguity in clients and ops.
- Distinguishing "wrong chat" from "not seated anywhere" can be done safely without changing domain entities, by inspecting active session seating at the application boundary.
