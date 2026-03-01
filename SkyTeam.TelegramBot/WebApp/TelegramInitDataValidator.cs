using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace SkyTeam.TelegramBot.WebApp;

public enum TelegramInitDataValidationStatus
{
    Ok,
    MissingInitData,
    MissingHash,
    InvalidHash,
    MissingAuthDate,
    InvalidAuthDate,
    Expired,
    MissingUser,
    InvalidUser
}

public sealed record TelegramInitDataValidationResult(
    TelegramInitDataValidationStatus Status,
    TelegramWebAppUser? Viewer,
    TelegramWebAppChat? Chat,
    string? StartParam,
    DateTimeOffset? AuthDate)
{
    public bool IsOk => Status == TelegramInitDataValidationStatus.Ok;

    // Test-friendly aliases.
    public bool IsValid => IsOk;
    public bool Success => IsOk;
}

public sealed class TelegramInitDataValidator
{
    private static readonly byte[] WebAppDataBytes = Encoding.UTF8.GetBytes("WebAppData");

    public TelegramInitDataValidationResult Validate(string initData, string botToken, TimeSpan maxAge, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(initData))
            return new(TelegramInitDataValidationStatus.MissingInitData, Viewer: null, Chat: null, StartParam: null, AuthDate: null);

        if (string.IsNullOrWhiteSpace(botToken))
            return new(TelegramInitDataValidationStatus.InvalidHash, Viewer: null, Chat: null, StartParam: null, AuthDate: null);

        var parsed = QueryHelpers.ParseQuery(initData);

        if (!parsed.TryGetValue("hash", out var hashValues) || string.IsNullOrWhiteSpace(hashValues.ToString()))
            return new(TelegramInitDataValidationStatus.MissingHash, Viewer: null, Chat: null, StartParam: null, AuthDate: null);

        var hashHex = hashValues.ToString();

        byte[] providedHash;
        try
        {
            providedHash = Convert.FromHexString(hashHex);
        }
        catch
        {
            return new(TelegramInitDataValidationStatus.InvalidHash, Viewer: null, Chat: null, StartParam: null, AuthDate: null);
        }

        if (!parsed.TryGetValue("auth_date", out var authDateValues) || string.IsNullOrWhiteSpace(authDateValues.ToString()))
            return new(TelegramInitDataValidationStatus.MissingAuthDate, Viewer: null, Chat: null, StartParam: null, AuthDate: null);

        if (!long.TryParse(authDateValues.ToString(), out var authDateSeconds))
            return new(TelegramInitDataValidationStatus.InvalidAuthDate, Viewer: null, Chat: null, StartParam: null, AuthDate: null);

        var authDate = DateTimeOffset.FromUnixTimeSeconds(authDateSeconds);
        if (now - authDate > maxAge)
            return new(TelegramInitDataValidationStatus.Expired, Viewer: null, Chat: null, StartParam: null, AuthDate: authDate);

        var dataCheckString = BuildDataCheckString(parsed);

        var secretKey = HMACSHA256.HashData(WebAppDataBytes, Encoding.UTF8.GetBytes(botToken));
        var expectedHash = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));

        if (expectedHash.Length != providedHash.Length || !CryptographicOperations.FixedTimeEquals(expectedHash, providedHash))
            return new(TelegramInitDataValidationStatus.InvalidHash, Viewer: null, Chat: null, StartParam: null, AuthDate: authDate);

        if (!parsed.TryGetValue("user", out var userValues) || string.IsNullOrWhiteSpace(userValues.ToString()))
            return new(TelegramInitDataValidationStatus.MissingUser, Viewer: null, Chat: null, StartParam: null, AuthDate: authDate);

        var userJson = userValues.ToString();
        if (!TryParseUser(userJson, out var viewer))
            return new(TelegramInitDataValidationStatus.InvalidUser, Viewer: null, Chat: null, StartParam: null, AuthDate: authDate);

        TelegramWebAppChat? chat = null;
        if (parsed.TryGetValue("chat", out var chatValues) && !string.IsNullOrWhiteSpace(chatValues.ToString()))
            _ = TryParseChat(chatValues.ToString(), out chat);

        var startParam = parsed.TryGetValue("start_param", out var startParamValues)
            ? startParamValues.ToString()
            : null;

        return new(TelegramInitDataValidationStatus.Ok, viewer, chat, startParam, authDate);
    }

    private static string BuildDataCheckString(IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues> parsed)
    {
        var fields = parsed
            .Where(p => !string.Equals(p.Key, "hash", StringComparison.Ordinal) && p.Value.Count > 0)
            .Select(p => (Key: p.Key, Value: p.Value.ToString()))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}");

        return string.Join("\n", fields);
    }

    private static bool TryParseUser(string userJson, out TelegramWebAppUser user)
    {
        try
        {
            using var doc = JsonDocument.Parse(userJson);

            if (!doc.RootElement.TryGetProperty("id", out var idProp) || !idProp.TryGetInt64(out var userId))
            {
                user = default!;
                return false;
            }

            var username = doc.RootElement.TryGetProperty("username", out var usernameProp)
                ? usernameProp.GetString()
                : null;

            var firstName = doc.RootElement.TryGetProperty("first_name", out var firstNameProp)
                ? firstNameProp.GetString()
                : null;

            var lastName = doc.RootElement.TryGetProperty("last_name", out var lastNameProp)
                ? lastNameProp.GetString()
                : null;

            var displayName = !string.IsNullOrWhiteSpace(username)
                ? "@" + username
                : string.Join(" ", new[] { firstName, lastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = userId.ToString();

            user = new(userId, displayName);
            return true;
        }
        catch
        {
            user = default!;
            return false;
        }
    }

    private static bool TryParseChat(string chatJson, out TelegramWebAppChat chat)
    {
        try
        {
            using var doc = JsonDocument.Parse(chatJson);

            if (!doc.RootElement.TryGetProperty("id", out var idProp) || !idProp.TryGetInt64(out var chatId))
            {
                chat = default!;
                return false;
            }

            var type = doc.RootElement.TryGetProperty("type", out var typeProp)
                ? typeProp.GetString()
                : null;

            type ??= "unknown";

            chat = new(chatId, type);
            return true;
        }
        catch
        {
            chat = default!;
            return false;
        }
    }
}
