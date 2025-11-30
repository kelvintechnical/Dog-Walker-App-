using DogWalker.Core.Enums;

namespace DogWalker.Core.DTOs;

public record BookingDto(
    Guid Id,
    Guid ClientId,
    Guid WalkerId,
    Guid DogId,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset EndTimeUtc,
    ServiceType ServiceType,
    BookingStatus Status,
    double Price,
    double TipAmount,
    double DistanceMiles
);
