namespace SkyTeam.Application.Tests.Telegram;

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Xunit.Sdk;

public sealed class Issue59WebAppInitDataValidationTests
{
    private const string TestBotToken = "TEST_BOT_TOKEN:123456";
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);

    [Fact]
    public void TelegramInitDataValidator_ShouldValidate_WhenInitDataIsCorrect()
    {
        // Arrange
        var validate = ResolveValidateMethodOrSkip();
        var initData = BuildInitData(
            botToken: TestBotToken,
            authDate: Now.AddSeconds(-60),
            userId: 111,
            displayName: "Alice",
            startParam: "123456789");

        // Act
        var result = validate(initData, TestBotToken, MaxAge, Now);

        // Assert
        AssertValidationSucceeded(result, expectedUserId: 111, expectedDisplayName: "Alice",
            expectedStartParam: "123456789");
    }

    [Fact]
    public void TelegramInitDataValidator_ShouldReject_WhenHashIsWrong()
    {
        // Arrange
        var validate = ResolveValidateMethodOrSkip();
        var initData = BuildInitData(
            botToken: TestBotToken,
            authDate: Now.AddSeconds(-60),
            userId: 111,
            displayName: "Alice",
            startParam: "123456789");
        var tampered = TamperHash(initData);

        // Act
        var result = validate(tampered, TestBotToken, MaxAge, Now);

        // Assert
        AssertValidationFailed(result);
    }

    [Fact]
    public void TelegramInitDataValidator_ShouldReject_WhenAuthDateIsExpired()
    {
        // Arrange
        var validate = ResolveValidateMethodOrSkip();
        var initData = BuildInitData(
            botToken: TestBotToken,
            authDate: Now - MaxAge - TimeSpan.FromSeconds(1),
            userId: 111,
            displayName: "Alice",
            startParam: "123456789");

        // Act
        var result = validate(initData, TestBotToken, MaxAge, Now);

        // Assert
        AssertValidationFailed(result);
    }

    [Fact]
    public void TelegramInitDataValidator_ShouldExposeExpiredStatus_ForAuthDateFreshnessUx()
    {
        // Arrange
        var validate = ResolveValidateMethodOrSkip();
        var expiredAuthDate = Now - MaxAge - TimeSpan.FromSeconds(1);
        var initData = BuildInitData(
            botToken: TestBotToken,
            authDate: expiredAuthDate,
            userId: 111,
            displayName: "Alice",
            startParam: "123456789");

        // Act
        var result = validate(initData, TestBotToken, MaxAge, Now);

        // Assert
        AssertValidationFailed(result);
        ReadEnumName(result!, "Status", "ValidationStatus").Should().Be("Expired");
        ReadDateTimeOffset(result!, "AuthDate", "ValidatedAt").Should().Be(expiredAuthDate);
    }

    [Fact]
    public void TelegramInitDataValidator_ShouldUseFixedTimeEquals_WhenSlice59IsImplemented()
    {
        // Arrange
        var sourceFiles = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.Application")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "SkyTeam.TelegramBot"))
        };

        // Act
        var hasValidatorTypeName = sourceFiles.Any(dir =>
            Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Any(file => File.ReadAllText(file).Contains("TelegramInitDataValidator", StringComparison.Ordinal)));

        if (!hasValidatorTypeName)
            throw SkipException.ForSkip("Slice #59 validator is not implemented yet; enable this test once TelegramInitDataValidator exists.");

        var usesFixedTimeEquals = sourceFiles.Any(dir =>
            Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories)
                .Any(file => File.ReadAllText(file).Contains("CryptographicOperations.FixedTimeEquals", StringComparison.Ordinal)));

        // Assert
        usesFixedTimeEquals.Should().BeTrue("hash comparisons must use CryptographicOperations.FixedTimeEquals to avoid timing attacks");
    }

    private static Func<string, string, TimeSpan, DateTimeOffset, object?> ResolveValidateMethodOrSkip()
    {
        var assembliesToScan = new[]
        {
            TryLoadAssembly("SkyTeam.Application"),
            TryLoadAssembly("SkyTeam.TelegramBot")
        }.Where(a => a is not null).Cast<Assembly>().ToArray();

        var validatorType = assembliesToScan
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => string.Equals(t.Name, "TelegramInitDataValidator", StringComparison.Ordinal));

        if (validatorType is null)
            throw SkipException.ForSkip("Slice #59 is not implemented yet; expected a TelegramInitDataValidator type in SkyTeam.Application or SkyTeam.TelegramBot.");

        var validateMethod = validatorType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .FirstOrDefault(m =>
                string.Equals(m.Name, "Validate", StringComparison.Ordinal) &&
                HasSignature(m, typeof(string), typeof(string), typeof(TimeSpan), typeof(DateTimeOffset)));

        if (validateMethod is null)
            throw SkipException.ForSkip(
                "Slice #59 validator contract missing: expected TelegramInitDataValidator.Validate(string initData, string botToken, TimeSpan maxAge, DateTimeOffset now).");

        object? instance = null;
        if (!validateMethod.IsStatic)
        {
            instance = Activator.CreateInstance(validatorType);
            if (instance is null)
                throw SkipException.ForSkip("TelegramInitDataValidator must be constructible for unit testing (parameterless ctor or static Validate).");
        }

        return (initData, botToken, maxAge, now) =>
            validateMethod.Invoke(instance, new object[] { initData, botToken, maxAge, now });
    }

    private static Assembly? TryLoadAssembly(string name)
    {
        try
        {
            return Assembly.Load(name);
        }
        catch
        {
            return null;
        }
    }

    private static bool HasSignature(MethodInfo method, params Type[] parameterTypes)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != parameterTypes.Length) return false;

        for (var i = 0; i < parameters.Length; i++)
            if (parameters[i].ParameterType != parameterTypes[i])
                return false;

        return true;
    }

    private static string BuildInitData(
        string botToken,
        DateTimeOffset authDate,
        long userId,
        string displayName,
        string startParam)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["query_id"] = "AAH-test-query-id",
            ["start_param"] = startParam,
            ["user"] = $"{{\"id\":{userId},\"first_name\":\"{displayName}\"}}"
        };

        var dataCheckString = string.Join("\n", fields
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={p.Value}"));

        var secretKey = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("WebAppData"),
            Encoding.UTF8.GetBytes(botToken));

        var expectedHashBytes = HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        var hashHex = Convert.ToHexString(expectedHashBytes).ToLowerInvariant();

        fields["hash"] = hashHex;

        return string.Join("&", fields
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static string TamperHash(string initData)
    {
        var fields = ParseQueryString(initData);

        if (!fields.TryGetValue("hash", out var hash) || hash.Length == 0)
            return initData;

        var tampered = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
        fields["hash"] = tampered;

        return string.Join("&", fields
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = segment.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length == 2 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static void AssertValidationSucceeded(
        object? result,
        long expectedUserId,
        string expectedDisplayName,
        string expectedStartParam)
    {
        result.Should().NotBeNull();

        ReadIsValid(result!).Should().BeTrue("valid initData must be accepted");

        ReadLong(result!, "UserId", "userId").Should().Be(expectedUserId);
        ReadString(result!, "DisplayName", "displayName", "FirstName", "first_name").Should().Contain(expectedDisplayName);
        ReadString(result!, "StartParam", "start_param", "StartParameter").Should().Be(expectedStartParam);
    }

    private static void AssertValidationFailed(object? result)
    {
        result.Should().NotBeNull();
        ReadIsValid(result!).Should().BeFalse("invalid initData must be rejected");
    }

    private static bool ReadIsValid(object result)
    {
        if (result is bool b) return b;

        var candidates = new[] { "IsOk", "IsValid", "IsSuccess", "Succeeded", "Success" };
        var prop = FindProperty(result, candidates, typeof(bool));
        if (prop is not null) return (bool)prop.GetValue(result)!;

        throw new XunitException(
            "InitDataValidationResult must expose a boolean success flag (IsValid/IsSuccess/Succeeded/Success) so callers and tests can reason about accept/reject.");
    }

    private static string ReadEnumName(object result, params string[] names)
    {
        var prop = FindProperty(result, names, typeof(Enum)) ??
                   result.GetType()
                       .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                       .FirstOrDefault(p => names.Any(name => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) && p.PropertyType.IsEnum);

        if (prop is null)
            throw new XunitException($"Expected validation result to expose {string.Join("/", names)} enum.");

        var value = prop.GetValue(result);
        value.Should().NotBeNull();
        return value!.ToString()!;
    }

    private static DateTimeOffset ReadDateTimeOffset(object result, params string[] names)
    {
        var prop = FindProperty(result, names, typeof(DateTimeOffset)) ??
                   FindProperty(result, names, typeof(DateTimeOffset?));

        if (prop is null)
            throw new XunitException($"Expected validation result to expose {string.Join("/", names)}.");

        var value = prop.GetValue(result);
        if (value is DateTimeOffset dto)
            return dto;

        if (value is null)
            throw new XunitException($"Expected {string.Join("/", names)} to contain a DateTimeOffset value.");

        var nullableValue = (DateTimeOffset?)value;
        if (nullableValue.HasValue)
            return nullableValue.Value;

        throw new XunitException($"Expected {string.Join("/", names)} to contain a DateTimeOffset value.");
    }

    private static long ReadLong(object result, params string[] names)
    {
        var prop = FindProperty(result, names, typeof(long)) ??
                   FindProperty(result, names, typeof(int)) ??
                   FindPropertyInNestedObject(result, names, typeof(long)) ??
                   FindPropertyInNestedObject(result, names, typeof(int));

        if (prop is null)
            throw new XunitException($"Expected validation result to expose {string.Join("/", names)}.");

        var value = prop.GetValue(prop.DeclaringType == result.GetType() ? result : GetNestedInstance(result, prop)!)!;
        return Convert.ToInt64(value);
    }

    private static string ReadString(object result, params string[] names)
    {
        var prop = FindProperty(result, names, typeof(string)) ??
                   FindPropertyInNestedObject(result, names, typeof(string));

        if (prop is null)
            throw new XunitException($"Expected validation result to expose {string.Join("/", names)}.");

        var instance = prop.DeclaringType == result.GetType() ? result : GetNestedInstance(result, prop)!;
        return (string)prop.GetValue(instance)!;
    }

    private static PropertyInfo? FindProperty(object instance, IEnumerable<string> names, Type propertyType)
    {
        var type = instance.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return names
            .Select(name => props.FirstOrDefault(p =>
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) && p.PropertyType == propertyType))
            .FirstOrDefault(p => p is not null);
    }

    private static PropertyInfo? FindPropertyInNestedObject(object instance, IEnumerable<string> names, Type propertyType)
    {
        foreach (var containerName in new[] { "User", "WebAppUser", "Viewer", "TelegramWebAppUser" })
        {
            var containerProp = instance.GetType().GetProperty(containerName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (containerProp is null) continue;

            var container = containerProp.GetValue(instance);
            if (container is null) continue;

            var nested = FindProperty(container, names, propertyType);
            if (nested is not null)
                return nested;
        }

        return null;
    }

    private static object? GetNestedInstance(object instance, PropertyInfo nestedProp)
    {
        foreach (var containerName in new[] { "User", "WebAppUser", "Viewer", "TelegramWebAppUser" })
        {
            var containerProp = instance.GetType().GetProperty(containerName,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (containerProp is null) continue;

            var container = containerProp.GetValue(instance);
            if (container is null) continue;

            if (container.GetType() == nestedProp.DeclaringType)
                return container;
        }

        return null;
    }
}
