# 2026-03-02T08:20:00Z — Issues #78/#79 Mini App UI Completion (Skiles)

**Issues:**  
- https://github.com/glconti/sky-team-bot/issues/78  
- https://github.com/glconti/sky-team-bot/issues/79  
**PR:** https://github.com/glconti/sky-team-bot/pull/87  
**Requested by:** Gianluigi Conti

## Context
- The Mini App shipped backend WebApp endpoints for lobby and in-game flows but still used a raw JSON debug UI.
- Issue #78 required a lobby UI with seat placeholders, stateful join/start actions, and clear error handling.
- Issue #79 required an in-game UI with readable cockpit state, active player actions, and concurrency conflict handling.

## Decision
- Replace the debug-only HTML with a minimal but production-usable lobby/in-game UI that:
  - renders lobby seats, placeholders, and action buttons with state-aware enablement,
  - surfaces in-game round/turn/cockpit status in readable panels,
  - routes roll/place/undo actions with expectedVersion and clear error messaging,
  - keeps responsive layout for mobile + desktop.
- Add focused UI source tests to lock placeholder/action labels, display-name truncation, and concurrency/version handling.

## Delivered Artifacts
- `SkyTeam.TelegramBot/wwwroot/index.html`
  - lobby + in-game UI panels, responsive layout, versioned action calls, conflict/error messaging.
- `SkyTeam.Application.Tests/Telegram/Issue78WebAppLobbyUiTests.cs`
- `SkyTeam.Application.Tests/Telegram/Issue79WebAppInGameUiTests.cs`

## Learnings
- A thin, readable Mini App UI can deliver all lobby/in-game acceptance criteria without adding a framework.
- Concurrency conflict messaging is most reliable when the UI immediately refreshes state after a 409.
