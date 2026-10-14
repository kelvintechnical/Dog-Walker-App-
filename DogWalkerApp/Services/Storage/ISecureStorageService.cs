namespace DogWalkerApp.Services.Storage;

public interface ISecureStorageService
{
    Task SetAsync(string key, string value);
    Task<string?> GetAsync(string key);
}
