using FluentValidation;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Tests.Cqrs;

// Minimal requests, validators and handlers used to test the CQRS wiring.
// They are found by assembly scanning, exactly like real module handlers will be.

internal sealed class CallTracker
{
    public int HandlerCalls { get; set; }
}

// Command with a response
internal sealed record OpenTestAccount(decimal InitialDeposit) : ICommand<Guid>;

internal sealed class OpenTestAccountValidator : AbstractValidator<OpenTestAccount>
{
    public OpenTestAccountValidator()
    {
        RuleFor(command => command.InitialDeposit).GreaterThan(0);
    }
}

internal sealed class OpenTestAccountHandler(CallTracker tracker) : ICommandHandler<OpenTestAccount, Guid>
{
    public Task<Result<Guid>> Handle(OpenTestAccount command, CancellationToken cancellationToken)
    {
        tracker.HandlerCalls++;
        return Task.FromResult(Result.Success(Guid.CreateVersion7()));
    }
}

// Command without a response
internal sealed record FreezeTestAccount(string Reason) : ICommand;

internal sealed class FreezeTestAccountValidator : AbstractValidator<FreezeTestAccount>
{
    public FreezeTestAccountValidator()
    {
        RuleFor(command => command.Reason).NotEmpty();
    }
}

internal sealed class FreezeTestAccountHandler(CallTracker tracker) : ICommandHandler<FreezeTestAccount>
{
    public Task<Result> Handle(FreezeTestAccount command, CancellationToken cancellationToken)
    {
        tracker.HandlerCalls++;
        return Task.FromResult(Result.Success());
    }
}

// Query
internal sealed record GetTestBalance(Guid AccountId) : IQuery<decimal>;

internal sealed class GetTestBalanceValidator : AbstractValidator<GetTestBalance>
{
    public GetTestBalanceValidator()
    {
        RuleFor(query => query.AccountId).NotEmpty();
    }
}

internal sealed class GetTestBalanceHandler(CallTracker tracker) : IQueryHandler<GetTestBalance, decimal>
{
    public Task<Result<decimal>> Handle(GetTestBalance query, CancellationToken cancellationToken)
    {
        tracker.HandlerCalls++;
        return Task.FromResult(Result.Success(250.00m));
    }
}
