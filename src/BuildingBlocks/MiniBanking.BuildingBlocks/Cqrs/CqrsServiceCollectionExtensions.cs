using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBanking.BuildingBlocks.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    /// <summary>
    /// Registers every command/query handler and validator found in <paramref name="assemblies"/>
    /// and wraps the handlers in decorators: Logging → Validation → Transaction (commands only) → Handler.
    /// </summary>
    public static IServiceCollection AddCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableToAny(
                    typeof(ICommandHandler<>),
                    typeof(ICommandHandler<,>),
                    typeof(IQueryHandler<,>)),
                publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddValidatorsFromAssemblies(assemblies, includeInternalTypes: true);

        // Decorators wrap what is already registered: the LAST one registered is the OUTERMOST.
        // Queries don't change state, so they get no transaction.
        services.TryDecorate(typeof(ICommandHandler<>), typeof(TransactionDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(TransactionDecorator.CommandHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(ValidationDecorator.QueryHandler<,>));

        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandHandler<>));
        services.TryDecorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));

        return services;
    }
}
