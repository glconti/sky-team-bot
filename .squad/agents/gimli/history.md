# Gimli — History

## Project Context
**User:** Gianluigi Conti
**Project:** Sky Team Bot — Telegram game adaptation
**Stack:** .NET 10 / C# 14, xUnit, FluentAssertions, Telegram Bot + WebApp

## Learnings

- Slice #59 Mini App shell calls `Telegram.WebApp.ready()` ASAP, then uses `Telegram.WebApp.initData` (raw signed querystring) for backend auth and `initDataUnsafe.start_param` only as a UI hint / convenience for building the request.
- Read-only contract (Slice #59): `GET /api/webapp/game-state?gameId=<start_param>` with header `X-Telegram-Init-Data: <initData>`; render returned JSON and surface non-200 bodies for debugging.
- Hosting note: `UseStaticFiles()` alone doesn’t serve `/` as `index.html`; either configure `UseDefaultFiles()`/`UseFileServer()` or point the WebApp URL explicitly to `/index.html`.

