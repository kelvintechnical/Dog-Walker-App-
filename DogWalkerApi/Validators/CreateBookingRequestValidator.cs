using DogWalker.Core.Requests;
using FluentValidation;

namespace DogWalkerApi.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(r => r.ClientId).NotEmpty();
        RuleFor(r => r.WalkerId).NotEmpty();
        RuleFor(r => r.DogId).NotEmpty();
        RuleFor(r => r.EndTimeUtc)
            .GreaterThan(r => r.StartTimeUtc)
            .WithMessage("End time must be after start time.");
        RuleFor(r => r.StartTimeUtc)
            .GreaterThan(DateTimeOffset.UtcNow.AddMinutes(-5))
            .WithMessage("Start time cannot be in the past.");
    }
}
