using AutoMapper;
using DogWalkerApi.Security;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Services;

public class BookingService : IBookingService
{
    private readonly AccountAccess _access;
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public BookingService(DogWalkerDbContext db, IMapper mapper, AccountAccess access)
    {
        _db = db;
        _access = access;
        _mapper = mapper;
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        await _access.RequireClientAsync(request.ClientId, write: true, ct: cancellationToken);
        if (!await _db.Dogs.AnyAsync(d => d.Id == request.DogId && d.ClientId == request.ClientId, cancellationToken)
            || !await _db.Walkers.AnyAsync(w => w.Id == request.WalkerId, cancellationToken))
            throw new ResourceAccessException(StatusCodes.Status404NotFound);
        if (!Enum.IsDefined(request.ServiceType) || request.EndTimeUtc <= request.StartTimeUtc)
            throw new ResourceAccessException(StatusCodes.Status400BadRequest);
        var booking = new Booking
        {
            ClientId = request.ClientId,
            WalkerId = request.WalkerId,
            DogId = request.DogId,
            ServiceType = request.ServiceType,
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            Notes = request.Notes,
            Status = BookingStatus.PendingConfirmation,
            Price = CalculatePrice(request.ServiceType, request.EndTimeUtc - request.StartTimeUtc)
        };

        await _db.Bookings.AddAsync(booking, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<BookingDto>(booking);
    }

    public async Task<IEnumerable<BookingDto>> GetUpcomingAsync(Guid walkerId, CancellationToken cancellationToken = default)
    {
        await _access.RequireWalkerAsync(walkerId, cancellationToken);
        var bookings = await _db.Bookings
            .Where(b => b.WalkerId == walkerId && b.StartTimeUtc >= DateTimeOffset.UtcNow.AddHours(-2))
            .OrderBy(b => b.StartTimeUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<BookingDto>>(bookings);
    }

    public async Task<IEnumerable<BookingDto>> GetForClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        await _access.RequireClientAsync(clientId, write: true, ct: cancellationToken);
        var bookings = await _db.Bookings.Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.StartTimeUtc).Take(100).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<BookingDto>>(bookings);
    }

    public async Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await _access.RequireBookingAsync(bookingId, ct: cancellationToken);
        var booking = await _db.Bookings
            .Include(b => b.RoutePoints)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        return booking is null ? null : _mapper.Map<BookingDto>(booking);
    }

    public async Task<BookingDto> UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var booking = await _access.RequireBookingAsync(bookingId,
            caregiverOnly: status != BookingStatus.Cancelled, ct: cancellationToken);
        if (!Enum.IsDefined(status)) throw new ResourceAccessException(StatusCodes.Status400BadRequest);
        if (booking is null)
        {
            throw new KeyNotFoundException($"Booking {bookingId} not found");
        }

        var allowed = (booking.Status, status) switch
        {
            (BookingStatus.PendingConfirmation, BookingStatus.Confirmed) => true,
            (BookingStatus.Confirmed, BookingStatus.InProgress) => true,
            (BookingStatus.InProgress, BookingStatus.Completed) => true,
            (BookingStatus.PendingConfirmation or BookingStatus.Confirmed, BookingStatus.Cancelled) => true,
            _ => false
        };
        if (!allowed) throw new ResourceAccessException(StatusCodes.Status409Conflict);
        booking.Status = status;
        booking.UpdatedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return _mapper.Map<BookingDto>(booking);
    }

    private static double CalculatePrice(ServiceType serviceType, TimeSpan duration)
    {
        return serviceType switch
        {
            ServiceType.ThirtyMinuteWalk => 25,
            ServiceType.OneHourWalk => 45,
            ServiceType.DogSitting => Math.Max(65, duration.TotalHours * 28),
            ServiceType.PuppyVisit => 35,
            ServiceType.GroupWalk => 30,
            ServiceType.Boarding => Math.Max(90, duration.TotalDays * 100),
            _ => 25
        };
    }
}
