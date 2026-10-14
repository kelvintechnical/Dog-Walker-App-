using DogWalker.Core.Models;
using DogWalkerApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DogWalkerApi.Hubs;

[Authorize]
public class WalkHub(AccountAccess access, BookingBroadcaster broadcaster) : Hub
{
    public async Task JoinBookingGroup(Guid bookingId)
    {
        await RequireAccess(bookingId);
        await Groups.AddToGroupAsync(Context.ConnectionId, bookingId.ToString());
    }

    public async Task SendRoutePoint(Guid bookingId, WalkRoutePoint point)
    {
        await RequireAccess(bookingId, caregiverOnly: true);
        if (point.BookingId != bookingId) throw new HubException("Resource unavailable.");
        await broadcaster.SendAsync(bookingId, "RoutePointUpdated", point);
    }

    public async Task SendStatus(Guid bookingId, string status)
    {
        // Status changes must use the persisted, authorized REST transition, not arbitrary hub strings.
        var booking = await RequireAccess(bookingId, caregiverOnly: true);
        if (status != booking.Status.ToString()) throw new HubException("Resource unavailable.");
        await broadcaster.SendAsync(bookingId, "StatusChanged", status);
    }

    private async Task<Booking> RequireAccess(Guid bookingId, bool caregiverOnly = false)
    {
        try { return await access.RequireBookingAsync(bookingId, caregiverOnly, ct: Context.ConnectionAborted, principal: Context.User); }
        catch (ResourceAccessException) { throw new HubException("Resource unavailable."); }
    }
}
