using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MiniBanking.BuildingBlocks.Persistence;
using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(MiniBanking.IntegrationTests.Infrastructure.MiniBankingApiFactory))]

namespace MiniBanking.IntegrationTests.Infrastructure;

/// <summary>
/// One instance for the whole test run (assembly fixture): starts a throwaway PostgreSQL in Docker,
/// then the real API in memory against it. Migrations run at startup, as in Development.
/// </summary>
public sealed class MiniBankingApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    private Respawner? _respawner;

    public string ConnectionString => _postgres.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Accessing Server starts the API, which applies all module migrations.
        _ = Server;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Point the API at the test container instead of the docker-compose database.
        builder.UseSetting(
            $"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}",
            ConnectionString);
    }

    /// <summary>Deletes all rows from every module table (keeps the schema and migration history).</summary>
    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        if (_respawner is null)
        {
            try
            {
                _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
                {
                    DbAdapter = DbAdapter.Postgres,
                    TablesToIgnore = [new Table("__EFMigrationsHistory")]
                });
            }
            catch (InvalidOperationException)
            {
                // Respawn refuses a database without tables (before the first migration): nothing to reset.
                return;
            }
        }

        await _respawner.ResetAsync(connection);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}
