namespace DogWalkerApp.Services.Storage;

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
