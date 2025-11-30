namespace DogWalker.Core.Models;

public class Walker : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Address ServiceAreaCenter { get; set; } = new();
    public double ServiceRadiusMiles { get; set; } = 5;
    public bool GpsTrackingEnabled { get; set; } = true;
    public string Bio { get; set; } = string.Empty;
    public List<Booking> AssignedBookings { get; set; } = new();
}
