using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Requests;
using Refit;

namespace DogWalkerApp.Services.Api;

public interface IDogWalkerApi
{
    [Get("/api/clients")]
    Task<IEnumerable<ClientDto>> GetClientsAsync();

    [Get("/api/clients/{id}")]
    Task<ClientDto> GetClientAsync(Guid id);

    [Post("/api/clients")]
    Task<ClientDto> CreateClientAsync([Body] CreateClientRequest request);

    [Post("/api/clients/{clientId}/dogs")]
    Task<DogDto> AddDogAsync(Guid clientId, [Body] CreateDogRequest request);

    [Get("/api/bookings/walker/{walkerId}")]
    Task<IEnumerable<BookingDto>> GetWalkerBookingsAsync(Guid walkerId);

    [Post("/api/bookings")]
    Task<BookingDto> CreateBookingAsync([Body] CreateBookingRequest request);

    [Patch("/api/bookings/{bookingId}/status")]
    Task<BookingDto> UpdateBookingStatusAsync(Guid bookingId, [Body] UpdateBookingStatusRequest request);

    [Post("/api/bookings/{bookingId}/route")]
    Task AppendRoutePointAsync(Guid bookingId, [Body] RoutePointRequest request);

    [Post("/api/bookings/{bookingId}/payments")]
    Task<PaymentDto> CreatePaymentIntentAsync(Guid bookingId, [Body] CreatePaymentIntent request);

    [Post("/api/bookings/{bookingId}/media")]
    Task UploadMediaAsync(Guid bookingId, [Body] MediaUploadRequest request);

    [Get("/api/messages/{clientId}/{walkerId}")]
    Task<IEnumerable<MessageDto>> GetMessagesAsync(Guid clientId, Guid walkerId);

    [Post("/api/messages")]
    Task<MessageDto> SendMessageAsync([Body] SendMessageRequest request);
}

public record CreatePaymentIntent(double Amount, double TipAmount);
