using DogWalker.Core.Enums;

namespace DogWalker.Core.Models;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public string StripePaymentIntentId { get; set; } = string.Empty;
    public string StripeCustomerId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public double Amount { get; set; }
    public double TipAmount { get; set; }
    public string Currency { get; set; } = "usd";
    public DateTimeOffset? CapturedAtUtc { get; set; }
    public DateTimeOffset? RefundedAtUtc { get; set; }
    public string ReceiptUrl { get; set; } = string.Empty;
}
