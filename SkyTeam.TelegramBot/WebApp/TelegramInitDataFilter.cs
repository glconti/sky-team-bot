using Microsoft.Extensions.Options;
using SkyTeam.TelegramBot.WebApp;

namespace SkyTeam.TelegramBot.WebApp;

public sealed record TelegramInitDataContext(
    TelegramWebAppUser Viewer,
    string? StartParam,
    DateTimeOffset AuthDate);

public sealed class TelegramInitDataFilter(
    TelegramInitDataValidator validator,
    IOptions<TelegramBotOptions> botOptions,
    IOptions<WebAppOptions> webAppOptions) : IEndpointFilter
{
    public const string ContextItemKey = "tg.webapp.context";

    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var initData = context.HttpContext.Request.Headers["X-Telegram-Init-Data"].ToString();
        var maxAgeSeconds = webAppOptions.Value.InitDataMaxAgeSeconds <= 0 ? 300 : webAppOptions.Value.InitDataMaxAgeSeconds;

        var result = validator.Validate(
            initData,
            botOptions.Value.BotToken ?? string.Empty,
            TimeSpan.FromSeconds(maxAgeSeconds),
            DateTimeOffset.UtcNow);

        if (!result.IsOk)
            return new(Results.Unauthorized());

        var initContext = new TelegramInitDataContext(
            Viewer: result.Viewer!,
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
