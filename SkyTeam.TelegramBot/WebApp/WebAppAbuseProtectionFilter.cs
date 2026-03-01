using System.Globalization;

namespace SkyTeam.TelegramBot.WebApp;

public sealed class WebAppAbuseProtectionFilter(
    WebAppAbuseProtector abuseProtector,
    ILogger<WebAppAbuseProtectionFilter> logger) : IEndpointFilter
{
    private const string LobbyCreatePath = "/api/webapp/lobby/new";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var now = DateTimeOffset.UtcNow;
        var initDataContext = httpContext.GetTelegramInitDataContext();

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!abuseProtector.TryAllowIp(ipAddress, now, out var ipRetryAfter))
            return new(Reject(httpContext, "ip", ipAddress, ipRetryAfter));

        var userKey = initDataContext.Viewer.UserId.ToString(CultureInfo.InvariantCulture);
        if (!abuseProtector.TryAllowUser(initDataContext.Viewer.UserId, now, out var userRetryAfter))
            return new(Reject(httpContext, "user", userKey, userRetryAfter));

        if (IsLobbyCreateRequest(httpContext.Request)
            && !abuseProtector.TryAllowLobbyCreate(initDataContext.Viewer.UserId, now, out var lobbyRetryAfter))
            return new(Reject(httpContext, "lobby-create", userKey, lobbyRetryAfter));

        return next(context);
    }

    private static bool IsLobbyCreateRequest(HttpRequest request)
        => HttpMethods.IsPost(request.Method)
           && string.Equals(request.Path.Value, LobbyCreatePath, StringComparison.OrdinalIgnoreCase);

    private IResult Reject(HttpContext httpContext, string scope, string key, TimeSpan retryAfter)
    {
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        logger.LogWarning(
            "WebApp request throttled by abuse guardrail. Scope={Scope} Key={Key} Path={Path} RetryAfterSeconds={RetryAfterSeconds}",
            scope,
            key,
            httpContext.Request.Path.Value,
            retryAfterSeconds);

        return Results.Json(
            new { error = "Too many requests. Please retry later." },
            statusCode: StatusCodes.Status429TooManyRequests);
    }
}
