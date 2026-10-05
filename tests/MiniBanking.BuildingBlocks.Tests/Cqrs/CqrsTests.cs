using Microsoft.Extensions.DependencyInjection;
using MiniBanking.BuildingBlocks.Cqrs;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Tests.Cqrs;

public sealed class CqrsTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    public CqrsTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<CallTracker>();
        services.AddCqrs(typeof(CqrsTests).Assembly);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        _scope = _provider.CreateScope();
    }

    private T Resolve<T>()
        where T : notnull => _scope.ServiceProvider.GetRequiredService<T>();

    private CallTracker Tracker => Resolve<CallTracker>();

    [Fact]
    public async Task Valid_command_reaches_the_handler()
    {
        var handler = Resolve<ICommandHandler<OpenTestAccount, Guid>>();

        var result = await handler.Handle(new OpenTestAccount(100m), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        Tracker.HandlerCalls.Should().Be(1);
    }

    [Fact]
    public async Task Invalid_command_returns_validation_error_and_never_reaches_the_handler()
    {
        var handler = Resolve<ICommandHandler<OpenTestAccount, Guid>>();

        var result = await handler.Handle(new OpenTestAccount(-5m), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Should().BeOfType<ValidationError>()
            .Which.Errors.Should().ContainKey(nameof(OpenTestAccount.InitialDeposit));
        Tracker.HandlerCalls.Should().Be(0);
    }

    [Fact]
    public async Task Command_without_response_is_validated_and_handled()
    {
        var handler = Resolve<ICommandHandler<FreezeTestAccount>>();

        var invalid = await handler.Handle(new FreezeTestAccount(""), TestContext.Current.CancellationToken);
        var valid = await handler.Handle(new FreezeTestAccount("Court order"), TestContext.Current.CancellationToken);

        invalid.Error.Should().BeOfType<ValidationError>();
        valid.IsSuccess.Should().BeTrue();
        Tracker.HandlerCalls.Should().Be(1);
    }

    [Fact]
    public async Task Query_is_validated_and_handled()
    {
        var handler = Resolve<IQueryHandler<GetTestBalance, decimal>>();

        var invalid = await handler.Handle(new GetTestBalance(Guid.Empty), TestContext.Current.CancellationToken);
        var valid = await handler.Handle(new GetTestBalance(Guid.CreateVersion7()), TestContext.Current.CancellationToken);

        invalid.Error.Should().BeOfType<ValidationError>();
        valid.Value.Should().Be(250.00m);
        Tracker.HandlerCalls.Should().Be(1);
    }

    [Fact]
    public void Handlers_are_wrapped_with_logging_as_the_outermost_decorator()
    {
        var handler = Resolve<ICommandHandler<OpenTestAccount, Guid>>();

        // The decorator types are internal, so we check their names via reflection.
        handler.GetType().DeclaringType!.Name.Should().Be("LoggingDecorator");
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
