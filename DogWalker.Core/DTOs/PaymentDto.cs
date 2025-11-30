using DogWalker.Core.Enums;

namespace DogWalker.Core.DTOs;

public record PaymentDto(
    Guid Id,
    Guid BookingId,
    double Amount,
    double TipAmount,
    PaymentStatus Status,
    DateTimeOffset? CapturedAtUtc
);
