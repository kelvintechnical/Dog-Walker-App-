using DogWalker.Core.Requests;
using DogWalkerApp.Services.Api;

namespace DogWalkerApp.Services.Gps;

public interface IGpsTrackerService
{
    event EventHandler<RoutePointRequest>? RoutePointCaptured;
    Task StartAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task StopAsync();
}

public class GpsTrackerService : IGpsTrackerService
{
    private readonly IDogWalkerApi _api;
    private CancellationTokenSource? _internalCts;
    private Guid _bookingId;

    public event EventHandler<RoutePointRequest>? RoutePointCaptured;

    public GpsTrackerService(IDogWalkerApi api)
    {
        _api = api;
    }

    public Task StartAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        _bookingId = bookingId;
        _internalCts?.Cancel();
        _internalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _ = TrackAsync(_internalCts.Token);
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        _internalCts?.Cancel();
        return Task.CompletedTask;
    }

    private async Task TrackAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var location = await Geolocation.GetLastKnownLocationAsync();
            if (location is null)
            {
                location = await Geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best));
            }

            if (location is not null && _bookingId != Guid.Empty)
            {
                var request = new RoutePointRequest(
                    location.Latitude,
                    location.Longitude,
                    0,
                    location.Speed);

                await _api.AppendRoutePointAsync(_bookingId, request);
                RoutePointCaptured?.Invoke(this, request);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }
}
