using Microsoft.Extensions.DependencyInjection;

namespace MiniBanking.BuildingBlocks.Errors;

public static class ErrorHandlingServiceCollectionExtensions
{
    /// <summary>
    /// Registers ProblemDetails support and the global exception handler.
    /// The host must also call <c>app.UseExceptionHandler()</c>.
    /// </summary>
    public static IServiceCollection AddErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
