namespace DogWalker.Core.Models;

public class MediaAsset : BaseEntity
{
    public Guid BookingId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "photo"; // photo or video
    public string Caption { get; set; } = string.Empty;
    public DateTimeOffset CapturedUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsSharedWithClient { get; set; } = true;
}
