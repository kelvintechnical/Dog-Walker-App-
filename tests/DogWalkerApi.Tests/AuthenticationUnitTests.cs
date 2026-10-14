using System.Security.Claims;
using DogWalkerApi.Security;
using DogWalkerApi.Validators;
using DogWalker.Core.Requests;
using DogWalker.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Xunit;

namespace DogWalkerApi.Tests;

public class AuthenticationUnitTests
{
    [Theory]
    [InlineData("", "", false)]
    [InlineData("http://identity.example.test", "api", true)]
    [InlineData("https://identity.example.test", "", false)]
    public void Missing_or_insecure_production_identity_configuration_fails_closed(string authority, string audience, bool development)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Authentication:Authority"] = authority, ["Authentication:Audience"] = audience }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddAccountAuthentication(config, development));
    }

    [Fact]
    public void Provider_neutral_options_preserve_all_token_validation_boundaries()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["Authentication:Authority"] = ApiFactory.Issuer, ["Authentication:Audience"] = ApiFactory.Audience }).Build();
        var services = new ServiceCollection(); services.AddLogging(); services.AddAccountAuthentication(config, false);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.True(options.RequireHttpsMetadata); Assert.False(options.MapInboundClaims);
        var validation = options.TokenValidationParameters;
        Assert.True(validation.ValidateIssuer); Assert.True(validation.ValidateAudience); Assert.True(validation.ValidateLifetime);
        Assert.True(validation.ValidateIssuerSigningKey); Assert.True(validation.RequireSignedTokens); Assert.True(validation.RequireExpirationTime);
        Assert.Equal(ApiFactory.Issuer, validation.ValidIssuer); Assert.Equal(ApiFactory.Audience, validation.ValidAudience);
        Assert.DoesNotContain("none", validation.ValidAlgorithms);
    }

    [Fact]
    public void Anonymous_or_missing_subject_identity_is_not_accepted()
    {
        Assert.Equal(401, Assert.Throws<ResourceAccessException>(() => AccountAccess.Identity(new ClaimsPrincipal())).StatusCode);
        Assert.Equal(401, Assert.Throws<ResourceAccessException>(() => AccountAccess.Identity(new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("iss", ApiFactory.Issuer) }, "authenticated")))).StatusCode);
    }

    [Fact]
    public void Invalid_service_and_reversed_booking_times_are_rejected()
    {
        var validator = new CreateBookingRequestValidator();
        var request = new CreateBookingRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (ServiceType)999,
            DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow, "");
        var result = validator.Validate(request);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.ServiceType));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(request.EndTimeUtc));
    }
}
