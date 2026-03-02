using System.Globalization;

namespace SkyTeam.TelegramBot.WebApp;

public sealed class WebAppAbuseProtectionFilter(
    WebAppAbuseProtector abuseProtector,
    ILogger<WebAppAbuseProtectionFilter> logger) : IEndpointFilter
{
    private const string LobbyCreatePath = "/api/webapp/lobby/new";
    private const string RollGamePath = "/api/webapp/game/roll";
    private const string PlaceDiePath = "/api/webapp/game/place";
    private const string UndoPlacementPath = "/api/webapp/game/undo";
    private const string IdempotencyKeyHeaderName = "X-Idempotency-Key";
    private const int MaxIdempotencyKeyLength = 64;
    private const long MaxJsonPayloadBytes = 2048;

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var now = DateTimeOffset.UtcNow;
        var initDataContext = httpContext.GetTelegramInitDataContext();
        var request = httpContext.Request;

        if (HasOversizedPayload(request))
            return new(RejectBadRequest(
                httpContext,
                initDataContext.Viewer.UserId,
                reason: "payload-too-large",
                error: "Request payload exceeds max size.",
                retryHint: "Reduce payload size and retry."));

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (!abuseProtector.TryAllowIp(ipAddress, now, out var ipRetryAfter))
            return new(Reject(httpContext, "ip", ipAddress, ipRetryAfter));

        var userKey = initDataContext.Viewer.UserId.ToString(CultureInfo.InvariantCulture);
        if (!abuseProtector.TryAllowUser(initDataContext.Viewer.UserId, now, out var userRetryAfter))
            return new(Reject(httpContext, "user", userKey, userRetryAfter));

        if (IsLobbyCreateRequest(httpContext.Request)
            && !abuseProtector.TryAllowLobbyCreate(initDataContext.Viewer.UserId, now, out var lobbyRetryAfter))
            return new(Reject(httpContext, "lobby-create", userKey, lobbyRetryAfter));

        if (IsMutationRequest(request))
        {
            var idempotencyKey = request.Headers[IdempotencyKeyHeaderName].ToString().Trim();
            if (string.IsNullOrWhiteSpace(idempotencyKey))
                return new(RejectBadRequest(
                    httpContext,
                    initDataContext.Viewer.UserId,
                    reason: "missing-idempotency-key",
                    error: "Missing idempotency key.",
                    retryHint: $"Provide {IdempotencyKeyHeaderName} header and retry."));

            if (idempotencyKey.Length > MaxIdempotencyKeyLength || idempotencyKey.Any(char.IsWhiteSpace))
                return new(RejectBadRequest(
                    httpContext,
                    initDataContext.Viewer.UserId,
                    reason: "invalid-idempotency-key",
                    error: "Invalid idempotency key.",
                    retryHint: $"Use a non-whitespace key with max {MaxIdempotencyKeyLength} characters."));

            var gameId = request.Query["gameId"].ToString();
            if (!abuseProtector.TryAllowMutationIdempotency(
                    initDataContext.Viewer.UserId,
                    gameId,
                    request.Path.Value ?? string.Empty,
                    idempotencyKey,
                    now,
                    out var replayRetryAfter))
            {
                var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(replayRetryAfter.TotalSeconds));
                return new(RejectBadRequest(
                    httpContext,
                    initDataContext.Viewer.UserId,
                    reason: "duplicate-idempotency-key",
                    error: "Duplicate idempotency key for this action.",
                    retryHint: $"Use a new idempotency key, or retry in {retryAfterSeconds} seconds."));
            }
        }

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
            new
            {
                error = "Too many requests. Please retry later.",
                retryAfterSeconds,
                retryHint = $"Retry after {retryAfterSeconds} seconds."
            },
            statusCode: StatusCodes.Status429TooManyRequests);
    }

    private IResult RejectBadRequest(
        HttpContext httpContext,
        long userId,
        string reason,
        string error,
        string retryHint)
    {
        logger.LogWarning(
            "WebApp request rejected by abuse guardrail. Reason={Reason} UserId={UserId} Path={Path}",
            reason,
            userId,
            httpContext.Request.Path.Value);

        return Results.BadRequest(new
        {
            error,
            retryHint
        });
    }

    private static bool IsMutationRequest(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
            return false;

        var path = request.Path.Value;
        return string.Equals(path, RollGamePath, StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, PlaceDiePath, StringComparison.OrdinalIgnoreCase)
               || string.Equals(path, UndoPlacementPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasOversizedPayload(HttpRequest request)
        => request.ContentLength.HasValue
           && request.ContentLength.Value > MaxJsonPayloadBytes
           && HttpMethods.IsPost(request.Method);
}
