using DogWalker.Core.Enums;

namespace DogWalker.Core.Requests;

public record SendMessageRequest(
    Guid ClientId,
    Guid WalkerId,
    Guid? BookingId,
    MessageDirection Direction,
    string Message
);
