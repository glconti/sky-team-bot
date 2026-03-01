namespace SkyTeam.TelegramBot.Persistence;

using System.Text.Json;
using SkyTeam.Application.GameSessions;

public sealed class JsonGameSessionPersistence(
    IConfiguration configuration,
    IHostEnvironment hostEnvironment) : IGameSessionPersistence
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly string _filePath = ResolveFilePath(
        configuration["Persistence:GameSessionsFilePath"],
        hostEnvironment.ContentRootPath);

    public PersistedGameSessionStoreState Load()
    {
        lock (_sync)
        {
            if (!File.Exists(_filePath))
                return PersistedGameSessionStoreState.Empty;

            using var stream = File.OpenRead(_filePath);
            return JsonSerializer.Deserialize<PersistedGameSessionStoreState>(stream, SerializerOptions)
                ?? PersistedGameSessionStoreState.Empty;
        }
    }

    public void Save(PersistedGameSessionStoreState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        lock (_sync)
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            var tempFilePath = $"{_filePath}.{Guid.NewGuid():N}.tmp";

            try
            {
                using (var stream = File.Create(tempFilePath))
                {
                    JsonSerializer.Serialize(stream, state, SerializerOptions);
                    stream.Flush(flushToDisk: true);
                }

                File.Move(tempFilePath, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }

    private static string ResolveFilePath(string? configuredPath, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(Path.Combine(contentRootPath, "data", "game-sessions.json"));

        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(contentRootPath, configuredPath));
    }
}
