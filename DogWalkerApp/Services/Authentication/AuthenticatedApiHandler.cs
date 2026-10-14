using System.Net;
using System.Net.Http.Headers;

namespace DogWalkerApp.Services.Authentication;

public sealed class AuthenticatedApiHandler(IAccessTokenProvider tokens, IAppSettingsProvider settings) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokens.GetAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Sign in before accessing your account. Authentication provider setup is required.");
        if (request.RequestUri?.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Authentication requires an HTTPS API endpoint.");
        var configuration = await settings.GetAsync();
        if (!Uri.TryCreate(configuration.ApiBaseUrl, UriKind.Absolute, out var endpoint)
            || endpoint.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new InvalidOperationException("Configure a secure API endpoint before signing in.");
        request.RequestUri = new Uri(endpoint, request.RequestUri!.PathAndQuery);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await base.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await tokens.ClearAsync();
            response.Dispose();
            throw new InvalidOperationException("Your session expired. Sign in again.");
        }
        return response;
    }
}
