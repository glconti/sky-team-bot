using System.Collections.Concurrent;
using System.Globalization;

namespace SkyTeam.TelegramBot.WebApp;

public sealed class WebAppAbuseProtector
{
    private static readonly TimeSpan PerUserWindow = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan PerIpWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan LobbyCreateWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan MutationIdempotencyWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan IdempotencySweepWindow = TimeSpan.FromMinutes(1);
    private const int MaxRequestsPerUserWindow = 10;
    private const int MaxRequestsPerIpWindow = 100;
    private const int MaxLobbyCreatesPerWindow = 1;

    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _userCounters = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _ipCounters = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SlidingWindowCounter> _lobbyCreateCounters = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _mutationIdempotencyKeys = new(StringComparer.Ordinal);
    private readonly Lock _mutationIdempotencySync = new();
    private DateTimeOffset _lastIdempotencySweepUtc = DateTimeOffset.MinValue;

    public bool TryAllowUser(long userId, DateTimeOffset now, out TimeSpan retryAfter)
        => TryAllow(
            _userCounters,
            userId.ToString(CultureInfo.InvariantCulture),
            PerUserWindow,
            MaxRequestsPerUserWindow,
            now,
            out retryAfter);

    public bool TryAllowIp(string ipAddress, DateTimeOffset now, out TimeSpan retryAfter)
    {
        var key = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress;
        return TryAllow(_ipCounters, key, PerIpWindow, MaxRequestsPerIpWindow, now, out retryAfter);
    }

    public bool TryAllowLobbyCreate(long userId, DateTimeOffset now, out TimeSpan retryAfter)
        => TryAllow(
            _lobbyCreateCounters,
            userId.ToString(CultureInfo.InvariantCulture),
            LobbyCreateWindow,
            MaxLobbyCreatesPerWindow,
            now,
            out retryAfter);

    public bool TryAllowMutationIdempotency(
        long userId,
        string? gameId,
        string actionPath,
        string idempotencyKey,
        DateTimeOffset now,
        out TimeSpan retryAfter)
    {
        var normalizedActionPath = string.IsNullOrWhiteSpace(actionPath) ? "unknown" : actionPath;
        var normalizedGameId = string.IsNullOrWhiteSpace(gameId) ? "unknown" : gameId;
        var compositeKey = $"{userId}:{normalizedGameId}:{normalizedActionPath}:{idempotencyKey}";

        lock (_mutationIdempotencySync)
        {
            if (now - _lastIdempotencySweepUtc >= IdempotencySweepWindow)
            {
                SweepExpiredIdempotencyKeys(now);
                _lastIdempotencySweepUtc = now;
            }

            if (_mutationIdempotencyKeys.TryGetValue(compositeKey, out var expiresAtUtc) && expiresAtUtc > now)
            {
                retryAfter = expiresAtUtc - now;
                if (retryAfter <= TimeSpan.Zero)
                    retryAfter = TimeSpan.FromSeconds(1);

                return false;
            }

            _mutationIdempotencyKeys[compositeKey] = now + MutationIdempotencyWindow;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    private void SweepExpiredIdempotencyKeys(DateTimeOffset now)
    {
        foreach (var pair in _mutationIdempotencyKeys)
        {
            if (pair.Value > now)
                continue;

            _mutationIdempotencyKeys.TryRemove(pair.Key, out _);
        }
    }

    private static bool TryAllow(
        ConcurrentDictionary<string, SlidingWindowCounter> counters,
        string key,
        TimeSpan window,
        int maxRequests,
        DateTimeOffset now,
        out TimeSpan retryAfter)
    {
        var counter = counters.GetOrAdd(key, _ => new SlidingWindowCounter(window, maxRequests));
        return counter.TryConsume(now, out retryAfter);
    }

    private sealed class SlidingWindowCounter(TimeSpan window, int maxRequests)
    {
        private readonly TimeSpan _window = window;
        private readonly int _maxRequests = maxRequests;
        private readonly Queue<DateTimeOffset> _timestamps = [];
        private readonly Lock _sync = new();

        public bool TryConsume(DateTimeOffset now, out TimeSpan retryAfter)
        {
            lock (_sync)
            {
                Trim(now);

                if (_timestamps.Count >= _maxRequests)
                {
                    var oldest = _timestamps.Peek();
                    retryAfter = oldest + _window - now;
                    if (retryAfter <= TimeSpan.Zero)
                        retryAfter = TimeSpan.FromSeconds(1);

                    return false;
                }

                _timestamps.Enqueue(now);
                retryAfter = TimeSpan.Zero;
                return true;
            }
        }

        private void Trim(DateTimeOffset now)
        {
            while (_timestamps.Count > 0 && now - _timestamps.Peek() >= _window)
                _timestamps.Dequeue();
        }
    }
}
