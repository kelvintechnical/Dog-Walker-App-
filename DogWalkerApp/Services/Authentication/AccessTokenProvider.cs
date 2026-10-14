using DogWalkerApp.Services.Storage;

namespace DogWalkerApp.Services.Authentication;

public interface IAccessTokenProvider
{
    Task<string?> GetAsync();
    Task ClearAsync();
}

// Provider-specific sign-in/refresh writes this key only after an approved PKCE flow.
// There is no default token, fabricated identity or development credential.
public sealed class AccessTokenProvider(ISecureStorageService storage) : IAccessTokenProvider
{
    public const string StorageKey = "friends-with-fur.access-token";
    public Task<string?> GetAsync() => storage.GetAsync(StorageKey);
    public Task ClearAsync() => storage.SetAsync(StorageKey, string.Empty);
}
