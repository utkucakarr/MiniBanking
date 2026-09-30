using MiniBanking.SharedKernel.Results;

namespace MiniBanking.SharedKernel.Tests.Results;

public class ResultTests
{
    private static readonly Error InsufficientFunds =
        Error.BusinessRule("Accounts.InsufficientFunds", "Available balance is not sufficient.");

    [Fact]
    public void Success_result_has_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_result_has_error()
    {
        var result = Result.Failure(InsufficientFunds);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(InsufficientFunds);
    }

    [Fact]
    public void Failure_with_Error_None_is_rejected()
    {
        var act = () => Result.Failure(Error.None);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Value_of_failed_result_cannot_be_accessed()
    {
        var result = Result.Failure<decimal>(InsufficientFunds);

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Value_and_Error_convert_implicitly_to_Result()
    {
        Result<decimal> fromValue = 250m;
        Result<decimal> fromError = InsufficientFunds;

        fromValue.IsSuccess.Should().BeTrue();
        fromValue.Value.Should().Be(250m);
        fromError.IsFailure.Should().BeTrue();
        fromError.Error.Should().Be(InsufficientFunds);
    }
}
