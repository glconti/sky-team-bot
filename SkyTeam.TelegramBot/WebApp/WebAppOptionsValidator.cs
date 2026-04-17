using Microsoft.Extensions.Options;

namespace SkyTeam.TelegramBot.WebApp;

public sealed class WebAppOptionsValidator : IValidateOptions<WebAppOptions>
{
    private const string MiniAppUrlError = "WebApp:MiniAppUrl (or SKYTEAM_MINI_APP_URL) must be an absolute HTTPS URL without query or fragment.";
    private const string MiniAppShortNameError = "WebApp:MiniAppShortName (or SKYTEAM_MINI_APP_SHORT_NAME) must be 3-32 characters and contain only letters, digits, or underscores.";

    public ValidateOptionsResult Validate(string? name, WebAppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.MiniAppUrl))
        {
            if (!Uri.TryCreate(options.MiniAppUrl, UriKind.Absolute, out var miniAppUri))
                return ValidateOptionsResult.Fail(MiniAppUrlError);

            if (!string.Equals(miniAppUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return ValidateOptionsResult.Fail(MiniAppUrlError);

            if (!string.IsNullOrEmpty(miniAppUri.Query) || !string.IsNullOrEmpty(miniAppUri.Fragment))
                return ValidateOptionsResult.Fail(MiniAppUrlError);
        }

        if (!string.IsNullOrWhiteSpace(options.MiniAppShortName))
        {
            var shortName = options.MiniAppShortName.Trim();
            if (!IsValidMiniAppShortName(shortName))
                return ValidateOptionsResult.Fail(MiniAppShortNameError);
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsValidMiniAppShortName(string value)
    {
        if (value.Length is < 3 or > 32)
            return false;

        return value.All(ch => char.IsLetterOrDigit(ch) || ch == '_');
    }
}
