using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Cqrs;

namespace MiniBanking.BuildingBlocks.Modules;

public static class ModuleExtensions
{
    public const string ApiPrefix = "/api/v1";

    /// <summary>
    /// Lets every module register its services, then registers the handlers of ALL modules in a single
    /// <see cref="CqrsServiceCollectionExtensions.AddCqrs"/> call.
    /// </summary>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration,
        params IModule[] modules)
    {
        foreach (var module in modules)
            module.Register(services, configuration);

        // One call for all modules: calling AddCqrs once per module would decorate earlier modules' handlers twice.
        services.AddCqrs(modules.Select(module => module.Assembly).Distinct().ToArray());

        services.AddSingleton<IReadOnlyCollection<IModule>>(modules);

        return services;
    }

    /// <summary>Gives every module its own route group: /api/v1/{module.Name}.</summary>
    public static IEndpointRouteBuilder MapModules(this IEndpointRouteBuilder endpoints)
    {
        var modules = endpoints.ServiceProvider.GetRequiredService<IReadOnlyCollection<IModule>>();

        foreach (var module in modules)
        {
            var group = endpoints
                .MapGroup($"{ApiPrefix}/{module.Name}")
                .WithTags(module.Name);

            module.MapEndpoints(group);
        }

        return endpoints;
    }
}
