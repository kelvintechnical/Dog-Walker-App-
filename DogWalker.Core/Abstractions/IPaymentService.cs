using DogWalker.Core.DTOs;

namespace DogWalker.Core.Abstractions;

public interface IPaymentService
{
    Task<PaymentDto> CreateOrUpdateIntentAsync(Guid bookingId, double amount, double tip, CancellationToken cancellationToken = default);
    Task<PaymentDto> CaptureAsync(Guid bookingId, CancellationToken cancellationToken = default);
    Task<PaymentDto> RefundAsync(Guid bookingId, double amount, CancellationToken cancellationToken = default);
}
