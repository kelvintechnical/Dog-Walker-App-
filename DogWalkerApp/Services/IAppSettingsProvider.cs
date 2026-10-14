using DogWalker.Core.Configuration;

namespace DogWalkerApp.Services;

public interface IAppSettingsProvider
{
    Task<AppEnvironmentOptions> GetAsync();
}
