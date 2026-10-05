using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Persistence;
using MiniBanking.IntegrationTests.Infrastructure;
using Npgsql;

namespace MiniBanking.IntegrationTests.BuildingBlocks;

/// <summary>Tests HasValueObjectConversion against real PostgreSQL, using the fake Notes module.</summary>
public sealed class ValueObjectConversionTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;

    public ValueObjectConversionTests(MiniBankingApiFactory factory)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(factory.ConnectionString)
        {
            Database = "value_object_conversion_tests"
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}"] = connectionString
            })
            .Build();

        _provider = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddModuleDbContext<NotesDbContext>(NotesDbContext.Schema)
            .BuildServiceProvider();
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    // A new scope each time, so every call gets a fresh DbContext (nothing cached from earlier calls).
    private NotesDbContext NewDbContext() => _provider.CreateScope().ServiceProvider.GetRequiredService<NotesDbContext>();

    public async ValueTask InitializeAsync()
    {
        var dbContext = NewDbContext();
        await dbContext.Database.EnsureDeletedAsync(CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken);
    }

    [Fact]
    public async Task Value_object_round_trips_through_a_single_column()
    {
        var id = Guid.CreateVersion7();
        var writer = NewDbContext();
        writer.Notes.Add(new Note { Id = id, Text = "Hello", Tag = NoteTag.Create("URGENT").Value });
        await writer.SaveChangesAsync(CancellationToken);

        var note = await NewDbContext().Notes.SingleAsync(n => n.Id == id, CancellationToken);

        note.Tag.Should().Be(NoteTag.Create("URGENT").Value);
    }

    [Fact]
    public async Task Persisted_value_is_not_revalidated_when_loaded()
    {
        // A row saved before the "upper case" rule existed: Create would reject it today.
        var id = Guid.CreateVersion7();
        await NewDbContext().Database.ExecuteSqlAsync(
            $"INSERT INTO notes.notes (id, text, tag) VALUES ({id}, 'Old note', 'legacy')", CancellationToken);

        var note = await NewDbContext().Notes.SingleAsync(n => n.Id == id, CancellationToken);

        NoteTag.Create("legacy").IsFailure.Should().BeTrue();
        note.Tag!.Value.Should().Be("legacy");
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();
}
