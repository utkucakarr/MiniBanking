using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Features.RegisterCustomer;

/// <summary>Registers a new retail customer. Returns the new customer's id.</summary>
internal sealed record RegisterCustomerCommand(
    string FirstName,
    string LastName,
    string NationalId,
    DateOnly DateOfBirth,
    string Email,
    string PhoneNumber) : ICommand<CustomerId>;
