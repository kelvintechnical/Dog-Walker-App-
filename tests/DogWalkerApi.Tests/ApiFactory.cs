using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Models;
using DogWalker.Core.Enums;
using DogWalkerApi.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DogWalkerApi.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "https://identity.example.test";
    public const string Audience = "friends-with-fur-api";
    private readonly RSA rsa = RSA.Create(2048);
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public Guid OwnerA { get; } = Guid.NewGuid();
    public Guid OwnerB { get; } = Guid.NewGuid();
    public Guid Walker { get; } = Guid.NewGuid();
    public Guid OtherWalker { get; } = Guid.NewGuid();
    public Guid DogA { get; } = Guid.NewGuid();
    public Guid HiddenDogA { get; } = Guid.NewGuid();
    public Guid DogB { get; } = Guid.NewGuid();
    public Guid BookingA { get; } = Guid.NewGuid();
    public Guid BookingB { get; } = Guid.NewGuid();
    public PaymentSpy Payments { get; } = new();
    public ApiFactory() { connection.Open(); }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Authority"] = Issuer,
            ["Authentication:Audience"] = Audience,
            ["ConnectionStrings:SqlServer"] = ""
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DogWalkerDbContext>();
            services.RemoveAll<DbContextOptions<DogWalkerDbContext>>();
            services.AddDbContext<DogWalkerDbContext>(options => options.UseSqlite(connection));
            services.AddScoped<DogWalkerDbContext>(provider => new SqliteTestDbContext(provider.GetRequiredService<DbContextOptions<DogWalkerDbContext>>()));
            services.RemoveAll<IPaymentService>();
            services.AddSingleton<IPaymentService>(Payments);
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = Issuer;
                options.TokenValidationParameters.ValidAudience = Audience;
                var key = new RsaSecurityKey(rsa) { KeyId = "ephemeral-test-key" };
                var discovery = new OpenIdConnectConfiguration { Issuer = Issuer };
                discovery.SigningKeys.Add(key);
                options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(discovery);
            });
        });
    }

    public string Token(string subject = "owner-a", string issuer = Issuer, string audience = Audience,
        bool expired = false, bool wrongKey = false, bool omitSubject = false, IEnumerable<Claim>? extras = null)
    {
        using var other = wrongKey ? RSA.Create(2048) : null;
        var key = new RsaSecurityKey(other ?? rsa) { KeyId = "ephemeral-test-key" };
        var claims = new List<Claim>();
        if (!omitSubject) claims.Add(new Claim("sub", subject));
        if (extras is not null) claims.AddRange(extras);
        var now = DateTime.UtcNow;
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, audience, claims,
            notBefore: now.AddMinutes(-10), expires: expired ? now.AddMinutes(-5) : now.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256)));
    }

    public HttpClient Client(string? subject = "owner-a")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        if (subject is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", Token(subject));
        return client;
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Clients.AddRange(new Client { Id = OwnerA, FullName = "Owner A", Email = "a@example.test" },
            new Client { Id = OwnerB, FullName = "Owner B", Email = "b@example.test" });
        db.Walkers.AddRange(new Walker { Id = Walker, FullName = "Assigned caregiver" },
            new Walker { Id = OtherWalker, FullName = "Other caregiver" });
        db.Dogs.AddRange(new Dog { Id = DogA, ClientId = OwnerA, Name = "A" },
            new Dog { Id = HiddenDogA, ClientId = OwnerA, Name = "Unassigned A" },
            new Dog { Id = DogB, ClientId = OwnerB, Name = "B" });
        db.Bookings.AddRange(new Booking { Id = BookingA, ClientId = OwnerA, DogId = DogA, WalkerId = Walker,
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(1), EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2), Price = 25 },
            new Booking { Id = BookingB, ClientId = OwnerB, DogId = DogB, WalkerId = OtherWalker,
            StartTimeUtc = DateTimeOffset.UtcNow.AddHours(1), EndTimeUtc = DateTimeOffset.UtcNow.AddHours(2), Price = 25 });
        db.Accounts.AddRange(new Account { Issuer = Issuer, Subject = "owner-a", ClientId = OwnerA },
            new Account { Issuer = Issuer, Subject = "owner-b", ClientId = OwnerB },
            new Account { Issuer = Issuer, Subject = "walker", WalkerId = Walker },
            new Account { Issuer = Issuer, Subject = "other-walker", WalkerId = OtherWalker },
            new Account { Issuer = Issuer, Subject = "admin", IsAdministrator = true },
            new Account { Issuer = Issuer, Subject = "disabled", IsDisabled = true });
        await db.SaveChangesAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { connection.Dispose(); rsa.Dispose(); }
    }
}

public sealed class PaymentSpy : IPaymentService
{
    public int Intents { get; private set; }
    public int Captures { get; private set; }
    public Task<PaymentDto> CreateOrUpdateIntentAsync(Guid bookingId, double amount, double tip, CancellationToken ct = default)
    { Intents++; return Task.FromResult(new PaymentDto(Guid.NewGuid(), bookingId, amount, tip, PaymentStatus.Pending, null)); }
    public Task<PaymentDto> CaptureAsync(Guid bookingId, CancellationToken ct = default)
    { Captures++; return Task.FromResult(new PaymentDto(Guid.NewGuid(), bookingId, 25, 0, PaymentStatus.Captured, DateTimeOffset.UtcNow)); }
    public Task<PaymentDto> RefundAsync(Guid bookingId, double amount, CancellationToken ct = default) => throw new NotSupportedException();
}

// SQLite lacks native DateTimeOffset ordering. Store UTC ticks only in the test provider;
// SQL Server integration exercises the original production mapping unchanged.
internal sealed class SqliteTestDbContext(DbContextOptions<DogWalkerDbContext> options) : DogWalkerDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        var converter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks, value => new DateTimeOffset(value, TimeSpan.Zero));
        foreach (var property in builder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
            if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTimeOffset))
                property.SetValueConverter(converter);
    }
}
