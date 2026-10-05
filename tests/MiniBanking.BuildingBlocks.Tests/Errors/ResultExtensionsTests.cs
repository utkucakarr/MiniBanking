using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MiniBanking.BuildingBlocks.Errors;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Tests.Errors;

public class ResultExtensionsTests
{
    public static TheoryData<ErrorType, int> StatusCodes => new()
    {
        { ErrorType.Validation, 400 },
        { ErrorType.Unauthorized, 401 },
        { ErrorType.Forbidden, 403 },
        { ErrorType.NotFound, 404 },
        { ErrorType.Conflict, 409 },
        { ErrorType.BusinessRule, 422 }
    };

    [Theory]
    [MemberData(nameof(StatusCodes))]
    public void Each_error_type_maps_to_its_http_status_code(ErrorType type, int expectedStatusCode)
    {
        var error = new Error("Accounts.Something", "Something happened.", type);

        var response = Result.Failure(error).ToProblem();

        var problem = response.Should().BeOfType<ProblemHttpResult>().Subject;
        problem.StatusCode.Should().Be(expectedStatusCode);
        problem.ProblemDetails.Title.Should().Be("Accounts.Something");
        problem.ProblemDetails.Detail.Should().Be("Something happened.");
        problem.ProblemDetails.Extensions["code"].Should().Be("Accounts.Something");
    }

    [Fact]
    public void Validation_error_becomes_400_with_errors_per_field()
    {
        var error = new ValidationError(new Dictionary<string, string[]>
        {
            ["Amount"] = ["Amount must be greater than zero."]
        });

        var response = Result.Failure(error).ToProblem();

        var problem = response.Should().BeOfType<ValidationProblem>().Subject;
        problem.StatusCode.Should().Be(400);
        problem.ProblemDetails.Errors.Should().ContainKey("Amount");
        problem.ProblemDetails.Extensions["code"].Should().Be("Validation.Failed");
    }

    [Fact]
    public void Match_returns_the_success_response_for_a_successful_result()
    {
        Result<int> result = 42;

        var response = result.Match(value => TypedResults.Ok(value));

        response.Should().BeOfType<Ok<int>>().Which.Value.Should().Be(42);
    }

    [Fact]
    public void Match_returns_a_problem_for_a_failed_result()
    {
        Result<int> result = Error.NotFound("Accounts.NotFound", "Account not found.");

        var response = result.Match(value => TypedResults.Ok(value));

        response.Should().BeOfType<ProblemHttpResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Successful_result_cannot_be_converted_to_a_problem()
    {
        var act = () => Result.Success().ToProblem();

        act.Should().Throw<InvalidOperationException>();
    }
}
