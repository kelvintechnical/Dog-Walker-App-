using System.Security.Claims;
using DogWalker.Core.Models;
using DogWalker.Core.Enums;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Security;

public sealed class AccountAccess(DogWalkerDbContext db, IHttpContextAccessor http)
{
    public static (string Issuer, string Subject) Identity(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            throw new ResourceAccessException(StatusCodes.Status401Unauthorized);
        var issuer = principal.FindFirstValue("iss");
        var subject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(subject)
            || issuer.Length > 256 || subject.Length > 256)
            throw new ResourceAccessException(StatusCodes.Status401Unauthorized);
        return (issuer, subject);
    }

    public (string Issuer, string Subject) CurrentIdentity()
        => Identity(http.HttpContext?.User ?? new ClaimsPrincipal());

    public async Task<Account?> FindAsync(CancellationToken ct = default, ClaimsPrincipal? principal = null)
    {
        var (issuer, subject) = Identity(principal ?? http.HttpContext?.User ?? new ClaimsPrincipal());
        var account = await db.Accounts.SingleOrDefaultAsync(a => a.Issuer == issuer && a.Subject == subject, ct);
        // SQL Server comparisons can pad trailing spaces. Never accept a database match
        // unless the complete issuer/subject pair is ordinally equal to the validated token.
        if (account is not null && (!string.Equals(account.Issuer, issuer, StringComparison.Ordinal)
            || !string.Equals(account.Subject, subject, StringComparison.Ordinal)))
            throw new ResourceAccessException(StatusCodes.Status401Unauthorized);
        if (account?.IsDisabled == true) throw new ResourceAccessException(StatusCodes.Status403Forbidden);
        return account;
    }

    public async Task<Account> RequireAsync(CancellationToken ct = default, ClaimsPrincipal? principal = null)
        => await FindAsync(ct, principal) ?? throw new ResourceAccessException(StatusCodes.Status403Forbidden);

    public async Task RequireAdministratorAsync(CancellationToken ct = default)
    {
        if (!(await RequireAsync(ct)).IsAdministrator) throw new ResourceAccessException(StatusCodes.Status403Forbidden);
    }

    // Customers own profiles. Caregivers see only profiles associated with their assigned bookings.
    public async Task RequireClientAsync(Guid clientId, bool write = false, CancellationToken ct = default)
    {
        var actor = await RequireAsync(ct);
        if (actor.IsAdministrator || actor.ClientId == clientId) return;
        if (!write && actor.WalkerId is Guid walkerId && await db.Bookings.AnyAsync(
            b => b.ClientId == clientId && b.WalkerId == walkerId
                && (b.Status == BookingStatus.PendingConfirmation || b.Status == BookingStatus.Confirmed
                    || b.Status == BookingStatus.InProgress), ct)) return;
        throw new ResourceAccessException(StatusCodes.Status404NotFound);
    }

    public async Task RequireWalkerAsync(Guid walkerId, CancellationToken ct = default)
    {
        var actor = await RequireAsync(ct);
        if (!actor.IsAdministrator && actor.WalkerId != walkerId)
            throw new ResourceAccessException(StatusCodes.Status404NotFound);
    }

    public async Task<Booking> RequireBookingAsync(Guid bookingId, bool caregiverOnly = false,
        bool ownerOnly = false, CancellationToken ct = default, ClaimsPrincipal? principal = null)
    {
        var actor = await RequireAsync(ct, principal);
        var booking = await db.Bookings.SingleOrDefaultAsync(b => b.Id == bookingId, ct);
        if (booking is null || !(actor.IsAdministrator
            || (!caregiverOnly && actor.ClientId == booking.ClientId)
            || (!ownerOnly && actor.WalkerId == booking.WalkerId)))
            throw new ResourceAccessException(StatusCodes.Status404NotFound);
        return booking;
    }

    public async Task RequireThreadAsync(Guid clientId, Guid walkerId, CancellationToken ct = default)
    {
        var actor = await RequireAsync(ct);
        if (!actor.IsAdministrator && !(actor.ClientId == clientId || actor.WalkerId == walkerId))
            throw new ResourceAccessException(StatusCodes.Status404NotFound);
        // Neither party can select an arbitrary counterparty to gain access or impersonate a relationship.
        if (!actor.IsAdministrator && !await db.Bookings.AnyAsync(
            b => b.ClientId == clientId && b.WalkerId == walkerId, ct))
            throw new ResourceAccessException(StatusCodes.Status404NotFound);
    }
}
