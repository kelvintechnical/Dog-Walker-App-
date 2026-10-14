using System.Text.Json;
using System.Text.Json.Serialization;
using DogWalker.Core.Configuration;

namespace DogWalkerApp.Services;

public class AppSettingsProvider : IAppSettingsProvider
{
    private readonly Lazy<Task<AppEnvironmentOptions>> _settings;

    public AppSettingsProvider()
    {
        _settings = new Lazy<Task<AppEnvironmentOptions>>(LoadInternalAsync);
    }

    public Task<AppEnvironmentOptions> GetAsync() => _settings.Value;

    private static async Task<AppEnvironmentOptions> LoadInternalAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("appsettings.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var root = JsonSerializer.Deserialize<JsonElement>(json);
        if (root.TryGetProperty("App", out var appElement))
        {
            var options = appElement.Deserialize<AppEnvironmentOptions>(new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
            if (options is not null)
            {
                return options;
            }
        }

        return new AppEnvironmentOptions();
    }
}
