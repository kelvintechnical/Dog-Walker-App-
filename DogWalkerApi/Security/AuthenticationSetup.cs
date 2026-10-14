using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace DogWalkerApi.Security;

public static class AuthenticationSetup
{
    public static IServiceCollection AddAccountAuthentication(this IServiceCollection services,
        IConfiguration configuration, bool development)
    {
        var authority = configuration["Authentication:Authority"];
        var audience = configuration["Authentication:Audience"];
        var configured = !string.IsNullOrWhiteSpace(authority) && !string.IsNullOrWhiteSpace(audience);
        if (configured && (!Uri.TryCreate(authority, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(uri.UserInfo)))
            throw new InvalidOperationException("Authentication authority must be an HTTPS issuer URL.");
        if ((!configured || string.IsNullOrWhiteSpace(authority) != string.IsNullOrWhiteSpace(audience)) && !development)
            throw new InvalidOperationException("Configure Authentication:Authority and Authentication:Audience before starting outside Development.");

        services.AddHttpContextAccessor();
        services.AddScoped<AccountAccess>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = true;
            if (configured) { options.Authority = authority; options.Audience = audience; }
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = authority,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256 },
                NameClaimType = "sub"
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // SignalR transports may need a query token, never accept one on ordinary REST endpoints.
                    if (context.Request.Path.StartsWithSegments("/hubs/walks")
                        && context.Request.Query.TryGetValue("access_token", out var token))
                        context.Token = token;
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    if (string.IsNullOrWhiteSpace(context.Principal?.FindFirst("sub")?.Value))
                    { context.Fail("Missing subject."); return; }
                    // Recheck disabled identities on every authenticated HTTP request; authorization uses SQL bindings.
                    var access = context.HttpContext.RequestServices.GetRequiredService<AccountAccess>();
                    try { await access.FindAsync(context.HttpContext.RequestAborted, context.Principal); }
                    catch (ResourceAccessException) { context.Fail("Account unavailable."); }
                }
            };
        });
        services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser().Build());
        return services;
    }
}
