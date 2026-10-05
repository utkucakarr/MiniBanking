using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.BuildingBlocks.Modules;
using MiniBanking.BuildingBlocks.Tests.Cqrs;

namespace MiniBanking.BuildingBlocks.Tests.Modules;

public class ModuleTests
{
    private sealed class TestModule : IModule
    {
        public bool Registered { get; private set; }

        public string Name => "tests";

        public Assembly Assembly => typeof(ModuleTests).Assembly;

        public void Register(IServiceCollection services, IConfiguration configuration)
        {
            Registered = true;
            services.AddSingleton<CallTracker>();
        }

        public void MapEndpoints(IEndpointRouteBuilder group) =>
            group.MapGet("/ping", () => "pong");
    }

    [Fact]
    public void AddModules_registers_module_services_and_handlers()
    {
        var module = new TestModule();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddModules(new ConfigurationBuilder().Build(), module);

        module.Registered.Should().BeTrue();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetService<ICommandHandler<OpenTestAccount, Guid>>().Should().NotBeNull();
    }

    [Fact]
    public void Handlers_are_decorated_only_once()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddModules(new ConfigurationBuilder().Build(), new TestModule());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<OpenTestAccount, Guid>>();

        // Logging → Validation → Transaction → Handler: walk the decorator chain via each decorator's "inner" field.
        var chain = new List<string>();
        object? current = handler;
        while (current is not null)
        {
            chain.Add(current.GetType().DeclaringType?.Name ?? current.GetType().Name);
            current = current.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                .FirstOrDefault(field => field.FieldType == typeof(ICommandHandler<OpenTestAccount, Guid>))
                ?.GetValue(current);
        }

        chain.Should().Equal(
            "LoggingDecorator", "ValidationDecorator", "TransactionDecorator", nameof(OpenTestAccountHandler));
    }

    [Fact]
    public void MapModules_maps_each_module_under_its_own_api_prefix()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.Services.AddModules(builder.Configuration, new TestModule());
        var app = builder.Build();

        app.MapModules();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText);

        routes.Should().Contain("/api/v1/tests/ping");
    }
}
