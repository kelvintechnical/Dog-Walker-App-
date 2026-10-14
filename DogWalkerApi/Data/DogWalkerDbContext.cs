using DogWalker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Data;

public class DogWalkerDbContext : DbContext
{
    public DogWalkerDbContext(DbContextOptions<DogWalkerDbContext> options) : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
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

        builder.Entity<Account>(account =>
        {
            account.Property(a => a.Issuer).HasMaxLength(256).IsRequired();
            account.Property(a => a.Subject).HasMaxLength(256).IsRequired();
            account.HasIndex(a => new { a.Issuer, a.Subject }).IsUnique();
            account.HasIndex(a => a.ClientId).IsUnique().HasFilter("[ClientId] IS NOT NULL");
            account.HasIndex(a => a.WalkerId).IsUnique().HasFilter("[WalkerId] IS NOT NULL");
            account.HasOne(a => a.Client).WithOne().HasForeignKey<Account>(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
            account.HasOne(a => a.Walker).WithOne().HasForeignKey<Account>(a => a.WalkerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        if (Database.IsSqlServer())
        {
            // Subject identifiers are case-sensitive even on a case-insensitive SQL Server database.
            builder.Entity<Account>().Property(a => a.Issuer).UseCollation("Latin1_General_100_BIN2");
            builder.Entity<Account>().Property(a => a.Subject).UseCollation("Latin1_General_100_BIN2");
        }

        // Client -> booking -> messages and client -> thread -> messages otherwise create
        // multiple cascade paths on SQL Server. Retain history instead of cascading through bookings.
        builder.Entity<Booking>().HasMany(b => b.Messages).WithOne()
            .HasForeignKey("BookingId").OnDelete(DeleteBehavior.Restrict);

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
