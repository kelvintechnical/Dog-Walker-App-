using DogWalkerApi.Data;
using DogWalkerApi.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Security;

// Resolve current participants at delivery time. A stale group membership does not grant data access.
public sealed class BookingBroadcaster(DogWalkerDbContext db, IHubContext<WalkHub> hub)
{
    public async Task SendAsync(Guid bookingId, string method, object payload, CancellationToken ct = default)
    {
        var booking = await db.Bookings.AsNoTracking().SingleOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null) return;
        var recipients = await db.Accounts.AsNoTracking().Where(a => !a.IsDisabled
            && (a.ClientId == booking.ClientId || a.WalkerId == booking.WalkerId))
            .Select(a => new { a.Issuer, a.Subject }).ToListAsync(ct);
        await hub.Clients.Users(recipients.Select(a => AccountUserIdProvider.Key(a.Issuer, a.Subject)).ToArray())
            .SendAsync(method, payload, ct);
    }
}
