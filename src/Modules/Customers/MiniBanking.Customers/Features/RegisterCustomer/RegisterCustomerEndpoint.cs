using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.BuildingBlocks.Errors;
using MiniBanking.BuildingBlocks.Modules;
using MiniBanking.Customers.Domain;

namespace MiniBanking.Customers.Features.RegisterCustomer;

internal static class RegisterCustomerEndpoint
{
    /// <summary>The JSON body the client sends.</summary>
    internal sealed record Request(
        string FirstName,
        string LastName,
        string NationalId,
        DateOnly DateOfBirth,
        string Email,
        string PhoneNumber);

    /// <summary>The JSON body we return.</summary>
    internal sealed record Response(Guid Id);

    public static void Map(IEndpointRouteBuilder group) =>
        group.MapPost("/", HandleAsync)
            .WithName("RegisterCustomer")
            .WithSummary("Registers a new retail customer. KYC starts as Pending.")
            .Produces<Response>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> HandleAsync(
        Request request,
        ICommandHandler<RegisterCustomerCommand, CustomerId> handler,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCustomerCommand(
            request.FirstName,
            request.LastName,
            request.NationalId,
            request.DateOfBirth,
            request.Email,
            request.PhoneNumber);

        var result = await handler.Handle(command, cancellationToken);

        // 201 Created + a Location header with the new customer's address (read by GetCustomer, M12).
        return result.Match(customerId => TypedResults.Created(
            $"{ModuleExtensions.ApiPrefix}/customers/{customerId.Value}",
            new Response(customerId.Value)));
    }
}
