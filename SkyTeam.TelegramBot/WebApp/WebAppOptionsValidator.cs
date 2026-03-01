using Microsoft.Extensions.Options;

namespace SkyTeam.TelegramBot.WebApp;

public sealed class WebAppOptionsValidator : IValidateOptions<WebAppOptions>
{
    private const string MiniAppUrlError = "WebApp:MiniAppUrl (or SKYTEAM_MINI_APP_URL) must be an absolute HTTPS URL without query or fragment.";

    public ValidateOptionsResult Validate(string? name, WebAppOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.MiniAppUrl))
            return ValidateOptionsResult.Success;

        if (!Uri.TryCreate(options.MiniAppUrl, UriKind.Absolute, out var miniAppUri))
            return ValidateOptionsResult.Fail(MiniAppUrlError);

        if (!string.Equals(miniAppUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return ValidateOptionsResult.Fail(MiniAppUrlError);

        if (!string.IsNullOrEmpty(miniAppUri.Query) || !string.IsNullOrEmpty(miniAppUri.Fragment))
            return ValidateOptionsResult.Fail(MiniAppUrlError);

        return ValidateOptionsResult.Success;
    }
}
