using DogWalker.Core.Enums;

namespace DogWalker.Core.Models;

public class Booking : BaseEntity
{
    public Guid ClientId { get; set; }
    public Guid WalkerId { get; set; }
    public Guid DogId { get; set; }
    public DateTimeOffset StartTimeUtc { get; set; }
    public DateTimeOffset EndTimeUtc { get; set; }
    public ServiceType ServiceType { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.PendingConfirmation;
    public string Notes { get; set; } = string.Empty;
    public double Price { get; set; }
    public double TipAmount { get; set; }
    public double DistanceMiles { get; set; }
    public Payment? Payment { get; set; }
    public List<WalkRoutePoint> RoutePoints { get; set; } = new();
    public List<MediaAsset> Media { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
    public bool AutoChargeEnabled { get; set; } = true;
}
