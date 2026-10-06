using Microsoft.EntityFrameworkCore;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.Customers.Domain;
using MiniBanking.Customers.Infrastructure;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Features.RegisterCustomer;

internal sealed class RegisterCustomerHandler(CustomersDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<RegisterCustomerCommand, CustomerId>
{
    public async Task<Result<CustomerId>> Handle(RegisterCustomerCommand command, CancellationToken cancellationToken)
    {
        // 1. Untrusted input → value objects. Each one rejects an invalid value.
        var name = PersonName.Create(command.FirstName, command.LastName);
        if (name.IsFailure)
            return name.Error;

        var nationalId = NationalId.Create(command.NationalId);
        if (nationalId.IsFailure)
            return nationalId.Error;

        var email = Email.Create(command.Email);
        if (email.IsFailure)
            return email.Error;

        var phoneNumber = PhoneNumber.Create(command.PhoneNumber);
        if (phoneNumber.IsFailure)
            return phoneNumber.Error;

        // 2. A rule that needs the database: one customer per national id.
        var alreadyRegistered = await dbContext.Customers
            .AnyAsync(customer => customer.NationalId == nationalId.Value, cancellationToken);
        if (alreadyRegistered)
            return CustomerErrors.NationalIdAlreadyRegistered;

        // 3. The aggregate enforces its own rules (age) and records what happened.
        var customer = Customer.Register(
            name.Value,
            nationalId.Value,
            command.DateOfBirth,
            email.Value,
            phoneNumber.Value,
            timeProvider.GetUtcNow());
        if (customer.IsFailure)
            return customer.Error;

        // 4. Only track it: the transaction decorator calls SaveChanges and commits.
        dbContext.Customers.Add(customer.Value);

        return customer.Value.Id;
    }
}
