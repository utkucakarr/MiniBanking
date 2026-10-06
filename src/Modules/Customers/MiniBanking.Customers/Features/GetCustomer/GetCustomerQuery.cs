using MiniBanking.BuildingBlocks.Cqrs;

namespace MiniBanking.Customers.Features.GetCustomer;

internal sealed record GetCustomerQuery(Guid CustomerId) : IQuery<CustomerResponse>;

/// <summary>What the API returns for a customer: plain values, no domain types.</summary>
internal sealed record CustomerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string NationalId,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber,
    string KycStatus,
    DateTimeOffset RegisteredAt);
