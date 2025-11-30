using CommunityToolkit.Maui;
using DogWalker.Core.Configuration;
using DogWalkerApp.Services;
using DogWalkerApp.Services.Api;
using DogWalkerApp.Services.Gps;
using DogWalkerApp.Services.Media;
using DogWalkerApp.Services.Messaging;
using DogWalkerApp.Services.Storage;
using DogWalkerApp.ViewModels;
using DogWalkerApp.Views;
using Microsoft.Extensions.Logging;
using Refit;

namespace DogWalkerApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddSingleton<IAppSettingsProvider, AppSettingsProvider>();
        builder.Services.AddSingleton(new AppEnvironmentOptions());

        builder.Services.AddRefitClient<IDogWalkerApi>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri("https://localhost:5001");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        builder.Services.AddSingleton<ISecureStorageService, SecureStorageService>();
        builder.Services.AddSingleton<IMediaCaptureService, MediaCaptureService>();
        builder.Services.AddSingleton<IGpsTrackerService, GpsTrackerService>();
        builder.Services.AddSingleton<IRealTimeMessagingService, RealTimeMessagingService>();

        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<BookingsViewModel>();
        builder.Services.AddTransient<ClientsViewModel>();
        builder.Services.AddTransient<MessagesViewModel>();
        builder.Services.AddTransient<WalkTrackerViewModel>();

        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<BookingsPage>();
        builder.Services.AddTransient<ClientsPage>();
        builder.Services.AddTransient<MessagesPage>();
        builder.Services.AddTransient<WalkTrackerPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
