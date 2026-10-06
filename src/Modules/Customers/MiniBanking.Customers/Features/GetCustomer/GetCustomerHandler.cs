using Microsoft.EntityFrameworkCore;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.Customers.Domain;
using MiniBanking.Customers.Infrastructure;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.Customers.Features.GetCustomer;

/// <summary>A query: reads straight into the response, no aggregate, no change tracking (ADR 0002).</summary>
internal sealed class GetCustomerHandler(CustomersDbContext dbContext)
    : IQueryHandler<GetCustomerQuery, CustomerResponse>
{
    public async Task<Result<CustomerResponse>> Handle(GetCustomerQuery query, CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(query.CustomerId);

        var customer = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == customerId)
            .Select(customer => new CustomerResponse(
                customer.Id.Value,
                customer.Name.FirstName,
                customer.Name.LastName,
                customer.NationalId.Value,
                customer.DateOfBirth,
                customer.Email.Value,
                customer.PhoneNumber.Value,
                customer.KycStatus.ToString(),
                customer.RegisteredAt))
            .SingleOrDefaultAsync(cancellationToken);

        if (customer is null)
            return CustomerErrors.NotFound(query.CustomerId);

        return customer;
    }
}
