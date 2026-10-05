using Microsoft.AspNetCore.Http;
using MiniBanking.SharedKernel.Results;

namespace MiniBanking.BuildingBlocks.Errors;

/// <summary>
/// Turns a <see cref="Result"/> into an HTTP response. Failures become RFC 9457 ProblemDetails (ADR 0005).
/// </summary>
public static class ResultExtensions
{
    /// <summary>Returns <paramref name="onSuccess"/> for a success, otherwise a ProblemDetails response.</summary>
    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : result.ToProblem();

    /// <summary>Returns <paramref name="onSuccess"/> with the value for a success, otherwise a ProblemDetails response.</summary>
    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.ToProblem();

    /// <summary>Maps a failed result to a ProblemDetails response.</summary>
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("A successful result cannot be converted to a problem.");

        var error = result.Error;
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error is ValidationError validationError)
        {
            return TypedResults.ValidationProblem(
                errors: validationError.Errors,
                title: error.Message,
                extensions: extensions);
        }

        return TypedResults.Problem(
            statusCode: StatusCodeFor(error.Type),
            title: error.Code,
            detail: error.Message,
            extensions: extensions);
    }

    /// <summary>The single place where error categories are mapped to HTTP status codes.</summary>
    public static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };
}
