using DogWalker.Core.Enums;

namespace DogWalker.Core.Models;

public class Client : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Address Address { get; set; } = new();
    public List<Dog> Dogs { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
    public List<MessageThread> MessageThreads { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTimeOffset LastBookingUtc { get; set; }
    public double LifetimeSpend { get; set; }
    public EnvironmentTier PreferredEnvironment { get; set; } = EnvironmentTier.Production;
}
