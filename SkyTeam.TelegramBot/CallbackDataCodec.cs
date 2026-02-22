namespace SkyTeam.TelegramBot;

public static class CallbackDataCodec
{
    private const string VersionPrefix = "v1:grp:";
    private const int TelegramCallbackDataMaxLength = 64;

    private static readonly HashSet<string> SupportedActions =
    [
        "new",
        "join",
        "start",
        "roll",
        "place-dm",
        "refresh"
    ];

    public static string EncodeGroupAction(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return VersionPrefix + action;
    }

    public static bool TryDecodeGroupAction(string? callbackData, out string canonicalCallbackData)
    {
        canonicalCallbackData = string.Empty;

        if (string.IsNullOrWhiteSpace(callbackData) || callbackData.Length > TelegramCallbackDataMaxLength)
            return false;

        if (!callbackData.StartsWith(VersionPrefix, StringComparison.Ordinal))
            return false;

        var action = callbackData[VersionPrefix.Length..];
        if (!SupportedActions.Contains(action))
            return false;

        canonicalCallbackData = EncodeGroupAction(action);
        return true;
    }
}
