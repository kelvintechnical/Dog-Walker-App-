using DogWalker.Core.Models;
using Microsoft.AspNetCore.SignalR;

namespace DogWalkerApi.Hubs;

public class WalkHub : Hub
{
    public async Task JoinBookingGroup(Guid bookingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, bookingId.ToString());
    }

    public async Task SendRoutePoint(Guid bookingId, WalkRoutePoint point)
    {
        await Clients.Group(bookingId.ToString()).SendAsync("RoutePointUpdated", point);
    }

    public async Task SendStatus(Guid bookingId, string status)
    {
        await Clients.Group(bookingId.ToString()).SendAsync("StatusChanged", status);
    }
}
