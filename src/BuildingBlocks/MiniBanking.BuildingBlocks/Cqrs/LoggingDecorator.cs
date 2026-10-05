using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Cqrs;

/// <summary>
/// Outermost decorator: logs which request ran, how long it took and whether it failed.
/// Exceptions are not logged here; they bubble up to the global exception handler.
/// </summary>
internal static class LoggingDecorator
{
    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> inner,
        ILogger<CommandHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public Task<Result> Handle(TCommand command, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TCommand).Name, () => inner.Handle(command, cancellationToken));
    }

    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> inner,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TCommand).Name, () => inner.Handle(command, cancellationToken));
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> inner,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken) =>
            LogAsync(logger, typeof(TQuery).Name, () => inner.Handle(query, cancellationToken));
    }

    private static async Task<TResult> LogAsync<TResult>(ILogger logger, string requestName, Func<Task<TResult>> next)
        where TResult : Result
    {
        var startedAt = Stopwatch.GetTimestamp();

        var result = await next();

        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (result.IsSuccess)
            logger.LogInformation("{RequestName} succeeded in {ElapsedMs:0} ms", requestName, elapsedMs);
        else
            logger.LogWarning(
                "{RequestName} failed with {ErrorCode} in {ElapsedMs:0} ms",
                requestName, result.Error.Code, elapsedMs);

        return result;
    }
}
