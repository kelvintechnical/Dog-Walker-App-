using AutoMapper;
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
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public BookingService(DogWalkerDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
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
        var bookings = await _db.Bookings
            .Where(b => b.WalkerId == walkerId && b.StartTimeUtc >= DateTimeOffset.UtcNow.AddHours(-2))
            .OrderBy(b => b.StartTimeUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<BookingDto>>(bookings);
    }

    public async Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.RoutePoints)
            .Include(b => b.Media)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        return booking is null ? null : _mapper.Map<BookingDto>(booking);
    }

    public async Task<BookingDto> UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            throw new KeyNotFoundException($"Booking {bookingId} not found");
        }

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
