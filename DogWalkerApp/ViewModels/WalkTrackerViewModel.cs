using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApp.Services.Api;
using DogWalkerApp.Services.Gps;
using DogWalkerApp.Services.Media;

namespace DogWalkerApp.ViewModels;

public partial class WalkTrackerViewModel : BaseViewModel
{
    private readonly IDogWalkerApi _api;
    private readonly IGpsTrackerService _gpsTracker;
    private readonly IMediaCaptureService _mediaCapture;

    public ObservableCollection<WalkRoutePoint> Route { get; } = new();

    [ObservableProperty]
    private Guid _activeBookingId;

    [ObservableProperty]
    private bool _isTracking;

    public WalkTrackerViewModel(IDogWalkerApi api, IGpsTrackerService gpsTracker, IMediaCaptureService mediaCapture)
    {
        _api = api;
        _gpsTracker = gpsTracker;
        _mediaCapture = mediaCapture;
        Title = "Live Walk";
        _gpsTracker.RoutePointCaptured += (_, request) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Route.Add(new WalkRoutePoint
                {
                    Latitude = request.Latitude,
                    Longitude = request.Longitude,
                    RecordedAtUtc = DateTimeOffset.UtcNow,
                    TotalDistanceMeters = request.TotalDistanceMeters,
                    SpeedMph = request.SpeedMph
                });
            });
        };
    }

    [RelayCommand]
    private Task BeginWalkAsync() => SafeExecuteAsync(async () =>
    {
        ActiveBookingId = Guid.NewGuid();
        IsTracking = true;
        await _gpsTracker.StartAsync(ActiveBookingId);
    });

    [RelayCommand]
    private Task EndWalkAsync() => SafeExecuteAsync(async () =>
    {
        await _gpsTracker.StopAsync();
        IsTracking = false;
        if (ActiveBookingId != Guid.Empty)
        {
            await _api.UpdateBookingStatusAsync(ActiveBookingId, new UpdateBookingStatusRequest(Core.Enums.BookingStatus.Completed));
        }
    });

    [RelayCommand]
    private Task CapturePhotoAsync() => SafeExecuteAsync(async () =>
    {
        if (ActiveBookingId == Guid.Empty)
        {
            return;
        }

        var file = await _mediaCapture.CapturePhotoAsync();
        if (file is null)
        {
            return;
        }

        var request = new MediaUploadRequest(file.FullPath, null, "photo", "Having a blast!");
        await _api.UploadMediaAsync(ActiveBookingId, request);
    });
}
