using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Persistence;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Cqrs;

/// <summary>
/// Innermost decorator, commands only: runs the handler in one database transaction on the DbContext
/// of the command's own module, then saves and commits (ADR 0004). Handlers never call SaveChanges.
/// A failed result or an exception rolls everything back.
/// </summary>
internal static class TransactionDecorator
{
    internal static readonly Error ConcurrencyConflict = Error.Conflict(
        "Concurrency.Conflict",
        "The data was changed by another request. Reload it and try again.");

    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        IEnumerable<ModuleDatabase> databases,
        IServiceProvider serviceProvider)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var dbContext = FindDbContext<TCommand>(databases, serviceProvider);
            if (dbContext is null)
                return await inner.Handle(command, cancellationToken);

            return await RunInTransactionAsync(
                dbContext,
                () => inner.Handle(command, cancellationToken),
                Result.Failure,
                cancellationToken);
        }
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        IEnumerable<ModuleDatabase> databases,
        IServiceProvider serviceProvider)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var dbContext = FindDbContext<TCommand>(databases, serviceProvider);
            if (dbContext is null)
                return await inner.Handle(command, cancellationToken);

            return await RunInTransactionAsync(
                dbContext,
                () => inner.Handle(command, cancellationToken),
                Result.Failure<TResponse>,
                cancellationToken);
        }
    }

    /// <summary>
    /// The DbContext registered for the assembly that contains the command, or null when that module
    /// has no database (then the command simply runs without a transaction).
    /// </summary>
    private static DbContext? FindDbContext<TCommand>(
        IEnumerable<ModuleDatabase> databases,
        IServiceProvider serviceProvider)
    {
        var database = databases.SingleOrDefault(db => db.ModuleAssembly == typeof(TCommand).Assembly);

        return database is null
            ? null
            : (DbContext)serviceProvider.GetRequiredService(database.DbContextType);
    }

    /// <param name="toFailure">Creates a failed result of the right type (Result or Result&lt;T&gt;) from an error.</param>
    private static async Task<TResult> RunInTransactionAsync<TResult>(
        DbContext dbContext,
        Func<Task<TResult>> handle,
        Func<Error, TResult> toFailure,
        CancellationToken cancellationToken)
        where TResult : Result
    {
        // Disposing a transaction that was not committed rolls it back.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var result = await handle();
        if (result.IsFailure)
            return result;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Optimistic concurrency (xmin): someone else changed the row after we loaded it (ADR 0005).
            return toFailure(ConcurrencyConflict);
        }

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}
