using System.Net;
using DogWalkerApp.Services.Authentication;
using DogWalkerApp.Services.Storage;
using Xunit;

namespace DogWalkerApi.Tests;

public class MobileAuthenticationTests
{
    private sealed class Settings : DogWalkerApp.Services.IAppSettingsProvider
    {
        public Task<DogWalker.Core.Configuration.AppEnvironmentOptions> GetAsync()
            => Task.FromResult(new DogWalker.Core.Configuration.AppEnvironmentOptions { ApiBaseUrl = "https://api.example.test" });
    }
    private sealed class MemoryStorage : ISecureStorageService
    {
        public Dictionary<string, string> Values { get; } = new();
        public Task SetAsync(string key, string value) { Values[key] = value; return Task.CompletedTask; }
        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));
    }
    private sealed class RecordingTransport(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public int Requests { get; private set; }
        public string? Scheme { get; private set; }
        public string? Credential { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Requests++; Scheme = request.Headers.Authorization?.Scheme; Credential = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    [Fact]
    public async Task Missing_token_never_calls_network_or_uses_fabricated_credentials()
    {
        var transport = new RecordingTransport();
        using var client = new HttpClient(new AuthenticatedApiHandler(new AccessTokenProvider(new MemoryStorage()), new Settings()) { InnerHandler = transport });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("https://api.example.test/api/clients"));
        Assert.Equal(0, transport.Requests);
    }

    [Fact]
    public async Task Stored_access_token_uses_bearer_header_only_over_https()
    {
        var storage = new MemoryStorage(); await storage.SetAsync(AccessTokenProvider.StorageKey, "ephemeral-unit-test-token");
        var transport = new RecordingTransport();
        using var client = new HttpClient(new AuthenticatedApiHandler(new AccessTokenProvider(storage), new Settings()) { InnerHandler = transport });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("http://api.example.test/api/clients"));
        Assert.Equal(0, transport.Requests);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("https://api.example.test/api/clients")).StatusCode);
        Assert.Equal("Bearer", transport.Scheme); Assert.Equal("ephemeral-unit-test-token", transport.Credential);
    }

    [Fact]
    public async Task Expired_session_clears_token_and_requires_new_sign_in_without_silent_retry()
    {
        var storage = new MemoryStorage(); await storage.SetAsync(AccessTokenProvider.StorageKey, "ephemeral-unit-test-token");
        var transport = new RecordingTransport(HttpStatusCode.Unauthorized); var tokens = new AccessTokenProvider(storage);
        using var client = new HttpClient(new AuthenticatedApiHandler(tokens, new Settings()) { InnerHandler = transport });
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("https://api.example.test/api/clients"));
        Assert.Equal(string.Empty, await tokens.GetAsync()); Assert.Equal(1, transport.Requests);
    }
}
