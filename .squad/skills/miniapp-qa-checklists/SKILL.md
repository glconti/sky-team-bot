# Skill: Mini App QA Checklists & Test Patterns

**Owner:** Aloha (QA)  
**Tech:** xUnit + FluentAssertions, Integration Tests, E2E Client Testing

## Overview
Comprehensive QA & Definition of Done (DoD) checklists for Mini App features spanning:
- BotFather configuration & launch surfaces
- Group chat launchpad UX (cockpit message)
- WebApp UI (lobby, in-game, actions)
- Session persistence & concurrency
- Turn notifications & player communication
- Security (initData validation, user binding)
- Abuse prevention & rate limiting
- End-to-end API integration

---

## QA Checklist Categories

### 1. **Configuration & Launch (botfather-main-miniapp)**
**Objective:** Web App registered in BotFather and launch surfaces functional.

**DoD Checklist:**
- [ ] BotFather Web App field set to HTTPS Mini App URL (no self-signed certs)
- [ ] Mini App loads without SSL/TLS errors across iOS/Android/Desktop
- [ ] Open app button text & language match Telegram locale
- [ ] Short name ≤ 32 chars in BotFather config
- [ ] Default Web App URL resolves without redirects
- [ ] Menu button toggleable via `/setmenubutton` command

**Test Pattern:**
```csharp
[Fact]
public void WebAppConfiguration_ShouldBeValidAndHttps()
{
    // Arrange
    var options = new WebAppOptions { MiniAppUrl = "https://..." };
    
    // Act & Assert
    options.MiniAppUrl.Should().StartWith("https://");
    new Uri(options.MiniAppUrl).Scheme.Should().Be("https");
}
```

---

### 2. **Group Chat Launchpad UX (group-launchpad-ux)**
**Objective:** Cockpit message renders, persists, and remains interactive.

**DoD Checklist:**
- [ ] Cockpit message displays single-line game state (e.g., "🛩️ Pilot + 👨‍✈️ Copilot ready")
- [ ] Open app button always present and clickable
- [ ] Message edit preserves button state and valid callback data
- [ ] Callback data payload ≤ 64 bytes (Telegram hard limit)
- [ ] Max 1 edit per second (Telegram API constraint)
- [ ] Cockpit disappears or marks "Game Over" when lobby dissolves

**Test Pattern:**
```csharp
[Theory]
[InlineData(1)]
[InlineData(32)]
[InlineData(64)]
public void CallbackDataCodec_ShouldNotExceedTelegramLimit(int payloadSize)
{
    // Arrange
    var payload = new string('x', payloadSize);
    
    // Act
    var encoded = CallbackDataCodec.Encode(payload);
    
    // Assert
    encoded.Length.Should().BeLessThanOrEqualTo(64);
}
```

---

### 3. **WebApp Lobby UI (webapp-lobby-ui)**
**Objective:** Seat assignments, player names, and action buttons work correctly.

**DoD Checklist:**
- [ ] Displays pilot + copilot seats with avatars and display names
- [ ] Join button creates seat assignment with correct UserId
- [ ] New Lobby button resets UI state and sends `/sky new`
- [ ] Start button disabled until both seats filled
- [ ] Start button enabled once both seated (state machine validated)
- [ ] Player names truncate gracefully at 32 chars
- [ ] Spectators see read-only view without join button
- [ ] Rapid joins handled idempotently (only one seat filled)

**Test Pattern:**
```csharp
[Fact]
public void LobbyView_StartButton_ShouldTransitionFromDisabledToEnabledWhenBothSeated()
{
    // Arrange
    var lobby = CreateLobby();
    
    // Act - seat pilot
    var pilotJoined = JoinAs(userId: 1);
    
    // Assert
    pilotJoined.StartButtonEnabled.Should().BeFalse();
    
    // Act - seat copilot
    var copilotJoined = JoinAs(userId: 2);
    
    // Assert
    copilotJoined.StartButtonEnabled.Should().BeTrue();
}
```

---

### 4. **WebApp In-Game UI (webapp-game-ui)**
**Objective:** Secret hand visibility, modules, actions, and live updates.

**DoD Checklist:**
- [ ] Hand cards visible in-game only (never leaked to Telegram history/DMs)
- [ ] Module status indicators (brake, flaps, engines, etc.) update without full page reload
- [ ] Roll button submits placement and disables until turn resolves
- [ ] Placement UI allows drag-and-drop or tap selection
- [ ] Undo button available only before roll submission
- [ ] Turn order indicator highlights current player clearly
- [ ] Score bars and module health update reactively
- [ ] Actions respond within 500ms (perceived performance)

**Test Pattern:**
```csharp
[Fact]
public void GameView_ShouldNotLeakSecretHandToTelegram_WhenPlayerRolls()
{
    // Arrange
    var player = CreatePlayer(userId: 1);
    var game = CreateGame();
    
    // Act
    var hand = game.GetSecretHand(player);
    var publicState = game.GetPublicState();
    
    // Assert
    publicState.SecretHand.Should().BeNull();
    hand.Cards.Should().NotBeEmpty();
}
```

---

### 5. **Game Session Persistence (game-session-persistence)**
**Objective:** Lobbies and games persist across bot restarts.

**DoD Checklist:**
- [ ] Game state serializes to persistent store after each turn
- [ ] Lobby record includes ChatId, Pilot UserId, Copilot UserId, created timestamp
- [ ] Game sessions keyed by (ChatId, GameId) support concurrent games
- [ ] Session recovery loads within 100ms on bot restart
- [ ] Stale lobbies cleaned up after 24h TTL
- [ ] Concurrent updates use optimistic locking (version/ETag checks)
- [ ] Sensitive data encrypted or hashed at rest
- [ ] No data corruption under concurrent writes

**Test Pattern:**
```csharp
[Theory]
[InlineData(1)]
[InlineData(10)]
[InlineData(100)]
public async Task SessionStore_ShouldRecoverWithin100Ms_ForConcurrentGames(int gameCount)
{
    // Arrange
    var store = new GameSessionStore();
    var games = Enumerable.Range(0, gameCount)
        .Select(i => CreateAndPersistGame(i))
        .ToList();
    
    // Act
    var sw = Stopwatch.StartNew();
    var recovered = await store.LoadAllAsync();
    sw.Stop();
    
    // Assert
    recovered.Should().HaveCount(gameCount);
    sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
}
```

---

### 6. **Turn Notifications (turn-notifications)**
**Objective:** Players notified of their turn without leaking secrets.

**DoD Checklist:**
- [ ] Notification sent only to active player (not spectators)
- [ ] Notification includes game summary + action prompt
- [ ] Timeout reminder sent after X minutes without action
- [ ] Read receipts tracked (optional analytics)
- [ ] Respects Telegram mute settings (do_not_disturb flag)
- [ ] Cockpit updated immediately after action received
- [ ] Secret hand/placements never in notification content
- [ ] Notification formatted for all Telegram clients (text-only safe)

**Test Pattern:**
```csharp
[Fact]
public void TurnNotification_ShouldBeSentOnlyToActivePlayer()
{
    // Arrange
    var pilot = CreatePlayer(userId: 1);
    var copilot = CreatePlayer(userId: 2);
    var game = CreateGame(pilot, copilot);
    
    // Act
    var notifications = game.GetTurnNotifications();
    
    // Assert
    notifications.Should().ContainSingle()
        .Which.TargetUserId.Should().Be(pilot.UserId);
    notifications.Single().Content.Should().NotContain("secret");
}
```

---

### 7. **Concurrency Conflict Resolution (concurrency-conflicts)**
**Objective:** Simultaneous actions handled safely (no race conditions).

**DoD Checklist:**
- [ ] Two rolls within 10ms: second rejected with conflict error
- [ ] Placement conflict (same module claimed): second rejected
- [ ] Optimistic lock version check enforced on game updates
- [ ] Stale state detected client-side, triggers refresh
- [ ] Join during game-start: join rejects with reason
- [ ] Concurrent lobby creates in same group: idempotent (exactly one)
- [ ] No deadlocks under concurrent load (4+ concurrent games)
- [ ] Conflict error messages user-friendly (not stack traces)

**Test Pattern:**
```csharp
[Fact]
public void ConcurrentRoll_ShouldRejectSecondRoll_WithConflictError()
{
    // Arrange
    var game = CreateGame();
    var pilot = game.Pilot;
    var copilot = game.Copilot;
    
    // Act
    var roll1 = pilot.SubmitRoll(new[] { 1, 2 });
    var roll2 = copilot.SubmitRoll(new[] { 3, 4 });
    
    // Assert (assuming pilot submitted first based on timestamp)
    roll1.Should().BeSuccessful();
    roll2.Should().Fail()
        .And.Subject.Error.Should().Be("Game state conflict");
}
```

---

### 8. **Security Context Binding (security-context-binding)**
**Objective:** Player identity verified via Telegram and bound to actions.

**DoD Checklist:**
- [ ] initData validated via HMAC-SHA256 before any action
- [ ] initData hash verified using BotFather token (stored securely, not in code)
- [ ] User ID from initData matches WebApp context
- [ ] initData auth_date checked for freshness (reject if >24h old)
- [ ] All WebApp endpoints require valid initData; reject 401 if missing/invalid
- [ ] Player UserId binds to actions (cannot impersonate other player)
- [ ] CORS headers restrict to Telegram domains only
- [ ] Sensitive data never logged (bot token, private keys, hashes)

**Test Pattern:**
```csharp
[Theory]
[InlineData("invalid_hash")]
[InlineData("")]
[InlineData(null)]
public void TelegramInitDataValidator_ShouldReject_WhenHashInvalid(string hash)
{
    // Arrange
    var validator = new TelegramInitDataValidator(botToken: "secret123");
    var initData = $"user={{}}&hash={hash}";
    
    // Act
    var isValid = validator.Validate(initData, now: DateTimeOffset.UtcNow);
    
    // Assert
    isValid.Should().BeFalse();
}

[Fact]
public void TelegramInitDataValidator_ShouldReject_WhenAuthDateOlderThan24Hours()
{
    // Arrange
    var validator = new TelegramInitDataValidator(botToken: "secret123");
    var now = DateTimeOffset.UtcNow;
    var oldAuthDate = now.AddHours(-25).ToUnixTimeSeconds();
    var initData = $"auth_date={oldAuthDate}&hash=valid";
    
    // Act
    var isValid = validator.Validate(initData, now);
    
    // Assert
    isValid.Should().BeFalse();
}
```

---

### 9. **Abuse Prevention & Rate Limiting (abuse-limits)**
**Objective:** Prevent spam, DDoS, and game creation/action floods.

**DoD Checklist:**
- [ ] Max 1 game creation per user per 5 minutes
- [ ] Max 10 API requests per user per second
- [ ] Consecutive `/sky` commands rate-limited after 10
- [ ] Max 1 concurrent active game per user
- [ ] Message floods throttled (>50 edits/min suppressed)
- [ ] IP-based rate limit on WebApp (e.g., 100 req/min per IP)
- [ ] Idempotency keys on roll/placement (replay prevention)
- [ ] Suspicious activity logged (invalid initData, repeated 401s, command spam)

**Test Pattern:**
```csharp
[Fact]
public void GameCreationRateLimit_ShouldReject_IfUserCreatesMultipleGamesWithin5Minutes()
{
    // Arrange
    var userId = 123;
    var limiter = new RateLimiter(maxGamesPerUserPer5Min: 1);
    
    // Act
    var game1 = limiter.CreateGame(userId);
    var game2 = limiter.CreateGame(userId);
    
    // Assert
    game1.Should().BeSuccessful();
    game2.Should().Fail()
        .And.Subject.Error.Should().Contain("rate limit");
}

[Fact]
public void ApiRequestRateLimit_ShouldReject_After10RequestsPerSecond()
{
    // Arrange
    var userId = 123;
    var limiter = new RateLimiter(maxApiRequestsPerSecond: 10);
    
    // Act
    var requests = Enumerable.Range(0, 15)
        .Select(_ => limiter.AllowRequest(userId))
        .ToList();
    
    // Assert
    requests.Take(10).Should().AllSatisfy(r => r.Should().BeTrue());
    requests.Skip(10).Should().AllSatisfy(r => r.Should().BeFalse());
}
```

---

### 10. **End-to-End API Integration (api-integration-tests)**
**Objective:** Full flows from bot commands through Mini App to cockpit updates.

**DoD Checklist:**
- [ ] `/sky new` creates lobby (calls CreateNewLobby endpoint)
- [ ] `/sky join` seats player correctly (calls JoinLobby with UserId)
- [ ] `/sky start` transitions lobby → game (calls StartGame)
- [ ] `/sky state` returns current JSON snapshot
- [ ] Mini App API: submit dice placement returns 200 + updated state
- [ ] Mini App API: undo placement returns 200 + reverted state
- [ ] Mini App API: refresh returns latest state without side effects
- [ ] Cockpit message edited within 100ms of game state change
- [ ] WebApp → Bot callback flow: action → process → cockpit update

**Test Pattern:**
```csharp
[Fact]
public async Task FullGameFlow_ShouldCreateLobbyJoinAndStartGameSuccessfully()
{
    // Arrange
    var bot = CreateBotClient();
    var group = CreateTestGroup();
    var pilot = CreateTestUser(userId: 1);
    var copilot = CreateTestUser(userId: 2);
    
    // Act
    var lobbyCreated = await bot.SendCommand(group.Id, pilot, "/sky new");
    var pilotJoined = await bot.SendCommand(group.Id, pilot, "/sky join");
    var copilotJoined = await bot.SendCommand(group.Id, copilot, "/sky join");
    var gameStarted = await bot.SendCommand(group.Id, pilot, "/sky start");
    
    // Assert
    lobbyCreated.Should().BeSuccessful();
    pilotJoined.Should().BeSuccessful();
    copilotJoined.Should().BeSuccessful();
    gameStarted.Should().BeSuccessful();
    
    var state = await bot.GetGameState(group.Id);
    state.Status.Should().Be(GameStatus.InProgress);
    state.Pilot.UserId.Should().Be(pilot.UserId);
    state.Copilot.UserId.Should().Be(copilot.UserId);
}

[Fact]
public async Task PlacementSubmission_ShouldUpdateGameAndCockpitWithin100Ms()
{
    // Arrange
    var game = await StartGameAndLoadMiniApp();
    var sw = Stopwatch.StartNew();
    
    // Act
    var placementResult = await game.SubmitPlacement(moduleId: "brake", value: 1);
    var cockpitState = await game.GetCockpitSnapshot();
    sw.Stop();
    
    // Assert
    placementResult.Should().BeSuccessful();
    cockpitState.Modules["brake"].Value.Should().Be(1);
    sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(100));
}
```

---

### 11. **QA Test Matrix (qa-matrix)**
**Objective:** Coverage across clients, devices, languages, and performance.

**Clients:**
- Telegram iOS (latest stable)
- Telegram Android (latest stable)
- Telegram Desktop (latest stable)
- Telegram Web (latest stable)

**Device Sizes:**
- Mobile: 320px (iPhone SE), 375px (iPhone 12), 480px (small Android)
- Tablet: 768px (iPad mini), 1024px (iPad Pro)
- Desktop: 1920px, 2560px (ultrawide)

**Locales:**
- English (en)
- Russian (ru)
- Chinese Simplified (zh-Hans)
- Spanish (es)
- Japanese (ja)

**Performance Targets:**
- Mini App load: < 2s on 4G
- API response: < 500ms (p95)
- Cockpit update: < 100ms (p95)
- Action response: < 200ms (p95)

**Accessibility:**
- Screen reader navigation (NVDA, JAWS, VoiceOver)
- Keyboard-only gameplay (no mouse required)
- Color contrast ≥ 4.5:1
- Touch targets ≥ 48px

**Themes:**
- Light mode (default Telegram)
- Dark mode (Telegram dark theme)
- Verify colors adjust without UI reload

**Test Pattern:**
```csharp
[Theory]
[InlineData("en")]
[InlineData("ru")]
[InlineData("zh-Hans")]
[InlineData("es")]
[InlineData("ja")]
public void LocalizationMatrix_ShouldRenderCorrectly_WithLocale(string locale)
{
    // Arrange
    var ui = CreateWebAppUI(locale: locale);
    
    // Act
    var snapshot = ui.Render();
    
    // Assert
    snapshot.ToString().Should().NotContain("{key:"); // no untranslated keys
}

[Theory]
[InlineData(320)]
[InlineData(480)]
[InlineData(768)]
[InlineData(1024)]
[InlineData(1920)]
public void ResponsiveUI_ShouldReflowCorrectly_AtBreakpoint(int viewportWidth)
{
    // Arrange
    var ui = CreateWebAppUI(viewportWidth: viewportWidth);
    
    // Act
    var layout = ui.Render();
    
    // Assert
    layout.TouchTargets.Should().AllSatisfy(t => t.Size.Should().BeGreaterThanOrEqualTo(48));
    layout.Should().NotHaveVerticalOverflow();
}
```

---

## Integration with CI/CD

### Unit & Domain Tests
- Run on every commit (SkyTeam.Domain.Tests)
- xUnit framework with `--logger:trx` for TFS integration
- Goal: 100% domain logic coverage

### Application & Integration Tests
- Run on every PR (SkyTeam.Application.Tests)
- Include Telegram bot command flows
- Include Mini App WebApp endpoint tests
- Mock external APIs (if needed)

### E2E Client Tests (Manual)
- Test plan spreadsheet per release
- Columns: Client | Feature | Device | Locale | Status | Notes
- Telegram iOS/Android/Desktop required for each feature
- Accessibility audit before release

### Performance Tests
- Monitor API response times (< 500ms target)
- Track cockpit update latency (< 100ms target)
- Load test: 10 concurrent games simultaneously

---

## Quick Checklist Template

Use this template for new Mini App features:

```
## Feature: [Name]

### QA Checklist
- [ ] Happy path: [specific action]
- [ ] Edge case 1: [boundary condition]
- [ ] Edge case 2: [error condition]
- [ ] Concurrency: [simultaneous action]
- [ ] Security: [authentication/authorization]
- [ ] Performance: [latency target]
- [ ] Telegram iOS: [manual test]
- [ ] Telegram Android: [manual test]
- [ ] Telegram Desktop: [manual test]

### Suggested Tests (xUnit + FA)
- Test: [MethodName_ShouldExpectedBehavior_WhenCondition](Domain.Tests or Application.Tests)
- Integration test: [flow name] (Application.Tests)

### Done Criteria
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] Manual QA passed on 3+ clients
- [ ] Performance benchmarks met
- [ ] Security review completed
```

---

## References
- **Previous QA Work:** `.squad/orchestration-log/2026-03-01T21-55-06Z-aloha-miniapp-tests.md`
- **Mini App Skill:** `.squad/skills/telegram-mini-apps/SKILL.md`
- **Test Examples:** `SkyTeam.Domain.Tests/*.cs`, `SkyTeam.Application.Tests/*.cs`
- **Aloha Charter:** `.squad/agents/aloha/charter.md`

---

## Reusable Pattern: Persistence QA When Hooks Are Missing

When durable persistence is planned but implementation seams are not yet exposed:

1. Add one **active deterministic concurrency guard** test against current behavior.
2. Add **skipped contract tests** for:
   - persistence round-trip across restart/rehydration
   - stale write/version conflict handling
3. Include clear skip reasons that name the missing hook(s).
4. Write a blocker handoff in `.squad/decisions/inbox/` with required API seams (rehydration + expected version token).
