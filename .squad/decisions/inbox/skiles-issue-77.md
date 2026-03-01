# Skiles — Issue #77 decision inbox

## 2026-03-02: Harden in-group Open App launchpad (slice 1)

**Issue:** #77  
**PR stream:** #87

### Decision
1. Keep the in-group cockpit launch button on `startapp` deep-links (`https://t.me/<bot_username>?startapp=<groupChatId>`), including when `WebApp:MiniAppUrl` is configured.
2. Emit Open app links only when deeplink inputs are safe: non-zero `groupChatId` and syntactically valid bot username.
3. If a safe deeplink cannot be built, keep group-safe fallback behavior (`Refresh` + `/sky state`) and avoid DM-first guidance.

### Rationale
- `startapp` preserves the per-group selector in signed Mini App context and aligns with Main Mini App launch behavior.
- Safety gating prevents malformed or unsafe deeplink emission from invalid runtime values.
- Preserving cockpit fallback controls keeps the launchpad robust across refreshes and transient bot identity issues.

### Slice delivered
- `TelegramBotService` now renders Open app as `startapp` URL in group cockpit and `/sky app` redirects.
- Added deeplink safety helper for username/chat validation before rendering Open app URLs.
- Added tests for startapp rendering and invalid-username hiding behavior.
- Updated `readme.md` with launchpad persistence/safety notes.

### Remaining scope
- Manual client verification matrix (iOS, Android, Desktop/Web) remains to be executed against live Telegram clients.
- Optional app-short-name fallback (`https://t.me/<bot>/<app_short_name>?startapp=...`) remains deferred.
