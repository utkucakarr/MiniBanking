using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.BuildingBlocks.Persistence;
using MiniBanking.IntegrationTests.Infrastructure;
using MiniBanking.SharedKernel.Results;
using Npgsql;

namespace MiniBanking.IntegrationTests.BuildingBlocks;

/// <summary>
/// Tests the transaction decorator on its own: a fake Notes module with its own DbContext,
/// in a separate database on the shared PostgreSQL container.
/// </summary>
public sealed class TransactionDecoratorTests : IAsyncLifetime
{
    private readonly ServiceProvider _provider;

    public TransactionDecoratorTests(MiniBankingApiFactory factory)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(factory.ConnectionString)
        {
            Database = "transaction_decorator_tests"
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{PersistenceServiceCollectionExtensions.ConnectionStringName}"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddModuleDbContext<NotesDbContext>(NotesDbContext.Schema);
        services.AddCqrs(typeof(TransactionDecoratorTests).Assembly);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        // A fresh database per test; EnsureCreated builds the tables straight from the model (no migrations).
        await using var scope = _provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotesDbContext>();
        await dbContext.Database.EnsureDeletedAsync(CancellationToken);
        await dbContext.Database.EnsureCreatedAsync(CancellationToken);
    }

    [Fact]
    public async Task Successful_command_is_saved_and_committed()
    {
        var result = await SendAsync<AddNote, Guid>(new AddNote("Hello"));

        result.IsSuccess.Should().BeTrue();
        (await LoadNotesAsync()).Should().ContainSingle(note => note.Id == result.Value && note.Text == "Hello");
    }

    [Fact]
    public async Task Failed_result_rolls_back_everything()
    {
        var result = await SendAsync<AddNote, Guid>(new AddNote("Rejected", Reject: true));

        result.Error.Should().Be(NoteErrors.Rejected);
        (await LoadNotesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Exception_rolls_back_everything_and_propagates()
    {
        await using var scope = _provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<AddNoteThenCrash>>();

        var act = () => handler.Handle(new AddNoteThenCrash("Lost"), CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await LoadNotesAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Concurrent_change_returns_conflict_instead_of_overwriting()
    {
        var added = await SendAsync<AddNote, Guid>(new AddNote("Original"));

        await using var scope = _provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<RenameNoteWithConcurrentEdit>>();
        var result = await handler.Handle(new RenameNoteWithConcurrentEdit(added.Value, "Mine"), CancellationToken);

        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("Concurrency.Conflict");
        (await LoadNotesAsync()).Single().Text.Should().Be("changed by someone else");
    }

    private async Task<Result<TResponse>> SendAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommand<TResponse>
    {
        // A new scope per request, like an HTTP request in the API.
        await using var scope = _provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        return await handler.Handle(command, CancellationToken);
    }

    private async Task<List<Note>> LoadNotesAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<NotesDbContext>()
            .Notes.AsNoTracking().ToListAsync(CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _provider.DisposeAsync();
}
