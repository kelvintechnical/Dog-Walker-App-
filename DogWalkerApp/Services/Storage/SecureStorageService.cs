namespace DogWalkerApp.Services.Storage;

public interface ISecureStorageService
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
}

public class SecureStorageService : ISecureStorageService
{
    public Task SetAsync(string key, string value) => SecureStorage.SetAsync(key, value);

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch
        {
            return null;
        }
    }
}
