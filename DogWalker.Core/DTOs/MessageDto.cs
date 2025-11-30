using DogWalker.Core.Enums;

namespace DogWalker.Core.DTOs;

public record MessageDto(
    Guid Id,
    Guid ThreadId,
    MessageDirection Direction,
    string Body,
    DateTimeOffset SentAtUtc,
    bool IsRead
);
