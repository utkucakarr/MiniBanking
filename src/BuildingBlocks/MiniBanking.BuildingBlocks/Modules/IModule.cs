using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBanking.BuildingBlocks.Modules;

/// <summary>
/// Entry point of a business module (Customers, Accounts, Ledger, ...).
/// The host only knows modules through this interface (ADR 0001).
/// </summary>
public interface IModule
{
    /// <summary>Short name, also used as the URL segment: /api/v1/{Name}.</summary>
    string Name { get; }

    /// <summary>The assembly containing the module's handlers and validators.</summary>
    Assembly Assembly { get; }

    /// <summary>Registers the module's own services (DbContext, options, ...).</summary>
    void Register(IServiceCollection services, IConfiguration configuration);

    /// <summary>Maps the module's endpoints onto its route group (already prefixed with /api/v1/{Name}).</summary>
    void MapEndpoints(IEndpointRouteBuilder group);
}
