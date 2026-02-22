# 2026-02-22T10:57:04Z: Sully — Slice #59 design review

**By:** Sully (Architect)  
**Epic:** #57 — Mini App Foundation  
**Slice:** #59 — WebApp Foundation

## Delivered

- **Design Decision Document:** `sully-slice59-contracts.md` (merged to inbox)
  - Hosting strategy: Convert `SkyTeam.TelegramBot` to ASP.NET Core Web SDK (no new project).
  - Static file strategy: Minimal `wwwroot/index.html` shell.
  - WebApp API contract: `GET /api/webapp/game-state` endpoint shape and query semantics.
  - Security design: `TelegramInitDataValidator` service, HMAC-SHA256 validation, constant-time comparison, 5-minute auth window.
  - Detailed action items for Skiles, Gimli, and Aloha.
  - Risk & edge case analysis (HTTPS, replay, start_param spoofing, bot token, concurrency, gameId parse, missing state, initData missing, clock skew).

## Design Rationale

1. **Single-host approach** — In-memory stores (InMemoryGroupGameSessionStore, InMemoryGroupLobbyStore) are singletons in Program.cs. No need for IPC; converting to Web SDK is a one-line change.
2. **Minimal shell** — Ship with vanilla HTML + Telegram.WebApp.js; no bundler or SPA framework.
3. **Security-first** — Full HMAC validation per Telegram spec, with auth_date freshness check.
4. **Clear action items** — Broken into backend (Skiles), shell (Gimli), and tests (Aloha).

## Status

✅ **Approved for implementation** — Design is locked; Skiles, Gimli, and Aloha proceed in parallel.
