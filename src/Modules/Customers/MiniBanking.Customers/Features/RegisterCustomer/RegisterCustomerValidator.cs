using FluentValidation;
using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Features.RegisterCustomer;

/// <summary>
/// Shape only: required fields and lengths (ADR 0005).
/// Formats and checksums are checked by the value objects; the age rule by the Customer aggregate.
/// </summary>
internal sealed class RegisterCustomerValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerValidator()
    {
        RuleFor(command => command.FirstName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(command => command.LastName).NotEmpty().MaximumLength(PersonName.MaxLength);
        RuleFor(command => command.NationalId).NotEmpty();
        RuleFor(command => command.DateOfBirth).NotEmpty();
        RuleFor(command => command.Email).NotEmpty().MaximumLength(Email.MaxLength);
        RuleFor(command => command.PhoneNumber).NotEmpty();
    }
}
