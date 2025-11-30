using DogWalker.Core.Enums;

namespace DogWalker.Core.Configuration;

public class AppEnvironmentOptions
{
    public const string SectionName = "App";

    public EnvironmentTier Tier { get; set; } = EnvironmentTier.Development;
    public string ApiBaseUrl { get; set; } = "https://localhost:5001";
    public string SignalRHubUrl { get; set; } = "https://localhost:5001/hubs/walks";
    public string StripePublishableKey { get; set; } = string.Empty;
    public string StripeSecretKey { get; set; } = string.Empty;
    public string SendGridApiKey { get; set; } = string.Empty;
    public string MapsApiKey { get; set; } = string.Empty;
}
