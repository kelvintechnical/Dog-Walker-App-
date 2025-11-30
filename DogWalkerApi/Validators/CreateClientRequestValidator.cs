using DogWalker.Core.Requests;
using FluentValidation;

namespace DogWalkerApi.Validators;

public class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(r => r.FullName).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Email).NotEmpty().EmailAddress();
        RuleFor(r => r.PhoneNumber).NotEmpty();
        RuleFor(r => r.Line1).NotEmpty();
        RuleFor(r => r.City).NotEmpty();
        RuleFor(r => r.State).NotEmpty();
        RuleFor(r => r.PostalCode).NotEmpty();
    }
}
