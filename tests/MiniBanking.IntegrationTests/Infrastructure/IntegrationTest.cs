namespace MiniBanking.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for tests that use the database: every test starts with empty tables.
/// Test classes run one after another (see xunit.runner.json) because they share one database.
/// </summary>
public abstract class IntegrationTest(MiniBankingApiFactory factory) : IAsyncLifetime
{
    protected MiniBankingApiFactory Factory { get; } = factory;

    protected HttpClient Client { get; } = factory.CreateClient();

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public virtual async ValueTask InitializeAsync() => await Factory.ResetDatabaseAsync();

    public virtual ValueTask DisposeAsync()
    {
        Client.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
