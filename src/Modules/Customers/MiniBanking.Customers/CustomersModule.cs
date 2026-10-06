using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Modules;
using MiniBanking.BuildingBlocks.Persistence;
using MiniBanking.Customers.Features.GetCustomer;
using MiniBanking.Customers.Features.RegisterCustomer;
using MiniBanking.Customers.Infrastructure;

namespace MiniBanking.Customers;

/// <summary>
/// Customers module: customer profiles and KYC status.
/// The only public type of the module, so the host can plug it in.
/// </summary>
public sealed class CustomersModule : IModule
{
    public string Name => "customers";

    public Assembly Assembly => typeof(CustomersModule).Assembly;

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddModuleDbContext<CustomersDbContext>(CustomersDbContext.Schema);
    }

    public void MapEndpoints(IEndpointRouteBuilder group)
    {
        // One line per slice, added together with the slice.
        RegisterCustomerEndpoint.Map(group);
        GetCustomerEndpoint.Map(group);
    }
}
