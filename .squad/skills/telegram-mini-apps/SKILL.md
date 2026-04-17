# Skill: Telegram Mini Apps (WebApps)

Sources:
- Mini Apps / WebApps: https://core.telegram.org/bots/webapps
- Bot platform overview: https://core.telegram.org/bots
- Bot API reference: https://core.telegram.org/bots/api

## What we build in SkyTeam
- **Primary UI**: Mini App (lobby + in-game cockpit + secret hand + placement UI).
- **Group chat**: low-noise “launchpad” message (single edited cockpit) + **Open app** button.
- **Hard constraint**: secrets never leave the Mini App (no DM hand/options).

## Launch surfaces (from Telegram docs)
Mini Apps can be launched:
- Main Mini App (profile “Launch app”)
- Inline button (`InlineKeyboardButton.web_app`)
- Menu button (configured via BotFather or `setChatMenuButton`)
- Keyboard button (`KeyboardButton.web_app`) — special: can call `Telegram.WebApp.sendData()`
- Direct links `https://t.me/<bot>?startapp=...`
- Attachment menu (special enablement; test env is easiest)

For SkyTeam we’ll use:
- **Inline button in the group cockpit** (Open app) and optionally menu button.
- Pass **group/game selector** via `startapp` / `tgWebAppStartParam`.

## WebApp JS API essentials
Include the Telegram script before anything else:
```html
<script src="https://telegram.org/js/telegram-web-app.js?59"></script>
```
Then use:
- `window.Telegram.WebApp.initData` (raw query string; **send to backend for verification**)
- `window.Telegram.WebApp.initDataUnsafe` (**never trust**; use only for quick UI hints)
- `window.Telegram.WebApp.ready()` (hide Telegram loading placeholder ASAP)

Notes:
- Prefer `viewportStableHeight` (not `viewportHeight`) for sticky bottom UI.
- Respect `safeAreaInset` / `contentSafeAreaInset` for notches / system bars.

## Server-side security: validating initData (MUST)
Per Telegram docs (“Validating data received via the Mini App”):
- Send `Telegram.WebApp.initData` to backend.
- Parse query string into key/value pairs.
- Build `data_check_string` by sorting fields alphabetically and joining `key=<value>` with `\n`, **excluding** `hash`.
- Compute:
  - `secret_key = HMAC_SHA256(<bot_token>, "WebAppData")`
  - `expected_hash = hex(HMAC_SHA256(data_check_string, secret_key))`
- Accept only if `expected_hash == hash`.
- Also check `auth_date` freshness to prevent replay.

Telegram doc snippet:
```
secret_key = HMAC_SHA256(<bot_token>, "WebAppData")
hex(HMAC_SHA256(data_check_string, secret_key)) == hash
```

### Third-party validation (optional)
Telegram also provides `signature` (Ed25519) for validation without bot token.
Production public key (hex) from docs:
- `e7bf03a2fa4602af4580703d88dda5bb59f32ed8b02a56c187fe7d34caed242d`

## Practical C# helper (sketch)
```csharp
static bool ValidateInitData(string initData, string botToken, TimeSpan maxAge, DateTimeOffset now)
{
    var kv = System.Web.HttpUtility.ParseQueryString(initData);

    var hash = kv["hash"] ?? "";
    var authDateStr = kv["auth_date"] ?? "0";
    _ = long.TryParse(authDateStr, out var authDate);

    var fields = kv.AllKeys!
        .Where(k => k is not null && k is not "hash")
        .Select(k => (Key: k!, Value: kv[k!]!))
        .OrderBy(p => p.Key, StringComparer.Ordinal)
        .Select(p => $"{p.Key}={p.Value}");

    var dataCheckString = string.Join("\n", fields);

    using var hmacKey = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("WebAppData"));
    var secretKey = hmacKey.ComputeHash(System.Text.Encoding.UTF8.GetBytes(botToken));

    using var hmac = new System.Security.Cryptography.HMACSHA256(secretKey);
    var expected = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataCheckString));
    var expectedHex = Convert.ToHexString(expected).ToLowerInvariant();

    if (!CryptographicEquals(expectedHex, hash)) return false;

    var issuedAt = DateTimeOffset.FromUnixTimeSeconds(authDate);
    if (now - issuedAt > maxAge) return false;

    return true;
}
```
(Implementation note: use constant-time comparison for hashes.)

## WebApp → bot data channel (optional)
`Telegram.WebApp.sendData(data)`:
- Sends a **service message** to the bot containing `data` (max 4096 bytes) and **closes** the Mini App.
- **Only available** when launched via a **KeyboardButton** Mini App.

For real-time gameplay, prefer calling your backend APIs directly (Mini App stays open).

## Config guardrail pattern (BotFather Main Mini App URL)
When Mini App launch depends on operator-managed BotFather settings, add local runtime guardrails:
- Validate `WebApp:MiniAppUrl` / env override with `IValidateOptions<T>`:
  - absolute URL required
  - `https` scheme required
  - no query/fragment (keep URL as stable app shell base)
- Register `ValidateOnStart()` so misconfiguration fails fast at startup.
- Cover with focused tests for valid HTTPS and invalid http/relative/query/fragment cases.

## Abuse guardrail slice pattern (WebApp transport)
For first-pass abuse protection without new infrastructure:
- Add a singleton in-memory sliding-window guard service.
- Enforce it in a dedicated `IEndpointFilter` after auth/initData validation.
- Start with pragmatic defaults:
  - per-user request rate
  - per-IP request rate
  - strict limit on create/start mutating endpoints
- On rejection:
  - return `429 Too Many Requests`
  - include `Retry-After` header
  - log scope/key/path for auditing
- Keep validation in transport boundary (`400`) and avoid domain mutations on reject paths.

## Async turn notification dedup hygiene
When using in-memory dedup keys for DM/group turn notifications:
- Scope dedup by transition key + recipient to prevent duplicate sends on retries.
- Reset stale dedup keys when a **new game starts in the same group**; otherwise first-turn notifications can be suppressed across sessions.
- Keep fallback sends best-effort (`try/catch` + warning log) so Telegram transport failures do not fail gameplay mutations.
