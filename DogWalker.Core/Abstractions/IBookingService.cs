using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Requests;

namespace DogWalker.Core.Abstractions;

public interface IBookingService
{
    Task<BookingDto> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<BookingDto>> GetUpcomingAsync(Guid walkerId, CancellationToken cancellationToken = default);
    Task<BookingDto?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<BookingDto> UpdateStatusAsync(Guid bookingId, BookingStatus status, CancellationToken cancellationToken = default);
}
