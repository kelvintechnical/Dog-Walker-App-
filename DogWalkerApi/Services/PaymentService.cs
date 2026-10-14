using AutoMapper;
using DogWalkerApi.Security;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Models;
using DogWalkerApi.Data;
using DogWalkerApi.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

namespace DogWalkerApi.Services;

public class PaymentService : IPaymentService
{
    private readonly AccountAccess _access;
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;
    private readonly PaymentIntentService _paymentIntents;
    private readonly StripeOptions _options;

    public PaymentService(
        DogWalkerDbContext db,
        IMapper mapper,
        IOptions<StripeOptions> options, AccountAccess access)
    {
        _db = db;
        _access = access;
        _mapper = mapper;
        _options = options.Value;

        StripeConfiguration.ApiKey = _options.SecretKey;
        _paymentIntents = new PaymentIntentService();
    }

    public async Task<PaymentDto> CreateOrUpdateIntentAsync(Guid bookingId, double amount, double tip, CancellationToken cancellationToken = default)
    {
        await _access.RequireBookingAsync(bookingId, ownerOnly: true, ct: cancellationToken);
        var booking = await _db.Bookings.Include(b => b.Payment).FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
        if (booking is null)
        {
            throw new KeyNotFoundException($"Booking {bookingId} not found");
        }

        if (!double.IsFinite(amount) || !double.IsFinite(tip) || tip < 0 || amount != booking.Price)
            throw new ResourceAccessException(StatusCodes.Status400BadRequest);
        var totalAmount = (decimal)(booking.Price + tip);
        var totalCents = Convert.ToInt64(totalAmount * 100);

        PaymentIntent intent;

        if (booking.Payment is not null && !string.IsNullOrWhiteSpace(booking.Payment.StripePaymentIntentId))
        {
            intent = await _paymentIntents.UpdateAsync(booking.Payment.StripePaymentIntentId, new PaymentIntentUpdateOptions
            {
                Amount = totalCents
            }, cancellationToken: cancellationToken);
        }
        else
        {
            intent = await _paymentIntents.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = totalCents,
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true }
            }, cancellationToken: cancellationToken);

            booking.Payment = new Payment
            {
                BookingId = booking.Id,
                StripePaymentIntentId = intent.Id,
                Amount = amount,
                TipAmount = tip,
                Status = PaymentStatus.Pending
            };
            _db.Payments.Add(booking.Payment);
        }

        booking.Payment.Amount = amount;
        booking.Payment.TipAmount = tip;
        booking.Payment.Status = PaymentStatus.Pending;

        await _db.SaveChangesAsync(cancellationToken);
        return _mapper.Map<PaymentDto>(booking.Payment);
    }

    public async Task<PaymentDto> CaptureAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        await _access.RequireBookingAsync(bookingId, caregiverOnly: true, ct: cancellationToken);
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken);
        if (payment is null)
        {
            throw new KeyNotFoundException($"Payment for booking {bookingId} not found");
        }

        var intent = await _paymentIntents.CaptureAsync(payment.StripePaymentIntentId, cancellationToken: cancellationToken);
        payment.Status = PaymentStatus.Captured;
        payment.CapturedAtUtc = DateTimeOffset.UtcNow;
        payment.ReceiptUrl = intent.LatestChargeId != null ? intent.LatestChargeId : payment.ReceiptUrl;

        await _db.SaveChangesAsync(cancellationToken);
        return _mapper.Map<PaymentDto>(payment);
    }

    public async Task<PaymentDto> RefundAsync(Guid bookingId, double amount, CancellationToken cancellationToken = default)
    {
        await _access.RequireAdministratorAsync(cancellationToken);
        await _access.RequireBookingAsync(bookingId, caregiverOnly: true, ct: cancellationToken);
        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, cancellationToken);
        if (payment is null)
        {
            throw new KeyNotFoundException($"Payment for booking {bookingId} not found");
        }

        var refundService = new RefundService();
        await refundService.CreateAsync(new RefundCreateOptions
        {
            PaymentIntent = payment.StripePaymentIntentId,
            Amount = Convert.ToInt64(amount * 100)
        }, cancellationToken: cancellationToken);

        payment.Status = PaymentStatus.Refunded;
        payment.RefundedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return _mapper.Map<PaymentDto>(payment);
    }
}
