using DogWalker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Data;

public class DogWalkerDbContext : DbContext
{
    public DogWalkerDbContext(DbContextOptions<DogWalkerDbContext> options) : base(options)
    {
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Dog> Dogs => Set<Dog>();
    public DbSet<Walker> Walkers => Set<Walker>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<MessageThread> MessageThreads => Set<MessageThread>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<WalkRoutePoint> RoutePoints => Set<WalkRoutePoint>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Client>()
            .OwnsOne(c => c.Address);

        builder.Entity<Walker>()
            .OwnsOne(w => w.ServiceAreaCenter);

        builder.Entity<Booking>()
            .HasMany(b => b.RoutePoints)
            .WithOne()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Booking>()
            .HasMany(b => b.Media)
            .WithOne()
            .HasForeignKey(m => m.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MessageThread>()
            .HasMany(t => t.Messages)
            .WithOne()
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
