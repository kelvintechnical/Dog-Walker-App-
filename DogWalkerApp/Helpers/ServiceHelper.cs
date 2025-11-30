using Microsoft.Extensions.DependencyInjection;

namespace DogWalkerApp.Helpers;

public static class ServiceHelper
{
    public static T GetRequiredService<T>() where T : notnull =>
        Current.GetRequiredService<T>();

    private static IServiceProvider Current =>
        Application.Current?.Handler?.MauiContext?.Services
        ?? throw new InvalidOperationException("MAUI context not available.");
}
