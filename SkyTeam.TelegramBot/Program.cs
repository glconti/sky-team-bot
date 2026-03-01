using System.Text.Json.Serialization;
using SkyTeam.Application.GameSessions;
using SkyTeam.Application.Lobby;
using SkyTeam.TelegramBot;
using SkyTeam.TelegramBot.WebApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryGroupLobbyStore>();
builder.Services.AddSingleton<InMemoryGroupGameSessionStore>();

builder.Services.Configure<TelegramBotOptions>(options =>
{
    options.BotToken = builder.Configuration["TELEGRAM_BOT_TOKEN"]
        ?? builder.Configuration["Telegram:BotToken"];
});

builder.Services.Configure<WebAppOptions>(options =>
{
    builder.Configuration.GetSection("WebApp").Bind(options);
    options.MiniAppUrl ??= builder.Configuration["SKYTEAM_MINI_APP_URL"];
});

builder.Services.AddSingleton<TelegramInitDataValidator>();
builder.Services.AddSingleton<TelegramBotService>();
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<TelegramBotService>());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapWebAppEndpoints();

app.Run();

public partial class Program;
