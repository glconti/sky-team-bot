using Microsoft.Extensions.Options;
using SkyTeam.TelegramBot.WebApp;

namespace SkyTeam.TelegramBot.WebApp;

public sealed record TelegramInitDataContext(
    TelegramWebAppUser Viewer,
    TelegramWebAppChat? Chat,
    string? StartParam,
    DateTimeOffset AuthDate);

public sealed class TelegramInitDataFilter(
    TelegramInitDataValidator validator,
    IOptions<TelegramBotOptions> botOptions,
    IOptions<WebAppOptions> webAppOptions,
    ILogger<TelegramInitDataFilter> logger) : IEndpointFilter
{
    public const string ContextItemKey = "tg.webapp.context";
    private const int MaxInitDataHeaderLength = 4096;

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var initData = context.HttpContext.Request.Headers["X-Telegram-Init-Data"].ToString();
        if (initData.Length > MaxInitDataHeaderLength)
        {
            logger.LogWarning("Rejected initData header that exceeded max length. Path={Path}", context.HttpContext.Request.Path.Value);
            return new(Results.BadRequest(new { error = "Invalid initData." }));
        }

        var maxAgeSeconds = webAppOptions.Value.InitDataMaxAgeSeconds <= 0 ? 300 : webAppOptions.Value.InitDataMaxAgeSeconds;

        var result = validator.Validate(
            initData,
            botOptions.Value.BotToken ?? string.Empty,
            TimeSpan.FromSeconds(maxAgeSeconds),
            DateTimeOffset.UtcNow);

        if (!result.IsOk)
        {
            logger.LogWarning("Rejected initData request. Status={Status} Path={Path}", result.Status, context.HttpContext.Request.Path.Value);
            return new(Results.Unauthorized());
        }

        var initContext = new TelegramInitDataContext(
            Viewer: result.Viewer!,
            Chat: result.Chat,
            StartParam: result.StartParam,
            AuthDate: result.AuthDate!.Value);

        context.HttpContext.Items[ContextItemKey] = initContext;

        return next(context);
    }
}

public static class TelegramInitDataHttpContextExtensions
{
    public static TelegramInitDataContext GetTelegramInitDataContext(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.Items.TryGetValue(TelegramInitDataFilter.ContextItemKey, out var value) && value is TelegramInitDataContext ctx
            ? ctx
            : throw new InvalidOperationException("Telegram initData context is missing.");
    }
}
