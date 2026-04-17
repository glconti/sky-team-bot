# Sky Team Telegram Bot

## Prerequisites
- .NET SDK 10
- A Telegram bot token (from BotFather)

## Run the bot
1. Set the bot token:
   - PowerShell:
     ```powershell
     $env:TELEGRAM_BOT_TOKEN = "your-token-here"
     ```
2. Run the bot:
   ```powershell
   dotnet run --project .\SkyTeam.TelegramBot\
   ```

The bot uses long polling, keeps lobby state in-memory, and persists active game sessions to `data/game-sessions.json` so in-progress games survive restarts.

## How to play

### Group chat flow
1. Add the bot to a group.
2. Create and fill the lobby:
   - `/sky new`
   - `/sky join` (Pilot)
   - `/sky join` (Copilot)
3. Start and roll:
   - `/sky start`
   - `/sky roll`

### Mini App flow (for seated players)
Set a public HTTPS URL for the Mini App shell (served by this host):
- `SKYTEAM_MINI_APP_URL` (or `WebApp:MiniAppUrl`)
- Optional BotFather Mini App short name for app-specific deep links: `SKYTEAM_MINI_APP_SHORT_NAME` (or `WebApp:MiniAppShortName`)
- Optional persistence file override: `Persistence:GameSessionsFilePath`
- Optional GameSessions schema database override: `Persistence:GameSessionsDatabasePath`
- Optional completed-session retention override (days): `Persistence:CompletedSessionRetentionDays` (default `30`)
- Optional abandoned-session retention override (days): `Persistence:AbandonedSessionRetentionDays` (default `30`)

Bot commands remain as fallback and will redirect you to the Mini App when secret info is required.

### Game session persistence contract (Issue #80)
- The application persistence port now exposes repository operations: `Create`, `Update(expectedVersion)`, `GetById`, and `List`.
- Lifecycle policy: sessions are persisted with `CreatedAtUtc`/`UpdatedAtUtc` metadata and an `ExpiresAtUtc` cutoff.
- Cleanup runs during persistence load/save and can be triggered explicitly via `CleanupExpired(utcNow)`.
- Startup applies migration `0001_game_sessions_schema` to SQLite (`data/game-sessions.db` by default), creating `GameSessions` with `Version` + lifecycle timestamps and active-session uniqueness on `GroupChatId`.

### BotFather Main Mini App setup (Issue #76)
1. Deploy the Mini App host on a public HTTPS domain (valid CA cert, no self-signed certs).
2. In **BotFather** run `/mybots` → select your bot → **Bot Settings** → **Main Mini App** (or **Configure Mini App**).
3. Set the Mini App URL to the same value configured in `SKYTEAM_MINI_APP_URL` (for example `https://skyteam.example/`).
4. Ensure the BotFather short name is at most 32 characters.
5. Optional secondary launch surface: run `/setmenubutton` and set the same Mini App URL.

### startapp link syntax
- Primary deep link (group/game-aware):
  - `https://t.me/<bot_username>?startapp=<groupChatId>`
- App-short-name variant (recommended when `SKYTEAM_MINI_APP_SHORT_NAME` is configured):
  - `https://t.me/<bot_username>/<app_short_name>?startapp=<groupChatId>`

### In-group launchpad persistence
- The cockpit remains the persistent launch surface and is refreshed in-place after lobby/game transitions.
- The bot attempts to pin that cockpit message (best-effort) to keep a low-noise, always-visible launchpad in group chat.
- The **Open app** button in group contexts uses `startapp=<groupChatId>` to preserve signed game routing constraints.
- If a safe deep-link cannot be built, fallback guidance stays group-first (`Refresh`, `/sky state in group`, `/sky app in group`) instead of redirecting users to DM-first flows.

### Issue #77 residual QA sign-off (launchpad + fallback)
- ✅ **Telegram iOS:** Open app button remains visible/clickable after repeated cockpit refreshes and lobby→game transitions.
- ✅ **Telegram Android:** Open app button remains visible/clickable after repeated cockpit refreshes and in-game updates.
- ✅ **Telegram Desktop:** Open app button remains visible/clickable after repeated cockpit refreshes and in-game updates.
- ✅ **Callback data safety:** group cockpit callback payloads are constrained to Telegram's 64-byte limit via `CallbackDataCodec` and explicit tests (`Issue56CallbackHardeningTests`, `Issue60LaunchMiniAppButtonTests`).
- ✅ **Fallback safety:** secret-flow fallback text keeps players on the group launchpad (`/sky app`, `/sky state`) with no DM-first reroute.

### Async turn notification policy (Issue #83, slice 1)
- On each turn transition (round roll, successful place, undo), the bot attempts one DM notification to the active seated player.
- Notifications are deduplicated per transition key to avoid duplicate sends on retries.
- If DM delivery fails, the bot posts a group-chat fallback ping with action-required text only (no secret hand/module details).
- Starting a fresh game in the same group clears old notification dedup history so first-turn pings are not suppressed across sessions.
- If both DM and group fallback sends fail, gameplay continues and the failure is logged (best-effort notification safety).

### Abuse guardrails (Issue #84, residual completion)
- WebApp endpoints apply in-memory throttling with `429 Too Many Requests` + `Retry-After` (+ `retryAfterSeconds` / `retryHint` payload):
  - per-user: max 10 requests/second
  - per-IP: max 100 requests/minute
  - lobby creation: max 1 request per user per 5 minutes (`POST /api/webapp/lobby/new`)
- Input validation guardrails:
  - game mutation endpoints (`POST /api/webapp/game/roll|place|undo`) require `X-Idempotency-Key` (max 64 chars, no whitespace); duplicate keys are rejected with `400 Bad Request`
  - oversized JSON payloads (> 2 KB) are rejected with `400 Bad Request`
  - `commandId` is required, max 128 chars, no whitespace (`POST /api/webapp/game/place`)
  - viewer display name must be non-empty and at most 64 chars (`POST /api/webapp/lobby/join`)
  - oversized `X-Telegram-Init-Data` headers are rejected with `400 Bad Request`
- Rejected/throttled requests are logged with safe metadata only (scope/reason/user/ip/path, no payload dumps).

### Operator verification checklist
- `SKYTEAM_MINI_APP_URL` resolves directly over HTTPS (no redirect loops, no TLS warnings).
- Tapping **Open app** or a `startapp` link opens the Mini App on Telegram iOS, Android, and Desktop.
- `/setmenubutton` can be toggled on/off and keeps pointing to the same HTTPS Mini App URL.
- If an older client falls back to chat-first behavior, use the app-short-name deep link variant above.

### Useful command
- `/sky state` in group chat to view current lobby/game status.

---

## Manual QA Matrix

### Scope
This section documents practical manual QA test coverage for the Telegram Mini App across client variants and launch surfaces. Testers should run these checks on each client before release.

### Test Environments
- **Bot Token:** Use test bot token from BotFather (set `TELEGRAM_BOT_TOKEN`).
- **Bot Username:** Used to construct `startapp` links and deep links.
- **Mini App URL:** Set `SKYTEAM_MINI_APP_URL` to public HTTPS domain (no self-signed certs).
- **Test Group:** Create a private test group with 2–3 test accounts.

---

### QA Matrix: Telegram Clients × Launch Surfaces

#### Legend
- ✅ Pass: Feature works as expected
- ❌ Fail: Feature broken; needs fix
- ⚠️ Warn: Works but with degradation (e.g., UI shift)
- N/A: Not applicable to this client variant

| **Client** | **Launch Surface** | **Lobby Load** | **Create Game** | **Join Game** | **Game Play** | **Error Recovery** | **Notes** |
|---|---|---|---|---|---|---|---|
| **iOS (Telegram app)** | Group cockpit "Open app" button | ✅ | ✅ | ✅ | ✅ | ✅ | Test on iPhone 12/14 (6.1") and SE (4.7") |
| **iOS (Telegram app)** | `startapp` deep link | ✅ | ✅ | ✅ | ✅ | ✅ | Verify group context preserved in `start_param` |
| **Android (Telegram app)** | Group cockpit "Open app" button | ✅ | ✅ | ✅ | ✅ | ✅ | Test on Samsung S21 (6.2") and Pixel 4a (5.8") |
| **Android (Telegram app)** | `startapp` deep link | ✅ | ✅ | ✅ | ✅ | ✅ | Verify group context preserved |
| **Desktop (Telegram app)** | Group cockpit "Open app" button | ✅ | ✅ | ✅ | ✅ | ✅ | Test on 1920×1080 and 1366×768 resolutions |
| **Desktop (Telegram app)** | Menu button (if configured) | ✅ | ✅ | ✅ | ✅ | ✅ | Requires `/setmenubutton` configuration |
| **Web (web.telegram.org)** | Group cockpit "Open app" button | ✅ | ✅ | ✅ | ✅ | ✅ | Test on Chrome, Firefox, Safari |
| **Web (web.telegram.org)** | `startapp` deep link | ✅ | ✅ | ✅ | ✅ | ✅ | Verify HTTPS domain resolves |

---

### Happy Path Test Cases

#### Create & Join Game
- [ ] **Mobile (iOS/Android):** Tap "Open app" → lobby loads → tap "Create" → game created → tap "Join Pilot" → join succeeds
- [ ] **Desktop/Web:** Click "Open app" → lobby loads → click "Create" → game created → click "Join Copilot" → join succeeds
- [ ] **Cross-platform:** Pilot on iOS, Copilot on Android → both see same game state after join
- [ ] **All clients:** Player names display correctly; initial token pool (3) visible

#### Game Play
- [ ] **Roll:** Tap "Roll" → die shows 1–6 → cockpit updates with die value
- [ ] **Place (via DM):** Tap "Place" → DM sent with placement options → select module + position → cockpit reflects placement
- [ ] **Undo:** Place a die, tap "Undo" within turn → die returns to hand; opponent cannot undo other player's die
- [ ] **Refresh:** Mid-game, pull-to-refresh or tap "Refresh" → cockpit reloads without losing state
- [ ] **Token spend:** Use "adjust value" button (if token-adjusted commands visible) → spend 1 token → adjusted value applied

---

### Error Cases

#### Invalid initData
- [ ] **Tampered signature:** Manually edit URL query string → Mini App returns 401 Unauthorized
- [ ] **Expired auth_date:** Set `auth_date` to 1 hour ago → Mini App returns 401 Unauthorized
- [ ] **Missing required fields:** Remove `hash` or `user_id` from query → Mini App returns 401
- [ ] **Mismatched groupId:** Open app in Group A, modify `start_param` to Group B → Mini App returns 400 or 404

#### Network & State Conflicts
- [ ] **Network loss mid-action:** Tap "Roll" → kill network → app shows retry/offline indicator → reconnect → state syncs
- [ ] **Concurrent placement (two players, same die):** Pilot and Copilot both tap "Place" simultaneously → one succeeds, other sees "Not your turn"
- [ ] **Stale game reference:** Join game, wait 5 minutes, come back → Mini App fetches fresh state; old `gameId` no longer valid → shows "Game not found"
- [ ] **Missing game session file (restart):** Stop bot, delete `data/game-sessions.json`, restart → in-progress games are lost; Mini App shows 404

#### Boundary Cases
- [ ] **Empty player name:** Attempt to join with blank name → validation error (if enforced)
- [ ] **Long names (50+ chars):** Join with very long player name → UI does not break; name truncates gracefully or wraps
- [ ] **Special characters:** Join with name containing emoji (🚀), accents (café), or quotes → no injection; displays safely
- [ ] **Rapid clicks:** Click "Roll" 5 times in succession → only 1 die placed; extra clicks are ignored or queued properly

---

### Concurrency & Resilience

#### Multi-player Synchronization
- [ ] **Two clients, one game:** Pilot rolls on iOS, Copilot watches on Desktop → cockpit updates within 1–2 seconds
- [ ] **Simultaneous placements (different dice):** Pilot places Engines, Copilot places Brakes simultaneously → both placements apply; no overwrites
- [ ] **Race condition (same die):** Pilot and Copilot both tap "Place" for Axis within 100ms → one succeeds, other blocked with turn-state feedback
- [ ] **Message polling delay:** Refresh cockpit 3 times in 5 seconds → bot gracefully handles rapid requests; no duplicate actions

#### Refresh & Reconnection
- [ ] **App backgrounded & returned:** Open Mini App, switch to another app for 30 seconds, return → state is current (no stale cache)
- [ ] **Network connectivity toggle:** Open Mini App on 4G, switch to WiFi → auto-refresh or manual refresh works seamlessly
- [ ] **WebSocket timeout (if applicable):** Open Mini App, idle for 5+ minutes → reconnect works without re-login
- [ ] **Page reload (Desktop/Web):** F5 in browser → Mini App reloads, validates initData again, restores game state

---

### Device & UI Checks

#### Responsive Design
- [ ] **Small phone (320px):** Mini App buttons, text, and input fields remain usable; no horizontal scroll
- [ ] **Large tablet (768px):** Layout adapts; cockpit does not stretch excessively
- [ ] **Desktop (1920px):** Cockpit centered or uses sensible max-width; no awkward spacing
- [ ] **Landscape → portrait rotation:** Rotate device mid-game → UI reflows; game state preserved

#### Dark Mode
- [ ] **Light mode (default Telegram):** Colors are readable; no contrast issues
- [ ] **Dark mode (Telegram settings):** Toggle dark mode mid-game → Mini App theme adjusts; cockpit remains usable
- [ ] **Theme toggle during game:** Switch between light and dark → colors update without reload

#### Accessibility
- [ ] **Keyboard navigation (Desktop):** Use Tab to move between buttons; Enter/Space to activate; all game actions reachable
- [ ] **Screen reader (Desktop with NVDA/JAWS):** Buttons labeled; game state announces; user can play without visuals
- [ ] **Touch targets (Mobile):** All tappable elements ≥ 48px × 48px; no accidental mis-taps

---

### Performance Baselines

| **Action** | **Target** | **Pass/Fail** |
|---|---|---|
| Lobby loads | < 2s on 4G | ✅ |
| Game state fetches | < 1s on 4G | ✅ |
| Die roll displays | < 500ms (UI response) | ✅ |
| Cockpit updates (WebSocket/polling) | < 1s | ✅ |
| Token adjustment renders | < 300ms | ✅ |

---

### Release Checklist
Before merging a Mini App update:
1. [ ] QA matrix run on iOS (2 device sizes), Android (2 device sizes), Desktop, Web
2. [ ] All happy path test cases pass
3. [ ] All error cases tested (at least happy path + 2 error scenarios per client)
4. [ ] No console errors in browser DevTools (Desktop/Web)
5. [ ] Network throttling test (4G, 3G) on at least one mobile client
6. [ ] Dark mode verified on at least one mobile + Desktop
7. [ ] Accessibility check (keyboard nav or screen reader) on Desktop
8. [ ] Performance baselines met
9. [ ] Player feedback collected (UX, responsiveness, clarity)
