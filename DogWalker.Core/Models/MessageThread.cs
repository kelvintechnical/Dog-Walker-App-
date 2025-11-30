using DogWalker.Core.Enums;

namespace DogWalker.Core.Models;

public class MessageThread : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid WalkerId { get; set; }
    public Guid? BookingId { get; set; }
    public List<Message> Messages { get; set; } = new();
    public DateTimeOffset LastMessageAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsArchived { get; set; }
    public EnvironmentTier TargetEnvironment { get; set; } = EnvironmentTier.Production;
}
