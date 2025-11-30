using DogWalker.Core.Enums;

namespace DogWalker.Core.Models;

public class Message : BaseEntity
{
    public Guid ThreadId { get; set; }
    public Guid SenderId { get; set; }
    public string Body { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset SentAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string DeliveryStatus { get; set; } = "sent";
}
