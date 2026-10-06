using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MiniBanking.BuildingBlocks.Persistence;

public static class MigrationExtensions
{
    /// <summary>
    /// Applies the pending EF Core migrations of every module.
    /// Development and tests only: in production, migrations are an explicit deployment step (ADR 0004).
    /// </summary>
    public static async Task MigrateModuleDatabasesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        // DbContext is a scoped service and there is no HTTP request at startup, so we create the scope ourselves.
        await using var scope = services.CreateAsyncScope();

        foreach (var database in scope.ServiceProvider.GetServices<ModuleDatabase>())
        {
            var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(database.DbContextType);
            await dbContext.Database.MigrateAsync(cancellationToken);
        }
    }
}
