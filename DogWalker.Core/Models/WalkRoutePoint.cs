namespace DogWalker.Core.Models;

public class WalkRoutePoint : BaseEntity
{
    public Guid BookingId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public double TotalDistanceMeters { get; set; }
    public double? SpeedMph { get; set; }
    public string Source { get; set; } = "device";
}
