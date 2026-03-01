namespace SkyTeam.TelegramBot.WebApp;

public sealed class WebAppOptions
{
    public int InitDataMaxAgeSeconds { get; set; } = 300;

    /// <summary>
    /// Public HTTPS URL of the Mini App shell (e.g. https://example.com/). Keep it as a base URL without query/fragment.
    /// </summary>
    public string? MiniAppUrl { get; set; }
}
