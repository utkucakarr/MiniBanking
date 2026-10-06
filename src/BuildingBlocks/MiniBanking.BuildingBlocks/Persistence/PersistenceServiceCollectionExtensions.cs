using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBanking.BuildingBlocks.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    /// <summary>All modules share one PostgreSQL database; each one uses its own schema inside it (ADR 0004).</summary>
    public const string ConnectionStringName = "Database";

    /// <summary>
    /// Registers a module's DbContext with the shared conventions: PostgreSQL, snake_case names and
    /// a migrations history table inside the module's own schema.
    /// </summary>
    public static IServiceCollection AddModuleDbContext<TContext>(this IServiceCollection services, string schema)
        where TContext : DbContext
    {
        services.AddDbContext<TContext>((serviceProvider, options) =>
        {
            // Read lazily (when the first DbContext is created), so integration tests can override the value.
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                    .GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException(
                    $"Connection string '{ConnectionStringName}' is missing. Set ConnectionStrings:{ConnectionStringName}.");

            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
                .UseSnakeCaseNamingConvention();
        });

        services.AddSingleton(new ModuleDatabase(typeof(TContext).Assembly, typeof(TContext)));

        return services;
    }
}
