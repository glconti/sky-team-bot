namespace SkyTeam.TelegramBot;

public enum CallbackMenuValidationStatus
{
    Valid,
    UnknownOrExpired,
    Duplicate
}

public sealed class CallbackMenuStateStore
{
    private readonly TimeSpan _timeToLive;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly object _sync = new();
    private readonly Dictionary<MenuStateKey, MenuState> _stateByKey = new();

    public CallbackMenuStateStore(TimeSpan? timeToLive = null, Func<DateTimeOffset>? utcNow = null)
    {
        _timeToLive = timeToLive ?? TimeSpan.FromHours(1);
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public void RegisterGroupMenu(long groupChatId, int messageId, IReadOnlyCollection<string> allowedCallbackData, long? userId = null)
    {
        ArgumentNullException.ThrowIfNull(allowedCallbackData);

        lock (_sync)
        {
            CleanupExpiredLocked();

            var now = _utcNow();
            var boundUserId = userId ?? 0;
            var key = new MenuStateKey(boundUserId, groupChatId, messageId);

            RemoveGroupStatesLocked(groupChatId);

            _stateByKey[key] = new MenuState(
                allowedCallbackData.ToHashSet(StringComparer.Ordinal),
                now.Add(_timeToLive));
        }
    }

    public CallbackMenuValidationStatus ValidateAndMarkProcessed(
        long userId,
        long groupChatId,
        int messageId,
        string callbackData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(callbackData);

        lock (_sync)
        {
            CleanupExpiredLocked();

            if (!TryGetStateLocked(userId, groupChatId, messageId, out var state))
                return CallbackMenuValidationStatus.UnknownOrExpired;

            if (!state.AllowedCallbackData.Contains(callbackData))
                return CallbackMenuValidationStatus.UnknownOrExpired;

            var callbackKey = $"{userId}:{callbackData}";
            if (!state.ProcessedCallbacks.Add(callbackKey))
                return CallbackMenuValidationStatus.Duplicate;

            return CallbackMenuValidationStatus.Valid;
        }
    }

    private bool TryGetStateLocked(long userId, long groupChatId, int messageId, out MenuState state)
    {
        if (_stateByKey.TryGetValue(new(userId, groupChatId, messageId), out state!))
            return true;

        return _stateByKey.TryGetValue(new(0, groupChatId, messageId), out state!);
    }

    private void CleanupExpiredLocked()
    {
        var now = _utcNow();
        var expiredKeys = _stateByKey
            .Where(pair => pair.Value.ExpiresAt <= now)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var expiredKey in expiredKeys)
            _stateByKey.Remove(expiredKey);
    }

    private void RemoveGroupStatesLocked(long groupChatId)
    {
        var staleKeys = _stateByKey.Keys
            .Where(key => key.GroupChatId == groupChatId)
            .ToArray();

        foreach (var staleKey in staleKeys)
            _stateByKey.Remove(staleKey);
    }

    private sealed record MenuState(
        HashSet<string> AllowedCallbackData,
        DateTimeOffset ExpiresAt)
    {
        public HashSet<string> ProcessedCallbacks { get; } = new(StringComparer.Ordinal);
    }

    private readonly record struct MenuStateKey(long UserId, long GroupChatId, int MessageId);
}
