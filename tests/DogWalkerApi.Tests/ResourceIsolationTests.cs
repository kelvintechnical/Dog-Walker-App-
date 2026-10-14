using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApi.Controllers;
using DogWalkerApi.Data;
using DogWalkerApi.Hubs;
using DogWalkerApi.Security;
using DogWalkerApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DogWalkerApi.Tests;

public sealed class ResourceIsolationTests : IAsyncLifetime
{
    private readonly ApiFactory api = new();
    public Task InitializeAsync() => api.SeedAsync();
    public Task DisposeAsync() { api.Dispose(); return Task.CompletedTask; }

    [Theory]
    [InlineData("/api/clients")]
    [InlineData("/api/dogs")]
    [InlineData("/api/accounts/me")]
    public async Task Anonymous_reads_require_authentication(string path)
    { using var client = api.Client(null); Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode); }

    [Theory]
    [InlineData("issuer")][InlineData("audience")][InlineData("expired")][InlineData("signature")][InlineData("subject")]
    public async Task Invalid_real_jwt_is_rejected(string invalid)
    {
        using var client = api.Client();
        client.DefaultRequestHeaders.Authorization = new("Bearer", api.Token(
            issuer: invalid == "issuer" ? "https://attacker.example.test" : ApiFactory.Issuer,
            audience: invalid == "audience" ? "another-api" : ApiFactory.Audience,
            expired: invalid == "expired", wrongKey: invalid == "signature", omitSubject: invalid == "subject"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/clients")).StatusCode);
    }

    [Fact]
    public async Task Query_tokens_do_not_authenticate_rest()
    { using var c = api.Client(null); Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/clients?access_token=" + api.Token())).StatusCode); }

    [Fact]
    public async Task Disabled_account_is_rejected()
    { using var c = api.Client("disabled"); Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/clients")).StatusCode); }

    [Fact]
    public async Task Unknown_identity_cannot_read_bound_resources()
    { using var c = api.Client("new-user"); Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/dogs")).StatusCode); }

    [Fact]
    public async Task Owner_lists_are_isolated_and_existing_reads_work()
    {
        using var c = api.Client();
        var clients = await c.GetFromJsonAsync<List<ClientDto>>("/api/clients");
        Assert.Equal(api.OwnerA, Assert.Single(clients!).Id);
        var dogs = await c.GetFromJsonAsync<List<DogDto>>("/api/dogs");
        Assert.Equal(2, dogs!.Count); Assert.All(dogs, d => Assert.Equal(api.OwnerA, d.ClientId));
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/bookings/{api.BookingA}")).StatusCode);
    }

    [Theory]
    [InlineData("client")][InlineData("dogs")][InlineData("booking")][InlineData("walker")][InlineData("messages")]
    public async Task Cross_account_reads_return_not_found(string resource)
    {
        using var c = api.Client();
        var path = resource switch {
            "client" => $"/api/clients/{api.OwnerB}", "dogs" => $"/api/dogs/client/{api.OwnerB}",
            "booking" => $"/api/bookings/{api.BookingB}", "walker" => $"/api/bookings/walker/{api.OtherWalker}",
            _ => $"/api/messages/{api.OwnerB}/{api.OtherWalker}" };
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Caregiver_reads_only_assigned_clients_and_booked_pets()
    {
        using var c = api.Client("walker");
        Assert.Equal(api.OwnerA, Assert.Single((await c.GetFromJsonAsync<List<ClientDto>>("/api/clients"))!).Id);
        Assert.Equal(api.DogA, Assert.Single((await c.GetFromJsonAsync<List<DogDto>>("/api/dogs"))!).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/bookings/{api.BookingB}")).StatusCode);
    }

    [Fact]
    public async Task Customer_cannot_claim_an_existing_profile_or_grant_admin_through_claims()
    {
        using var c = api.Client();
        c.DefaultRequestHeaders.Authorization = new("Bearer", api.Token(extras: new[] {
            new Claim("role", "Administrator"), new Claim("ClientId", api.OwnerB.ToString()) }));
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/clients/{api.OwnerB}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/accounts/bindings",
            new BindAccountRequest(ApiFactory.Issuer, "owner-a", api.OwnerB, null))).StatusCode);
    }

    private CreateBookingRequest Request(Guid client, Guid dog, Guid? walker = null) => new(client,
        walker ?? api.Walker, dog, ServiceType.ThirtyMinuteWalk, DateTimeOffset.UtcNow.AddHours(3), DateTimeOffset.UtcNow.AddHours(3.5), "Test");

    [Fact]
    public async Task Forged_owner_or_foreign_pet_booking_does_not_persist()
    {
        using var c = api.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync("/api/bookings", Request(api.OwnerB, api.DogB))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync("/api/bookings", Request(api.OwnerA, api.DogB))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync("/api/bookings", Request(api.OwnerA, api.DogA, Guid.NewGuid()))).StatusCode);
        using var scope = api.Services.CreateScope();
        Assert.Equal(2, await scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>().Bookings.CountAsync());
    }

    [Fact]
    public async Task Valid_booking_and_add_dog_preserve_contracts()
    {
        using var c = api.Client();
        var created = await c.PostAsJsonAsync("/api/bookings", Request(api.OwnerA, api.DogA));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(25, (await created.Content.ReadFromJsonAsync<BookingDto>())!.Price);
        var dog = new CreateDogRequest("New dog", "Mixed", null, 5, null, false, false);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"/api/clients/{api.OwnerB}/dogs", dog)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await c.PostAsJsonAsync($"/api/clients/{api.OwnerA}/dogs", dog)).StatusCode);
    }

    [Fact]
    public async Task New_identity_creates_its_own_profile_once()
    {
        using var c = api.Client("new-owner");
        var request = new CreateClientRequest("New owner", "new@example.test", "123456", "1 Road", null, "City", "CA", "12345", "US");
        var response = await c.PostAsJsonAsync("/api/clients", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var profile = await c.GetFromJsonAsync<AccountProfileDto>("/api/accounts/me");
        Assert.Equal((await response.Content.ReadFromJsonAsync<ClientDto>())!.Id, profile!.ClientId);
        Assert.False(profile.IsAdministrator);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/clients", request)).StatusCode);
    }

    [Theory]
    [InlineData("route")][InlineData("media")][InlineData("payments")][InlineData("status")]
    public async Task Cross_account_writes_are_denied_before_side_effects(string kind)
    {
        using var c = api.Client();
        HttpResponseMessage response = kind switch {
            "route" => await c.PostAsJsonAsync($"/api/bookings/{api.BookingB}/route", new RoutePointRequest(1, 2, 0, null)),
            "media" => await c.PostAsJsonAsync($"/api/bookings/{api.BookingB}/media", new MediaUploadRequest("https://example.test/photo", null, "photo", null)),
            "payments" => await c.PostAsJsonAsync($"/api/bookings/{api.BookingB}/payments", new PaymentRequest(25, 0)),
            _ => await c.PatchAsJsonAsync($"/api/bookings/{api.BookingB}/status", new UpdateBookingStatusRequest(BookingStatus.Completed)) };
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, api.Payments.Intents); Assert.Equal(0, api.Payments.Captures);
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
        Assert.Empty(await db.MediaAssets.ToListAsync()); Assert.Empty(await db.RoutePoints.ToListAsync());
        Assert.Equal(BookingStatus.PendingConfirmation, (await db.Bookings.FindAsync(api.BookingB))!.Status);
    }

    [Fact]
    public async Task Owner_cannot_write_caregiver_route_or_complete_booking()
    {
        using var c = api.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync($"/api/bookings/{api.BookingA}/route", new RoutePointRequest(1, 2, 0, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.PatchAsJsonAsync($"/api/bookings/{api.BookingA}/status", new UpdateBookingStatusRequest(BookingStatus.Completed))).StatusCode);
        Assert.Equal(0, api.Payments.Captures);
    }

    [Fact]
    public async Task Assigned_caregiver_can_confirm_but_cannot_jump_to_completed()
    {
        using var c = api.Client("walker");
        Assert.Equal(HttpStatusCode.Conflict, (await c.PatchAsJsonAsync($"/api/bookings/{api.BookingA}/status", new UpdateBookingStatusRequest(BookingStatus.Completed))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PatchAsJsonAsync($"/api/bookings/{api.BookingA}/status", new UpdateBookingStatusRequest(BookingStatus.Confirmed))).StatusCode);
    }

    [Fact]
    public async Task Message_direction_and_counterparty_cannot_be_forged()
    {
        using var c = api.Client();
        Assert.Equal(HttpStatusCode.NotFound, (await c.PostAsJsonAsync("/api/messages", new SendMessageRequest(api.OwnerB, api.OtherWalker, null, MessageDirection.ClientToWalker, "Attack"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsJsonAsync("/api/messages", new SendMessageRequest(api.OwnerA, api.Walker, null, MessageDirection.WalkerToClient, "Impersonation"))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/messages", new SendMessageRequest(api.OwnerA, api.Walker, api.BookingA, MessageDirection.ClientToWalker, "Hello"))).StatusCode);
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
        var message = Assert.Single(await db.Messages.ToListAsync());
        Assert.Equal((await db.Accounts.SingleAsync(a => a.Subject == "owner-a")).Id, message.SenderId);
    }

    [Fact]
    public async Task Admin_binding_is_explicit_unique_and_cannot_overwrite_existing_owner()
    {
        using var c = api.Client("admin");
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/accounts/bindings", new BindAccountRequest(ApiFactory.Issuer, "owner-a", api.OwnerB, null))).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/accounts/bindings", new BindAccountRequest(ApiFactory.Issuer, "new-sub", api.OwnerA, null))).StatusCode);
    }

    [Fact]
    public async Task Anonymous_hub_negotiation_is_denied()
    { using var c = api.Client(null); Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsync("/hubs/walks/negotiate?negotiateVersion=1", null)).StatusCode); }

    [Fact]
    public async Task Actual_signalr_connection_enforces_booking_membership_and_caregiver_writes()
    {
        await using var hub = new HubConnectionBuilder().WithUrl("https://localhost/hubs/walks", options => {
            options.AccessTokenProvider = () => Task.FromResult<string?>(api.Token());
            options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
        }).Build();
        await hub.StartAsync();
        await hub.InvokeAsync("JoinBookingGroup", api.BookingA);
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("JoinBookingGroup", api.BookingB));
        await Assert.ThrowsAsync<HubException>(() => hub.InvokeAsync("SendStatus", api.BookingA, "Completed"));
    }

    [Fact]
    public async Task Real_payment_service_rejects_foreign_booking_and_tampered_amount_without_network()
    {
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim("iss", ApiFactory.Issuer), new Claim("sub", "owner-a") }, "test-principal")) } };
        var access = new AccountAccess(db, http);
        var mapper = scope.ServiceProvider.GetRequiredService<AutoMapper.IMapper>();
        var payment = new PaymentService(db, mapper, Microsoft.Extensions.Options.Options.Create(new DogWalkerApi.Options.StripeOptions()), access);
        var foreign = await Assert.ThrowsAsync<ResourceAccessException>(() => payment.CreateOrUpdateIntentAsync(api.BookingB, 25, 0));
        Assert.Equal(404, foreign.StatusCode);
        var tamper = await Assert.ThrowsAsync<ResourceAccessException>(() => payment.CreateOrUpdateIntentAsync(api.BookingA, 1, 0));
        Assert.Equal(400, tamper.StatusCode);
    }

    [Fact]
    public async Task Owner_history_is_scoped_and_role_checks_cannot_be_bypassed()
    {
        using var c = api.Client();
        Assert.Equal(api.BookingA, Assert.Single((await c.GetFromJsonAsync<List<BookingDto>>($"/api/bookings/client/{api.OwnerA}"))!).Id);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/bookings/client/{api.OwnerB}")).StatusCode);
        using var caregiver = api.Client("walker");
        Assert.Equal(HttpStatusCode.NotFound, (await caregiver.GetAsync($"/api/bookings/client/{api.OwnerA}")).StatusCode);
    }

    [Fact]
    public async Task Disabled_connected_caregiver_cannot_keep_sending_hub_events()
    {
        await using var hub = new HubConnectionBuilder().WithUrl("https://localhost/hubs/walks", options => {
            options.AccessTokenProvider = () => Task.FromResult<string?>(api.Token("walker"));
            options.HttpMessageHandlerFactory = _ => api.Server.CreateHandler();
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
        }).Build();
        await hub.StartAsync();
        await hub.InvokeAsync("JoinBookingGroup", api.BookingA);
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
            var account = await db.Accounts.SingleAsync(a => a.Subject == "walker");
            account.IsDisabled = true; await db.SaveChangesAsync();
        }
        var exception = await Record.ExceptionAsync(() => hub.InvokeAsync("SendStatus", api.BookingA, "PendingConfirmation"));
        Assert.True(exception is HubException
            || exception is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized },
            "A revoked account must fail either hub authorization or transport authentication.");
    }

    [Fact]
    public async Task Caregiver_does_not_read_unassigned_pets_or_profiles_after_cancellation()
    {
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
            var booking = await db.Bookings.FindAsync(api.BookingA);
            booking!.Status = BookingStatus.Cancelled; await db.SaveChangesAsync();
        }
        using var c = api.Client("walker");
        Assert.Empty((await c.GetFromJsonAsync<List<DogDto>>("/api/dogs"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/clients/{api.OwnerA}")).StatusCode);
    }

    [Fact]
    public async Task Admin_can_bind_a_verified_unclaimed_legacy_profile()
    {
        var legacyId = Guid.NewGuid();
        using (var scope = api.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
            db.Clients.Add(new Client { Id = legacyId, FullName = "Legacy" }); await db.SaveChangesAsync();
        }
        using var admin = api.Client("admin");
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync("/api/accounts/bindings",
            new BindAccountRequest(ApiFactory.Issuer, "verified-legacy-subject", legacyId, null))).StatusCode);
        using var owner = api.Client("verified-legacy-subject");
        Assert.Equal(legacyId, (await owner.GetFromJsonAsync<AccountProfileDto>("/api/accounts/me"))!.ClientId);
        Assert.Equal(HttpStatusCode.NotFound, (await owner.GetAsync($"/api/clients/{api.OwnerA}")).StatusCode);
    }

    [Fact]
    public async Task Authorized_route_and_media_writes_persist_without_cross_account_access()
    {
        using var c = api.Client("walker");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/bookings/{api.BookingA}/route", new RoutePointRequest(1, 2, 0, null))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync($"/api/bookings/{api.BookingA}/media", new MediaUploadRequest("https://example.test/photo", null, "photo", null))).StatusCode);
        using var scope = api.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DogWalkerDbContext>();
        Assert.Equal(api.BookingA, Assert.Single(await db.RoutePoints.ToListAsync()).BookingId);
        Assert.Equal(api.BookingA, Assert.Single(await db.MediaAssets.ToListAsync()).BookingId);
    }
}
