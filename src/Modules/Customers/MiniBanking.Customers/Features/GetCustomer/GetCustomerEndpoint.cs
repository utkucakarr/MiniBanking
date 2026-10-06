using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.BuildingBlocks.Errors;

namespace MiniBanking.Customers.Features.GetCustomer;

internal static class GetCustomerEndpoint
{
    public static void Map(IEndpointRouteBuilder group) =>
        group.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetCustomer")
            .WithSummary("Returns a customer by id.")
            .Produces<CustomerResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> HandleAsync(
        Guid id,
        IQueryHandler<GetCustomerQuery, CustomerResponse> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetCustomerQuery(id), cancellationToken);

        return result.Match(customer => TypedResults.Ok(customer));
    }
}
