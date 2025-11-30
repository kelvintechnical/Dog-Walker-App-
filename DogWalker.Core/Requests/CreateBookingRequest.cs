using DogWalker.Core.Enums;

namespace DogWalker.Core.Requests;

public record CreateBookingRequest(
    Guid ClientId,
    Guid WalkerId,
    Guid DogId,
    ServiceType ServiceType,
    DateTimeOffset StartTimeUtc,
    DateTimeOffset EndTimeUtc,
    string Notes
);
