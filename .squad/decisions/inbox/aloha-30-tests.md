# Aloha: Issue #30 test boundary

Date: 2026-02-21

## Decision
Add coverage for public placements + strict alternation at the **application layer** (`SkyTeam.Application.Tests`) by exercising `SkyTeam.Application.Round.RoundTurnState`.

## Rationale
`RoundTurnState` is the pure, deterministic source of truth for alternation and the public placement log, and testing it avoids coupling the suite to Telegram transport/UI details.
